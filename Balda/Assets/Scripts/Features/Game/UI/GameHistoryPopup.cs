using System;
using Balda.Features.Game.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class GameHistoryPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button closeButton;

        private Action onPrevious;
        private Action onNext;
        private Action onClose;

        private void Awake()
        {
            if (previousButton != null)
                previousButton.onClick.AddListener(() => onPrevious?.Invoke());

            if (nextButton != null)
                nextButton.onClick.AddListener(() => onNext?.Invoke());

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleClose);

            HideImmediate();
        }

        public void Show(GameMoveRecord entry, int index, int totalCount, Action previousCallback, Action nextCallback, Action closeCallback)
        {
            onPrevious = previousCallback;
            onNext = nextCallback;
            onClose = closeCallback;

            UpdateEntry(entry, index, totalCount);
            SetVisible(true);
        }

        public void UpdateEntry(GameMoveRecord entry, int index, int totalCount)
        {
            if (titleText != null)
                titleText.text = BuildTitle(entry);

            if (summaryText != null)
                summaryText.text = BuildSummary(entry);

            if (counterText != null)
                counterText.text = totalCount > 0 ? $"{index + 1}/{totalCount}" : "0/0";

            if (previousButton != null)
                previousButton.interactable = index > 0;

            if (nextButton != null)
                nextButton.interactable = index >= 0 && index < totalCount - 1;
        }

        public void Hide()
        {
            onPrevious = null;
            onNext = null;
            onClose = null;
            SetVisible(false);
        }

        private void HideImmediate()
        {
            onPrevious = null;
            onNext = null;
            onClose = null;
            SetVisible(false);
        }

        private void HandleClose()
        {
            var callback = onClose;
            Hide();
            callback?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private static string BuildTitle(GameMoveRecord entry)
        {
            if (entry == null)
                return "История партии";

            if (entry.IsStartRecord)
                return "Стартовое слово";

            string playerName = string.IsNullOrWhiteSpace(entry.PlayerDisplayName)
                ? $"Игрок {entry.PlayerIndex + 1}"
                : entry.PlayerDisplayName;

            return $"Ход {entry.TurnNumber}: {playerName}";
        }

        private static string BuildSummary(GameMoveRecord entry)
        {
            if (entry == null)
                return "";

            if (entry.IsStartRecord)
                return string.IsNullOrWhiteSpace(entry.Word)
                    ? "Начальное состояние поля"
                    : $"Стартовое слово: {entry.Word}";

            string positionText = entry.PlacedRow >= 0 && entry.PlacedCol >= 0
                ? $"Клетка: {entry.PlacedRow + 1}, {entry.PlacedCol + 1}"
                : "Клетка: -";

            string letterText = string.IsNullOrWhiteSpace(entry.PlacedLetter)
                ? "Буква: -"
                : $"Буква: {entry.PlacedLetter}";

            string wordText = string.IsNullOrWhiteSpace(entry.Word)
                ? "Слово: -"
                : $"Слово: {entry.Word}";

            return wordText + " " +
                   //letterText + " " +
                   //positionText + " " +
                   $"Очки: +{entry.Score}";
        }
    }
}
