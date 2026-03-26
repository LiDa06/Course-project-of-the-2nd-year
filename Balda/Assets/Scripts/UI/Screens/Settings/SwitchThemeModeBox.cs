using Assets.Scripts.Data;
using Assets.Scripts.LocalData;
using TMPro;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Screens.Settings
{
    public class SwitchThemeModeBox : MonoBehaviour
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private SVGImage icon;
        [SerializeField] private Image background;


        [SerializeField] private Vector2 onPosition;
        [SerializeField] private Vector2 offPosition;

        [SerializeField] private SVGImage onIcon;
        [SerializeField] private SVGImage offIcon;

        [SerializeField] private Image onImage;
        [SerializeField] private Image offImage;

        [SerializeField] private TMP_Text text;

        private void OnEnable()
        {
            ApplyFromSettings();
            ThemeManager.ThemeChanged += OnThemeChanged;
        }
        private void OnDisable()
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
        }

        private void ApplyFromSettings()
        {
            Apply(LocalSettings.Instance.Theme);
        }

        public void OnThemeChanged()
        {
            Apply(ThemeManager.Instance.CurrentTheme);
        }

        private void Apply(ThemeType theme)
        {
            bool flag = theme == ThemeType.Dark;

            onImage.enabled = !flag;
            offImage.enabled = flag;
            onIcon.enabled = !flag;
            offIcon.enabled = flag;

            handle.anchoredPosition = flag ? onPosition : offPosition;
            icon = flag ? onIcon : offIcon;
            background = flag ? onImage : offImage;

            onImage.enabled = flag;
            offImage.enabled = !flag;
            onIcon.enabled = flag;
            offIcon.enabled = !flag;

            text.text = flag ? "Включена" : "Выключена";
        }
    }
}
