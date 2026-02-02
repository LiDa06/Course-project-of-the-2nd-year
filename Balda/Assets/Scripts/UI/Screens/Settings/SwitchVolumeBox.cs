using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Screens.Settings
{
    public class SwitchVolumeBox : MonoBehaviour
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

        public void Switch(bool isOn)
        {
            handle.anchoredPosition = isOn ? onPosition : offPosition;
            icon = isOn ? onIcon : offIcon;
            background = isOn ? onImage : offImage;
        }
    }
}
