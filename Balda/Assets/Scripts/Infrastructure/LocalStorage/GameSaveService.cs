using System.IO;
using UnityEngine;

namespace Balda.Infrastructure.LocalStorage
{
    public static class GameSaveService
    {
        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "local_game_save.json");

        public static void Save(LocalGameSave save)
        {
            if (save == null)
                return;

            save.SavedAtTicks = System.DateTime.UtcNow.Ticks;

            var json = JsonUtility.ToJson(save, true);
            File.WriteAllText(FilePath, json);
        }

        public static LocalGameSave Load()
        {
            if (!File.Exists(FilePath))
                return null;

            try
            {
                var json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<LocalGameSave>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("GameSaveService.Load failed: " + ex.Message);
                return null;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(FilePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}