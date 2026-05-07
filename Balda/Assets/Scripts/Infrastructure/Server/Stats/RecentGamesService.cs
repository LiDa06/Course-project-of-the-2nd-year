using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Balda.Infrastructure.LocalStorage;
using Balda.Infrastructure.Server.Models;

namespace Balda.Infrastructure.Server.Stats
{
    public class RecentGamesService
    {
        private readonly Supabase.Client _client;

        public RecentGamesService(Supabase.Client client)
        {
            _client = client;
        }

        public async Task<List<RecentGameInfo>> GetLastAsync(Guid userId, int count = 3)
        {
            var response = await _client
                .From<RecentGameEntity>()
                .Where(x => x.UserId == userId)
                .Get();

            if (response?.Models == null || response.Models.Count == 0)
                return new List<RecentGameInfo>();

            return response.Models
                .OrderBy(x => x.ListOrder)
                .ThenByDescending(x => x.FinishedAt)
                .Take(count)
                .Select(x => x.ToLocal())
                .ToList();
        }

        public async Task ReplaceLastAsync(Guid userId, List<RecentGameInfo> recentGames, int count = 3)
        {
            await DeleteAllAsync(userId);

            if (recentGames == null || recentGames.Count == 0)
                return;

            var entities = new List<RecentGameEntity>();
            int limit = Math.Min(count, recentGames.Count);

            for (int i = 0; i < limit; i++)
            {
                var entity = RecentGameEntity.FromLocal(userId, recentGames[i], i);
                if (entity != null)
                    entities.Add(entity);
            }

            if (entities.Count > 0)
                await _client.From<RecentGameEntity>().Insert(entities);
        }

        public async Task DeleteAllAsync(Guid userId)
        {
            await _client
                .From<RecentGameEntity>()
                .Where(x => x.UserId == userId)
                .Delete();
        }
    }
}
