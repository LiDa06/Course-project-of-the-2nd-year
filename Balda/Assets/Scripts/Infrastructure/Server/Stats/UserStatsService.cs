using System;
using System.Linq;
using System.Threading.Tasks;
using Balda.Infrastructure.Server.Models;
using Balda.Infrastructure.LocalStorage;

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
                PointsForAllTime = 0
            };

            var response = await _client
                .From<UserStatsEntity>()
                .Insert(entity);

            return response.Models.FirstOrDefault();
        }

        public async Task<bool> UpdateAsync(UserStatsEntity stats)
        {
            await _client.From<UserStatsEntity>().Update(stats);
            return true;
        }

        public async Task<bool> ResetStatsAsync(Guid userId)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null)
                return false;

            stats.Wins = 0;
            stats.Losses = 0;
            stats.GamePlayed = 0;
            stats.WordsMadeUp = 0;
            stats.AverageWordLen = 0;
            stats.LongestWord = 0;
            stats.SeriesOfVictories = 0;
            stats.PointsForAllTime = 0;

            await _client.From<UserStatsEntity>().Update(stats);
            return true;
        }

        public async Task MergeGuestProgressAsync(Guid userId, LocalPlayerData local)
        {
            var stats = await GetByUserIdAsync(userId);
            if (stats == null)
                return;

            int oldGames = stats.GamePlayed;
            int localGames = local.GamePlayed;

            stats.Wins += local.Wins;
            stats.Losses += local.Losses;
            stats.GamePlayed += local.GamePlayed;
            stats.WordsMadeUp += local.WordsMadeUp;
            stats.PointsForAllTime += local.PointsForAllTime;
            stats.SeriesOfVictories = Math.Max(stats.SeriesOfVictories, local.SeriesOfVictories);
            stats.LongestWord = Math.Max(stats.LongestWord, local.LongestWord);

            if (oldGames == 0 && localGames > 0)
            {
                stats.AverageWordLen = local.AverageWordLen;
            }
            else if (localGames > 0)
            {
                int totalGames = oldGames + localGames;
                int weighted =
                    (stats.AverageWordLen * oldGames) +
                    (local.AverageWordLen * localGames);

                stats.AverageWordLen = totalGames == 0 ? 0 : weighted / totalGames;
            }

            await UpdateAsync(stats);
        }
    }
}