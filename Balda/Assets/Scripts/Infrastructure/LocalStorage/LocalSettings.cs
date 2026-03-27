using System.IO;
using UnityEngine;
using AudioType = Balda.Infrastructure.Audio.AudioType;
using ThemeType = Balda.Infrastructure.Theme.ThemeType;

namespace Balda.Infrastructure.LocalStorage
{
    [System.Serializable]
    public class LocalSettings
    {
        public static LocalSettings Instance { get; private set; }

        public AudioType Audio = AudioType.On;
        public ThemeType Theme = ThemeType.Light;
        public int BoardSize = 5;

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "local_settings_data.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    Instance = JsonUtility.FromJson<LocalSettings>(json);

                    if (Instance == null)
                        Instance = CreateDefault();
                }
                else
                {
                    Instance = CreateDefault();
                    Save();
                }
            }
            catch
            {
                Instance = CreateDefault();
                Save();
            }
        }

        public static void Save()
        {
            Instance ??= CreateDefault();

            var json = JsonUtility.ToJson(Instance, true);
            File.WriteAllText(FilePath, json);
        }

        public static void ResetToDefault()
        {
            Instance = CreateDefault();
            Save();
        }

        private static LocalSettings CreateDefault()
        {
            return new LocalSettings
            {
                Audio = AudioType.On,
                Theme = ThemeType.Light,
                BoardSize = 5
            };
        }
    }
}