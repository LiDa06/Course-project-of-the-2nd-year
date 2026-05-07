using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Balda.UI.Common
{
    public class MessagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button okButton;

        private void Awake()
        {
            if (okButton != null)
                okButton.onClick.AddListener(Hide);
        }

        public void Show(string title, string message)
        {
            if (titleText != null)
                titleText.text = title;

            if (messageText != null)
                messageText.text = message;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}