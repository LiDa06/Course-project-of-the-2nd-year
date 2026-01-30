using UnityEngine;
using UnityEngine.UI;

public class SliderLabelsAligner : MonoBehaviour
{
    [SerializeField] private RectTransform labelsParent;
    [SerializeField] private Slider slider;

    public void Start()
    {
        OnRectTransformDimensionsChange();
    }

    public int GetFieldSize()
    {
        return Mathf.RoundToInt(slider.value);
    }

    void OnRectTransformDimensionsChange()
    {
        Align();
    }

    void Align()
    {
        float width = labelsParent.rect.width;
        int steps = Mathf.RoundToInt(slider.maxValue - slider.minValue);

        for (int i = 0; i < labelsParent.childCount; i++)
        {
            RectTransform label = labelsParent.GetChild(i) as RectTransform;

            float t = (float)i / steps;      
            float x = t * width;

            label.anchoredPosition = new Vector2(x, 0);
        }
    }
}
