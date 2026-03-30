using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class SavedGamePromptPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text messageText;

        private Action onContinue;
        private Action onNewGame;
        private Action onClose;

        private void Awake()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinue);

            if (newGameButton != null)
                newGameButton.onClick.AddListener(HandleNewGame);

            if (closeButton != null)
                closeButton.onClick.AddListener(HandleClose);

            HideImmediate();
        }

        public void Show(Action continueCallback, Action newGameCallback, Action closeCallback = null)
        {
            onContinue = continueCallback;
            onNewGame = newGameCallback;
            onClose = closeCallback;

            if (messageText != null)
                messageText.text = "Найдена сохранённая игра. Продолжить её или начать новую?";

            if (root != null)
                root.SetActive(true);
            else
                gameObject.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (root != null)
                root.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        private void HandleContinue()
        {
            var callback = onContinue;
            CleanupAndHide();
            callback?.Invoke();
        }

        private void HandleNewGame()
        {
            var callback = onNewGame;
            CleanupAndHide();
            callback?.Invoke();
        }

        private void HandleClose()
        {
            var callback = onClose;
            CleanupAndHide();
            callback?.Invoke();
        }

        private void CleanupAndHide()
        {
            onContinue = null;
            onNewGame = null;
            onClose = null;
            Hide();
        }
    }
}
