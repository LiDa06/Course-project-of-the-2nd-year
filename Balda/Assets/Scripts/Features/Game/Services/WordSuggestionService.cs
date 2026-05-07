using System;
using System.Collections.Generic;
using System.IO;
using Balda.Features.Game.Domain;
using Balda.Infrastructure.LocalStorage;
using UnityEngine;

namespace Balda.Features.Game.Services
{
    public class WordSuggestionService
    {
        [Serializable]
        private class SuggestedWordCollection
        {
            public List<SuggestedWordEntry> Items = new();
        }

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, "suggested_words.json");

        public void SaveSuggestion(string word, GameSession session)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            if (LocalPlayerData.Instance == null)
                LocalPlayerData.Load();

            SuggestedWordCollection collection = LoadInternal();

            collection.Items.Add(new SuggestedWordEntry
            {
                Word = word.Trim(),
                PlayerName = LocalPlayerData.Instance != null ? LocalPlayerData.Instance.LocalDisplayName : "",
                Email = LocalPlayerData.Instance != null ? LocalPlayerData.Instance.Email : "",
                BoardSize = session != null ? session.BoardSize : 5,
                SuggestedAtTicks = DateTime.UtcNow.Ticks
            });

            SaveInternal(collection);
        }

        private SuggestedWordCollection LoadInternal()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new SuggestedWordCollection();

                string json = File.ReadAllText(FilePath);
                SuggestedWordCollection collection = JsonUtility.FromJson<SuggestedWordCollection>(json);

                return collection ?? new SuggestedWordCollection();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("WordSuggestionService.LoadInternal failed: " + ex.Message);
                return new SuggestedWordCollection();
            }
        }

        private void SaveInternal(SuggestedWordCollection collection)
        {
            try
            {
                string json = JsonUtility.ToJson(collection, true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("WordSuggestionService.SaveInternal failed: " + ex.Message);
            }
        }
    }
}