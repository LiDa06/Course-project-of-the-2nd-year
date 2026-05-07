using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.UI.Common
{
    /// <summary>
    /// Универсальное окно подтверждения для опасных действий:
    /// сброс статистики, удаление аккаунта и т.п.
    /// </summary>
    public class ConfirmationPopup : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private TMP_Text cancelButtonText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);

            Hide();
        }

        public void Show(
            string title,
            string message,
            Action onConfirm,
            Action onCancel = null,
            string confirmText = "Да",
            string cancelText = "Отмена")
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (titleText != null)
                titleText.text = title ?? string.Empty;

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (confirmButtonText != null)
                confirmButtonText.text = string.IsNullOrWhiteSpace(confirmText) ? "Да" : confirmText;

            if (cancelButtonText != null)
                cancelButtonText.text = string.IsNullOrWhiteSpace(cancelText) ? "Отмена" : cancelText;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _onConfirm = null;
            _onCancel = null;
            gameObject.SetActive(false);
        }

        private void Confirm()
        {
            var callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        private void Cancel()
        {
            var callback = _onCancel;
            Hide();
            callback?.Invoke();
        }
    }
}
