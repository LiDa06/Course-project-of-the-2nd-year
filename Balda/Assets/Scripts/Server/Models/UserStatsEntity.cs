using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace Assets.Scripts.Server.Models
{
    [Table("user_stats")]
    public class UserStatsEntity : BaseModel
    {
        [PrimaryKey("user_id")]
        public Guid UserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("wins")]
        public int Wins { get; set; }

        [Column("losses")]
        public int Losses { get; set; }

        [Column("games_played")]
        public int GamePlayed { get; set; }

        [Column("words_made_up")]
        public int WordsMadeUp { get; set; }

        [Column("average_word_len")]
        public int AverageWordLen { get; set; }

        [Column("longest_word")]
        public int LongestWord { get; set; }

        [Column("series_of_victories")]
        public int SeriesOfVictories { get; set; }

        [Column("points_for_all_time")]
        public int PointsForAllTime { get; set; }
    }
}