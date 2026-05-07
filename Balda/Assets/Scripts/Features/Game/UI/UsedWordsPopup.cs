using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class UsedWordsPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text wordsText;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        public void Show(IReadOnlyList<string> words)
        {
            if (titleText != null)
                titleText.text = $"Все слова ({(words != null ? words.Count : 0)})";

            if (wordsText != null)
                wordsText.text = BuildWordsText(words);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private static string BuildWordsText(IReadOnlyList<string> words)
        {
            if (words == null || words.Count == 0)
                return "Пока слов нет.";

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < words.Count; i++)
            {
                string word = string.IsNullOrWhiteSpace(words[i]) ? "—" : words[i].Trim();
                builder.Append(i + 1)
                       .Append(". ")
                       .Append(word);

                if (i < words.Count - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }
    }
}