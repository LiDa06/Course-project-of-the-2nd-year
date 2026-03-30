using System;
using System.Collections.Generic;
using UnityEngine;

namespace Balda.Features.Game.Rules
{
    public class WordDictionaryService
    {
        private const string MainDictionaryResourcePath = "Dictionaries/ru_words_9960_utf8_upper";

        private readonly HashSet<string> words = new(StringComparer.OrdinalIgnoreCase);
        private bool isLoaded;

        public bool Contains(string word)
        {
            EnsureLoaded();
            string normalized = Normalize(word);

            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            return words.Contains(normalized);
        }

        public string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            return value
                .Trim()
                .ToUpperInvariant()
                .Replace('Ё', 'Е');
        }

        private void EnsureLoaded()
        {
            if (isLoaded)
                return;

            isLoaded = true;

            TextAsset asset = Resources.Load<TextAsset>(MainDictionaryResourcePath);
            if (asset == null)
            {
                Debug.LogError($"Не найден словарь по пути Resources/{MainDictionaryResourcePath}.txt");
                return;
            }

            string[] lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string word = Normalize(lines[i]);
                if (!string.IsNullOrWhiteSpace(word))
                    words.Add(word);
            }

            Debug.Log($"WordDictionaryService: загружено слов: {words.Count}");
        }
    }
}