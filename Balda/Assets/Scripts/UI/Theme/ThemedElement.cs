using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ThemeColorType
{
    Paper,
    Grid,
    Ink,
    InkLight,
    Accent,
    Cell,
    CellUsed,
    CellActive,
    //OutlineDark,
    //OutlineLight
}

public class ThemedElement : MonoBehaviour
{
    public ThemeColorType colorType;

    private Image image;
    private TMP_Text text;
    private Outline outline;

    private void Awake()
    {
        image = GetComponent<Image>();
        text = GetComponent<TMP_Text>();
        outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        ApplyTheme();
        ThemeManager.OnThemeChanged += ApplyTheme;
    }

    private void OnDisable()
    {
        ThemeManager.OnThemeChanged -= ApplyTheme;
    }

    public void ApplyTheme()
    {
        if (ThemeManager.Instance == null)
            return;

        Color color = ThemeManager.Instance.GetColor(colorType);

        if (image != null)
            image.color = color;

        if (text != null)
            text.color = color;

        if (outline != null)
            outline.effectColor = color;
    }
}
