using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Balda.Infrastructure.LocalStorage
{
    [Serializable]
    public class LocalPlayerData
    {
        public static LocalPlayerData Instance { get; private set; }

        public bool IsGuest = true;
        public bool IsFirstLaunch = true;

        public string LocalDisplayName = "Guest";
        public string Email = "";
        public string CloudUserId = "";

        public long CreatedAtTicks = DateTime.UtcNow.Ticks;

        public int Wins = 0;
        public int Losses = 0;
        public int GamePlayed = 0;
        public int WordsMadeUp = 0;
        public int AverageWordLen = 0;
        public int LongestWord = 0;
        public int SeriesOfVictories = 0;
        public int PointsForAllTime = 0;
        public int TotalLettersInAcceptedWords = 0;

        public List<RecentGameInfo> RecentGames = new();

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "local_player_data.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Instance = JsonUtility.FromJson<LocalPlayerData>(json);

                    if (Instance == null)
                    {
                        Instance = CreateDefault();
                        Save();
                    }

                    Instance.RecentGames ??= new List<RecentGameInfo>();
                }
                else
                {
                    Instance = CreateDefault();
                    Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("LocalPlayerData.Load failed: " + ex.Message);
                Instance = CreateDefault();
                Save();
            }
        }

        public static void Save()
        {
            Instance ??= CreateDefault();
            Instance.RecentGames ??= new List<RecentGameInfo>();

            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(FilePath, json);
        }

        public static void ResetToGuest()
        {
            Instance = CreateDefault();
            Save();
        }

        private static LocalPlayerData CreateDefault()
        {
            return new LocalPlayerData
            {
                IsGuest = true,
                IsFirstLaunch = true,
                LocalDisplayName = "Guest",
                Email = "",
                CloudUserId = "",
                CreatedAtTicks = DateTime.UtcNow.Ticks,
                Wins = 0,
                Losses = 0,
                GamePlayed = 0,
                WordsMadeUp = 0,
                AverageWordLen = 0,
                LongestWord = 0,
                SeriesOfVictories = 0,
                PointsForAllTime = 0,
                TotalLettersInAcceptedWords = 0,
                RecentGames = new List<RecentGameInfo>()
            };
        }

        public DateTime GetCreatedAtUtc()
        {
            return new DateTime(CreatedAtTicks, DateTimeKind.Utc);
        }

        public void SetGuest(string guestName = "Guest")
        {
            IsGuest = true;
            CloudUserId = "";
            Email = "";
            LocalDisplayName = string.IsNullOrWhiteSpace(guestName) ? "Guest" : guestName;
            Save();
        }

        public void MarkAsCloudUser(Guid userId, string username, string email)
        {
            IsGuest = false;
            IsFirstLaunch = false;
            CloudUserId = userId.ToString();
            LocalDisplayName = string.IsNullOrWhiteSpace(username) ? LocalDisplayName : username;
            Email = email ?? "";
            Save();
        }

        public void UpdateDisplayName(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                LocalDisplayName = newName.Trim();
                Save();
            }
        }

        public void UpdateEmail(string newEmail)
        {
            Email = string.IsNullOrWhiteSpace(newEmail) ? "" : newEmail.Trim();
            Save();
        }

        public void MarkFirstLaunchCompleted()
        {
            IsFirstLaunch = false;
            Save();
        }

        public void AddRecentGame(RecentGameInfo game)
        {
            if (game == null)
                return;

            RecentGames ??= new List<RecentGameInfo>();
            RecentGames.Insert(0, game);

            while (RecentGames.Count > 3)
                RecentGames.RemoveAt(RecentGames.Count - 1);
        }

        public void ResetStats()
        {
            Wins = 0;
            Losses = 0;
            GamePlayed = 0;
            WordsMadeUp = 0;
            AverageWordLen = 0;
            LongestWord = 0;
            SeriesOfVictories = 0;
            PointsForAllTime = 0;
            TotalLettersInAcceptedWords = 0;
            RecentGames = new List<RecentGameInfo>();
            Save();
        }
    }
}
