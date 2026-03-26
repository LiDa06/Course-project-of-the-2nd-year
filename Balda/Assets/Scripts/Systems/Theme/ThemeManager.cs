using UnityEngine;
using System;

public enum ThemeType
{
    Light,
    Dark
}

public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    public ThemeType CurrentTheme { get; private set; }

    public static event Action ThemeChanged;

    [Header("Light Theme")]
    public Color lightPaper = new Color32(245, 241, 232, 255);
    public Color lightGrid = new Color32(212, 207, 196, 255);
    public Color lightInk = new Color32(44, 44, 44, 255);
    public Color lightInkLight = new Color32(102, 102, 102, 255);
    public Color lightAccent = new Color32(139, 69, 19, 255);
    public Color lightCell = Color.white;
    public Color lightCellUsed = new Color32(232, 228, 218, 255);
    public Color lightCellActive = new Color32(255, 215, 0, 255);

    [Header("Dark Theme")]
    public Color darkPaper = new Color32(13, 13, 13, 255);
    public Color darkGrid = new Color32(85, 85, 85, 255);
    public Color darkInk = new Color32(240, 240, 240, 255);
    public Color darkInkLight = new Color32(184, 184, 184, 255);
    public Color darkAccent = new Color32(232, 181, 106, 255);
    public Color darkCell = new Color32(31, 31, 31, 255);
    public Color darkCellUsed = new Color32(42, 42, 42, 255);
    public Color darkCellActive = new Color32(139, 117, 32, 255);

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void Apply(ThemeType theme)
    {
        if (CurrentTheme == theme)
            return;

        CurrentTheme = theme;
        ThemeChanged?.Invoke();
    }

    public Color GetColor(ThemeColorType type)
    {
        return type switch
        {
            ThemeColorType.Paper => CurrentTheme == ThemeType.Light ? lightPaper : darkPaper,
            ThemeColorType.Grid => CurrentTheme == ThemeType.Light ? lightGrid : darkGrid,
            ThemeColorType.Ink => CurrentTheme == ThemeType.Light ? lightInk : darkInk,
            ThemeColorType.InkLight => CurrentTheme == ThemeType.Light ? lightInkLight : darkInkLight,
            ThemeColorType.Accent => CurrentTheme == ThemeType.Light ? lightAccent : darkAccent,
            ThemeColorType.Cell => CurrentTheme == ThemeType.Light ? lightCell : darkCell,
            ThemeColorType.CellUsed => CurrentTheme == ThemeType.Light ? lightCellUsed : darkCellUsed,
            ThemeColorType.CellActive => CurrentTheme == ThemeType.Light ? lightCellActive : darkCellActive,
            _ => Color.white
        };
    }
}
