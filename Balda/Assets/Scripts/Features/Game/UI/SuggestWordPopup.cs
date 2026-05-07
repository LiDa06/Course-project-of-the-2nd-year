using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.Features.Game.UI
{
    public class SuggestWordPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text wordInput;
        [SerializeField] private Button suggestButton;
        [SerializeField] private Button cancelButton;

        private Action<string> submitCallback;
        private Action cancelCallback;

        private void Awake()
        {
            if (suggestButton != null)
            {
                suggestButton.onClick.RemoveAllListeners();
                suggestButton.onClick.AddListener(OnSuggestClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        public void Show(string word, Action<string> onSubmit, Action onCancel)
        {
            submitCallback = onSubmit;
            cancelCallback = onCancel;

            if (wordInput != null)
                wordInput.text = word ?? "";

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            submitCallback = null;
            cancelCallback = null;
        }

        private void OnSuggestClicked()
        {
            string word = wordInput != null ? wordInput.text.Trim() : "";
            submitCallback?.Invoke(word);
        }

        private void OnCancelClicked()
        {
            cancelCallback?.Invoke();
        }
    }
}