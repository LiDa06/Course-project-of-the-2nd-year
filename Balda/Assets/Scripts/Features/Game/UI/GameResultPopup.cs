using System;
using Balda.Features.Game.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class GameResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button backButton;

        private Action onNewGame;
        private Action onBackToMenu;

        private void Awake()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(HandleNewGame);

            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(HandleBackToMenu);

            if (backButton != null)
                backButton.onClick.AddListener(Hide);

            HideImmediate();
        }

        public void Show(GameSession session, Action onNewGameCallback, Action onBackToMenuCallback)
        {
            onNewGame = onNewGameCallback;
            onBackToMenu = onBackToMenuCallback;

            if (titleText != null)
                titleText.text = BuildTitle(session);

            if (summaryText != null)
                summaryText.text = BuildSummary(session);

            SetVisible(true);
        }

        public void Hide()
        {
            onNewGame = null;
            onBackToMenu = null;
            SetVisible(false);
        }

        private void HideImmediate()
        {
            onNewGame = null;
            onBackToMenu = null;
            SetVisible(false);
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

        private void HandleNewGame()
        {
            var callback = onNewGame;
            Hide();
            callback?.Invoke();
        }

        private void HandleBackToMenu()
        {
            var callback = onBackToMenu;
            Hide();
            callback?.Invoke();
        }

        private static string BuildTitle(GameSession session)
        {
            if (session == null)
                return "Игра окончена";

            return session.WinnerIndex switch
            {
                0 => $"Победитель: { GetSafeName(session.PlayerOneDisplayName, "Игрок 1") }",
                1 => $"Победитель: { GetSafeName(session.PlayerTwoDisplayName, "Игрок 2") }",
                _ => "Ничья"
            };
        }

        private static string BuildSummary(GameSession session)
        {
            if (session == null)
                return "";

            string playerOneName = GetSafeName(session.PlayerOneDisplayName, "Игрок 1");
            string playerTwoName = GetSafeName(session.PlayerTwoDisplayName, "Игрок 2");
            string lastWord = string.IsNullOrWhiteSpace(session.LastAcceptedWord)
                ? "-"
                : session.LastAcceptedWord;

            return $"{playerOneName}: {session.PlayerOneScore}\n" +
                $"{playerTwoName}: {session.PlayerTwoScore}\n" +
                $"Последнее слово:\n{lastWord} (+{session.LastAcceptedScore})";
        }

        private static string GetSafeName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
