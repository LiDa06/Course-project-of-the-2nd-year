using System;
using Balda.Infrastructure.LocalStorage;
using Postgrest.Attributes;
using Postgrest.Models;

namespace Balda.Infrastructure.Server.Models
{
    [Table("recent_games")]
    public class RecentGameEntity : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("finished_at")]
        public DateTime FinishedAt { get; set; }

        [Column("list_order")]
        public int ListOrder { get; set; }

        [Column("mode")]
        public string Mode { get; set; }

        [Column("board_size")]
        public int BoardSize { get; set; }

        [Column("result")]
        public string Result { get; set; }

        [Column("opponent_name")]
        public string OpponentName { get; set; }

        [Column("player_one_score")]
        public int PlayerOneScore { get; set; }

        [Column("player_two_score")]
        public int PlayerTwoScore { get; set; }

        [Column("turn_count")]
        public int TurnCount { get; set; }

        [Column("best_word")]
        public string BestWord { get; set; }

        [Column("duration_seconds")]
        public int DurationSeconds { get; set; }

        public static RecentGameEntity FromLocal(Guid userId, RecentGameInfo game, int listOrder)
        {
            if (game == null)
                return null;

            return new RecentGameEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                FinishedAt = TicksToUtc(game.FinishedAtTicks),
                ListOrder = listOrder,
                Mode = string.IsNullOrWhiteSpace(game.Mode) ? "solo" : game.Mode,
                BoardSize = game.BoardSize,
                Result = string.IsNullOrWhiteSpace(game.Result) ? "draw" : game.Result,
                OpponentName = game.OpponentName ?? string.Empty,
                PlayerOneScore = game.PlayerOneScore,
                PlayerTwoScore = game.PlayerTwoScore,
                TurnCount = game.TurnCount,
                BestWord = game.BestWord ?? string.Empty,
                DurationSeconds = game.DurationSeconds
            };
        }

        public RecentGameInfo ToLocal()
        {
            return new RecentGameInfo
            {
                FinishedAtTicks = DateTime.SpecifyKind(FinishedAt, DateTimeKind.Utc).Ticks,
                Mode = string.IsNullOrWhiteSpace(Mode) ? "solo" : Mode,
                BoardSize = BoardSize,
                Result = string.IsNullOrWhiteSpace(Result) ? "draw" : Result,
                OpponentName = OpponentName ?? string.Empty,
                PlayerOneScore = PlayerOneScore,
                PlayerTwoScore = PlayerTwoScore,
                TurnCount = TurnCount,
                BestWord = BestWord ?? string.Empty,
                DurationSeconds = DurationSeconds
            };
        }

        private static DateTime TicksToUtc(long ticks)
        {
            if (ticks <= 0)
                return DateTime.UtcNow;

            try
            {
                return new DateTime(ticks, DateTimeKind.Utc);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }
}
