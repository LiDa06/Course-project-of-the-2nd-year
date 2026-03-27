using Balda.UI.Common;
using Balda.Core.Navigation;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI;
using Balda.Infrastructure.Audio;
using Balda.Infrastructure.LocalStorage;
using AudioType = Balda.Infrastructure.Audio.AudioType;

namespace Balda.Features.Settings.UI
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

        private void OnEnable()
        {
            ApplyFromSettings();
            AudioManager.AudioChanged += OnAudioChanged;
        }
        private void OnDisable()
        {
            AudioManager.AudioChanged -= OnAudioChanged;
        }

        private void ApplyFromSettings()
        {
            Apply(LocalSettings.Instance.Audio);
        }

        public void OnAudioChanged()
        {
            Apply(AudioManager.Instance.CurrentAudio);
        }

        private void Apply(AudioType audio)
        {
            bool flag = audio == AudioType.On;

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
        }
    }
}
