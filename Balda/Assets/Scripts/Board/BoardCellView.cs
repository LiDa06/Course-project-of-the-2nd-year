using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardCellView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image background;

    public int Row { get; private set; }
    public int Col { get; private set; }

    public char Letter { get; private set; } = '\0';
    public bool IsBlocked { get; private set; }

    private System.Action<BoardCellView> onClicked;

    public void Init(int row, int col, System.Action<BoardCellView> clickCallback)
    {
        Row = row;
        Col = col;
        onClicked = clickCallback;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClicked?.Invoke(this));

        SetLetter('\0', false);
    }

    public void SetLetter(char letter, bool isBlocked)
    {
        Letter = letter;
        IsBlocked = isBlocked;

        if (letter == '\0')
            letterText.text = "";
        else
            letterText.text = char.ToUpperInvariant(letter).ToString();
    }

    public bool IsEmpty()
    {
        return Letter == '\0';
    }
}