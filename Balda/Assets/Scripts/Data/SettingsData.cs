using System.IO;
using UnityEngine;

namespace Assets.Scripts.Data
{
    [System.Serializable]
    public class SettingsData
    {
        public static SettingsData Instance { get; private set; }

        public static bool SoundOn { get; private set; }
        public static ThemeType Theme { get; private set; }
        public static int BoardSize { get; private set; }

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "settings_data.json");

        public static void Load()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Instance = JsonUtility.FromJson<SettingsData>(json);
            }
            else
            {
                Instance = new SettingsData();
            }
        }

        public static void Save()
        {
            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(FilePath, json);
        }

        public static void SetSound(bool on)
        {
            SoundOn = on;
            PlayerPrefs.SetInt("Sound", on ? 1 : 0);
        }

        public static void SetTheme(ThemeType theme)
        {
            Theme = theme;
            PlayerPrefs.SetInt("Theme", (int)theme);
        }

        public static void SetBoardSize(int size)
        {
            BoardSize = size;
            PlayerPrefs.SetInt("BoardSize", size);
        }
    }
}
