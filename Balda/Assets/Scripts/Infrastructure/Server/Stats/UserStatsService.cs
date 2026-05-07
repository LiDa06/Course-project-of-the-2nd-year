using System;
using System.Linq;
using System.Threading.Tasks;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Server.Models;

namespace Balda.Infrastructure.Server.Stats
{
    public class UserStatsService
    {
        private readonly Supabase.Client _client;

        public UserStatsService(Supabase.Client client)
        {
            _client = client;
        }

        public async Task<UserStatsEntity> GetByUserIdAsync(Guid userId)
        {
            var response = await _client
                .From<UserStatsEntity>()
                .Where(x => x.UserId == userId)
                .Get();

            return response.Models.FirstOrDefault();
        }

        public async Task<UserStatsEntity> CreateDefaultAsync(Guid userId)
        {
            var entity = new UserStatsEntity
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Wins = 0,
                Losses = 0,
                GamePlayed = 0,
                WordsMadeUp = 0,
                AverageWordLen = 0,
                LongestWord = 0,
                SeriesOfVictories = 0,
                PointsForAllTime = 0,
                TotalLettersInAcceptedWords = 0
            };

            var response = await _client
                .From<UserStatsEntity>()
                .Insert(entity);

            return response.Models.FirstOrDefault();
        }

        public async Task<bool> UpdateAsync(UserStatsEntity stats)
        {
            if (stats == null)
                return false;

            await _client.From<UserStatsEntity>().Update(stats);
            return true;
        }

        public async Task<bool> SaveFromLocalAsync(Guid userId, LocalPlayerData local)
        {
            if (local == null)
                return false;

            var stats = await GetByUserIdAsync(userId);
            bool needsInsert = stats == null;

            if (stats == null)
            {
                stats = new UserStatsEntity
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
            }

            stats.Wins = local.Wins;
            stats.Losses = local.Losses;
            stats.GamePlayed = local.GamePlayed;
            stats.WordsMadeUp = local.WordsMadeUp;
            stats.AverageWordLen = local.AverageWordLen;
            stats.LongestWord = local.LongestWord;
            stats.PointsForAllTime = local.PointsForAllTime;
            stats.TotalLettersInAcceptedWords = local.TotalLettersInAcceptedWords;

            if (stats.CreatedAt == default)
                stats.CreatedAt = DateTime.UtcNow;

            if (needsInsert)
                await _client.From<UserStatsEntity>().Insert(stats);
            else
                await _client.From<UserStatsEntity>().Update(stats);

            return true;
        }

        public async Task<bool> ResetStatsAsync(Guid userId)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null)
            {
                stats = await CreateDefaultAsync(userId);
                return stats != null;
            }

            stats.Wins = 0;
            stats.Losses = 0;
            stats.GamePlayed = 0;
            stats.WordsMadeUp = 0;
            stats.AverageWordLen = 0;
            stats.LongestWord = 0;
            stats.SeriesOfVictories = 0;
            stats.PointsForAllTime = 0;
            stats.TotalLettersInAcceptedWords = 0;

            await _client.From<UserStatsEntity>().Update(stats);
            return true;
        }

        public async Task MergeGuestProgressAsync(Guid userId, LocalPlayerData local)
        {
            if (local == null)
                return;

            var stats = await GetByUserIdAsync(userId);
            if (stats == null)
                stats = await CreateDefaultAsync(userId);

            if (stats == null)
                return;

            stats.Wins += local.Wins;
            stats.Losses += local.Losses;
            stats.GamePlayed += local.GamePlayed;
            stats.WordsMadeUp += local.WordsMadeUp;
            stats.PointsForAllTime += local.PointsForAllTime;
            stats.TotalLettersInAcceptedWords += local.TotalLettersInAcceptedWords;
            stats.LongestWord = Math.Max(stats.LongestWord, local.LongestWord);

            stats.AverageWordLen = stats.WordsMadeUp > 0
                ? RoundToInt((float)stats.TotalLettersInAcceptedWords / stats.WordsMadeUp)
                : 0;

            await UpdateAsync(stats);
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
