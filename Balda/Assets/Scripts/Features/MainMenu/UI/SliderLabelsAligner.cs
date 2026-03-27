using UnityEngine;
using UnityEngine.UI;
using Balda.Infrastructure.LocalStorage;

namespace Balda.Features.MainMenu.UI
{
    public class SliderLabelsAligner : MonoBehaviour
    {
        [SerializeField] private RectTransform labelsParent;
        [SerializeField] private Slider slider;

        private void Start()
        {
            Align();
        }

        public void RefreshFromSettings()
        {
            if (LocalSettings.Instance == null)
                LocalSettings.Load();

            slider.value = Mathf.Clamp(LocalSettings.Instance.BoardSize, slider.minValue, slider.maxValue);
            Align();
        }

        public int GetFieldSize()
        {
            return Mathf.RoundToInt(slider.value);
        }

        private void OnRectTransformDimensionsChange()
        {
            Align();
        }

        private void Align()
        {
            if (labelsParent == null || slider == null)
                return;

            float width = labelsParent.rect.width;
            int steps = Mathf.RoundToInt(slider.maxValue - slider.minValue);

            for (int i = 0; i < labelsParent.childCount; i++)
            {
                RectTransform label = labelsParent.GetChild(i) as RectTransform;
                if (label == null) continue;

                float t = (float)i / steps;
                float x = t * width;

                label.anchoredPosition = new Vector2(x, 0);
            }
        }
    }
}