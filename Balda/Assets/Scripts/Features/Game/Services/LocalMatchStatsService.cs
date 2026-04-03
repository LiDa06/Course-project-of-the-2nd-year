using System;
using Balda.Features.Game.Domain;
using Balda.Infrastructure.LocalStorage;

namespace Balda.Features.Game.Services
{
    public class LocalMatchStatsService
    {
        public void ApplyFinishedMatch(GameSession session)
        {
            if (session == null)
                return;

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            var playerData = LocalPlayerData.Instance;
            if (playerData == null)
                return;

            playerData.GamePlayed++;
            playerData.PointsForAllTime += session.PlayerOneScore;

            int matchWordCount = session.UsedWords?.Count ?? 0;
            playerData.WordsMadeUp += matchWordCount;

            int longestWordInMatch = 0;
            int totalLettersInMatch = 0;
            string bestWord = "";

            if (session.UsedWords != null)
            {
                for (int i = 0; i < session.UsedWords.Count; i++)
                {
                    string word = session.UsedWords[i] ?? "";
                    int length = word.Trim().Length;
                    totalLettersInMatch += length;

                    if (length > longestWordInMatch)
                    {
                        longestWordInMatch = length;
                        bestWord = word;
                    }
                }
            }

            playerData.TotalLettersInAcceptedWords += totalLettersInMatch;
            playerData.AverageWordLen = playerData.WordsMadeUp > 0
                ? MathfRoundToInt((float)playerData.TotalLettersInAcceptedWords / playerData.WordsMadeUp)
                : 0;

            if (longestWordInMatch > playerData.LongestWord)
                playerData.LongestWord = longestWordInMatch;

            switch (session.WinnerIndex)
            {
                case 0:
                    playerData.Wins++;
                    playerData.SeriesOfVictories++;
                    break;
                case 1:
                    playerData.Losses++;
                    playerData.SeriesOfVictories = 0;
                    break;
                default:
                    playerData.SeriesOfVictories = 0;
                    break;
            }

            playerData.AddRecentGame(new RecentGameInfo
            {
                FinishedAtTicks = session.FinishedAtTicks > 0 ? session.FinishedAtTicks : DateTime.UtcNow.Ticks,
                Mode = ToModeString(session.Mode),
                BoardSize = session.BoardSize,
                Result = ToResultString(session.WinnerIndex),
                PlayerOneScore = session.PlayerOneScore,
                PlayerTwoScore = session.PlayerTwoScore,
                TurnCount = session.TurnNumber,
                BestWord = bestWord,
                DurationSeconds = CalculateDurationSeconds(session)
            });

            LocalPlayerData.Save();
        }

        private static int CalculateDurationSeconds(GameSession session)
        {
            if (session.StartedAtTicks <= 0 || session.FinishedAtTicks <= 0)
                return 0;

            var start = new DateTime(session.StartedAtTicks, DateTimeKind.Utc);
            var finish = new DateTime(session.FinishedAtTicks, DateTimeKind.Utc);
            return Math.Max(0, (int)(finish - start).TotalSeconds);
        }

        private static string ToModeString(GameMode mode)
        {
            return mode switch
            {
                GameMode.Solo => "solo",
                GameMode.LocalVersus => "local",
                GameMode.Online => "online",
                _ => "solo"
            };
        }

        private static string ToResultString(int winnerIndex)
        {
            return winnerIndex switch
            {
                0 => "win",
                1 => "loss",
                _ => "draw"
            };
        }

        private static int MathfRoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
