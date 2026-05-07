using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Balda.Features.Game.Domain;
using Balda.Infrastructure.Theme;

namespace Balda.Features.Game.UI
{
    public class BoardCellView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private Image background;

        public int Row { get; private set; }
        public int Col { get; private set; }

        public string Letter { get; private set; } = "";
        public bool IsStartLetter { get; private set; }

        private bool isInteractable = true;
        private System.Action<int, int> onClicked;

        private bool isSelected;
        private bool isPlacedLetterCell;

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

        private void OnEnable()
        {
            ThemeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void OnDisable()
        {
            ThemeManager.ThemeChanged -= ApplyTheme;
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

            isSelected = false;
            isPlacedLetterCell = false;
            ApplyTheme();
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Letter);
        }

        public void SetSelectionState(bool selected, bool placedLetterCell)
        {
            isSelected = selected;
            isPlacedLetterCell = placedLetterCell;
            ApplyTheme();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            onClicked?.Invoke(Row, Col);
        }

        private void ApplyTheme()
        {
            if (ThemeManager.Instance == null)
                return;

            if (letterText != null)
                letterText.color = ThemeManager.Instance.GetColor(ThemeColorType.Ink);

            if (background == null)
                return;

            if (isPlacedLetterCell)
            {
                background.color = ThemeManager.Instance.GetColor(ThemeColorType.CellActive);
                return;
            }

            if (isSelected)
            {
                background.color = ThemeManager.Instance.GetColor(ThemeColorType.CellActive);
                return;
            }

            background.color = IsStartLetter
                ? ThemeManager.Instance.GetColor(ThemeColorType.CellUsed)
                : ThemeManager.Instance.GetColor(ThemeColorType.Cell);
        }

        private void Clear()
        {
            Letter = "";
            IsStartLetter = false;
            isSelected = false;
            isPlacedLetterCell = false;

            if (letterText != null)
                letterText.text = "";

            ApplyTheme();
        }
    }
}