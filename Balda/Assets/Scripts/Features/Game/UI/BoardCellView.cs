using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Balda.Features.Game.Domain;

namespace Balda.Features.Game.UI
{
    public class BoardCellView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private Image background;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color startLetterColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.72f, 0.88f, 1f, 1f);
        [SerializeField] private Color placedLetterColor = new Color(0.95f, 0.87f, 0.53f, 1f);

        public int Row { get; private set; }
        public int Col { get; private set; }

        public string Letter { get; private set; } = "";
        public bool IsStartLetter { get; private set; }

        private bool isInteractable = true;
        private System.Action<int, int> onClicked;

        private void Awake()
        {
            var rootImage = GetComponent<Image>();
            if (rootImage != null)
                rootImage.raycastTarget = true;

            if (letterText != null)
                letterText.raycastTarget = false;

            if (background != null)
                background.raycastTarget = false;

            var childImages = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < childImages.Length; i++)
            {
                if (childImages[i].gameObject != gameObject)
                    childImages[i].raycastTarget = false;
            }
        }

        public void Init(int row, int col, System.Action<int, int> clickCallback)
        {
            Row = row;
            Col = col;
            onClicked = clickCallback;

            Clear();
        }

        public void Render(BoardCellData data)
        {
            if (data == null)
            {
                Clear();
                return;
            }

            Letter = data.Letter ?? "";
            IsStartLetter = data.IsStartLetter;

            if (letterText != null)
                letterText.text = Letter;

            ApplyVisual(false, false);
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Letter);
        }

        public void SetSelectionState(bool isSelected, bool isPlacedLetterCell)
        {
            ApplyVisual(isSelected, isPlacedLetterCell);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            onClicked?.Invoke(Row, Col);
        }

        private void ApplyVisual(bool isSelected, bool isPlacedLetterCell)
        {
            if (background == null)
                return;

            if (isPlacedLetterCell)
            {
                background.color = placedLetterColor;
                return;
            }

            if (isSelected)
            {
                background.color = selectedColor;
                return;
            }

            background.color = IsStartLetter ? startLetterColor : normalColor;
        }

        private void Clear()
        {
            Letter = "";
            IsStartLetter = false;

            if (letterText != null)
                letterText.text = "";

            ApplyVisual(false, false);
        }
    }
}