using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.LocalData
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

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "local_player_data.json");

        public static void Load()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Instance = JsonUtility.FromJson<LocalPlayerData>(json);
            }
            else
            {
                Instance = new LocalPlayerData();
                Save();
            }
        }

        public static void Save()
        {
            Instance ??= new LocalPlayerData();

            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(FilePath, json);
        }

        public DateTime GetCreatedAtUtc() => new DateTime(CreatedAtTicks, DateTimeKind.Utc);

        public void MarkAsCloudUser(Guid userId, string username, string email)
        {
            IsGuest = false;
            CloudUserId = userId.ToString();
            LocalDisplayName = username;
            Email = email;
        }
        public static void ResetToGuest()
        {
            Instance = new LocalPlayerData();
            Save();
        }
    }
}