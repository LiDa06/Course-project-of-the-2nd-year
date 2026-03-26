using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VectorGraphics;

public enum ThemeColorType
{
    Paper,
    Grid,
    Ink,
    InkLight,
    Accent,
    Cell,
    CellUsed,
    CellActive
}

public class ThemedElement : MonoBehaviour
{
    public ThemeColorType colorType;

    private Image image;
    private TMP_Text text;
    private SVGImage icon;
    private Outline outline;

    private void Awake()
    {
        image = GetComponent<Image>();
        text = GetComponent<TMP_Text>();
        icon = GetComponent<SVGImage>();
        outline = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        ApplyTheme();
        ThemeManager.ThemeChanged += ApplyTheme;
    }

    private void OnDisable()
    {
        ThemeManager.ThemeChanged -= ApplyTheme;
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

        if (icon != null)
            icon.color = color;

        if (outline != null)
            outline.effectColor = color;
    }
}
