using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [System.Serializable]
    public class UserData
    {
        public static UserData Instance { get; private set; }

        public bool IsFirstLaunch = true;
        public string Name = "Guest";
        public string Email = "";
        public DateTime RegistrationDate = DateTime.Today;
        public int Wins = 0;
        public int Losses = 0;
        public int GamePlayed = 0;
        public int WordsMadeUp = 0;
        public int AverageWordLen = 0;
        public int TheLongestWord = 0;
        public int SeriesOfVictories = 0;
        public int PointsForAllTime = 0;

        private static string Path =>
            System.IO.Path.Combine(Application.persistentDataPath, "user_data.json");

        public static object Instanse { get; internal set; }

        public static void Load()
        {
            if (File.Exists(Path))
            {
                var json = File.ReadAllText(Path);
                Instance = JsonUtility.FromJson<UserData>(json);
            }
            else
            {
                Instance = new UserData();
            }
        }

        public static void Save()
        {
            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(Path, json);
        }
    }
}
