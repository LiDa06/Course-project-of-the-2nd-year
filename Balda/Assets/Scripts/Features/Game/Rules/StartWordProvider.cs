using System;
using System.Collections.Generic;
using UnityEngine;

namespace Balda.Features.Game.Rules
{
    public class StartWordProvider
    {
        private const string StartWordsResourcePath = "Dictionaries/ru_start_words_5_10_utf8_upper";

        private readonly WordDictionaryService dictionaryService;

        private readonly Dictionary<int, List<string>> allWordsByLength = new();
        private readonly Dictionary<int, List<string>> remainingWordsByLength = new();
        private readonly Dictionary<int, string> lastWordByLength = new();

        private bool isLoaded;

        public StartWordProvider(WordDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
        }

        public string GetStartWord(int length)
        {
            EnsureLoaded();

            if (!allWordsByLength.TryGetValue(length, out var allWords) || allWords.Count == 0)
                throw new InvalidOperationException($"Нет стартовых слов длины {length}.");

            if (!remainingWordsByLength.TryGetValue(length, out var remaining) || remaining.Count == 0)
            {
                RefillPool(length);
                remaining = remainingWordsByLength[length];
            }

            int lastIndex = remaining.Count - 1;
            string selected = remaining[lastIndex];
            remaining.RemoveAt(lastIndex);

            lastWordByLength[length] = selected;
            return selected;
        }

        private void EnsureLoaded()
        {
            if (isLoaded)
                return;

            isLoaded = true;

            TextAsset asset = Resources.Load<TextAsset>(StartWordsResourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    $"Не найден файл стартовых слов: Resources/{StartWordsResourcePath}.txt");

            string[] lines = asset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> unique = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < lines.Length; i++)
            {
                string word = dictionaryService.Normalize(lines[i]);

                if (string.IsNullOrWhiteSpace(word))
                    continue;

                if (word.Length < 5 || word.Length > 10)
                    continue;

                if (!dictionaryService.Contains(word))
                    continue;

                if (!unique.Add(word))
                    continue;

                if (!allWordsByLength.TryGetValue(word.Length, out var list))
                {
                    list = new List<string>();
                    allWordsByLength[word.Length] = list;
                }

                list.Add(word);
            }

            foreach (var pair in allWordsByLength)
                Shuffle(pair.Value);

            Debug.Log($"StartWordProvider: загружены стартовые слова для длин {string.Join(", ", allWordsByLength.Keys)}");
        }

        private void RefillPool(int length)
        {
            List<string> pool = new List<string>(allWordsByLength[length]);

            if (lastWordByLength.TryGetValue(length, out string lastWord) && pool.Count > 1)
                pool.Remove(lastWord);

            Shuffle(pool);
            remainingWordsByLength[length] = pool;
        }

        private void Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}