using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class LetterInputPopup : MonoBehaviour
    {
        private static readonly string[] RussianLetters =
        {
            "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й",
            "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф",
            "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я"
        };

        private const float ButtonsSpacing = 20f;

        [SerializeField] private GameObject root;
        [SerializeField] private Transform lettersContainer;
        [SerializeField] private Button letterButtonPrefab;
        [SerializeField] private Button cancelButton;

        private Action<string> onConfirm;
        private Action onCancel;
        private bool isBuilt;

        private void Awake()
        {
            ConfigureLayoutSpacing();
            BuildLetterButtons();

            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancel);

            Hide();
        }

        public void Show(Action<string> confirmCallback, Action cancelCallback = null)
        {
            onConfirm = confirmCallback;
            onCancel = cancelCallback;

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void ConfigureLayoutSpacing()
        {
            if (lettersContainer == null)
                return;

            var gridLayout = lettersContainer.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.spacing = new Vector2(ButtonsSpacing, ButtonsSpacing);
                return;
            }

            var horizontalLayout = lettersContainer.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                horizontalLayout.spacing = ButtonsSpacing;
                return;
            }

            var verticalLayout = lettersContainer.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
                verticalLayout.spacing = ButtonsSpacing;
        }

        private void BuildLetterButtons()
        {
            if (isBuilt || lettersContainer == null || letterButtonPrefab == null)
                return;

            for (int i = 0; i < RussianLetters.Length; i++)
            {
                string letter = RussianLetters[i];
                Button button = Instantiate(letterButtonPrefab, lettersContainer);
                button.name = $"Letter_{letter}";

                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = letter;
                    label.raycastTarget = false;
                }

                button.onClick.AddListener(() => HandleLetterSelected(letter));
            }

            isBuilt = true;
        }

        private void HandleLetterSelected(string letter)
        {
            if (string.IsNullOrWhiteSpace(letter))
                return;

            onConfirm?.Invoke(letter.Trim().ToUpperInvariant());
            CleanupAndHide();
        }

        private void HandleCancel()
        {
            onCancel?.Invoke();
            CleanupAndHide();
        }

        private void CleanupAndHide()
        {
            onConfirm = null;
            onCancel = null;
            Hide();
        }
    }
}
