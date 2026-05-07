using System;
using UnityEngine;

namespace Balda.Infrastructure.Audio
{
    public enum AudioType
    {
        On,
        Off
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioType CurrentAudio { get; private set; } = AudioType.On;

        public static event Action AudioChanged;

        [Header("Audio Theme")]
        [SerializeField] private AudioTheme audioTheme = new AudioTheme();

        [Header("Sources")]
        [Tooltip("Можно не назначать: AudioManager сам найдёт или создаст AudioSource на этом объекте.")]
        [SerializeField] private AudioSource backgroundSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureBackgroundSource();
            ApplyThemeToSource();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            EnsureBackgroundSource();
            ApplyThemeToSource();
            UpdateBackgroundPlayback();
        }

        public void Apply(AudioType audio)
        {
            bool changed = CurrentAudio != audio;
            CurrentAudio = audio;

            EnsureBackgroundSource();
            ApplyThemeToSource();
            UpdateBackgroundPlayback();

            if (changed)
                AudioChanged?.Invoke();
        }

        public void Refresh()
        {
            EnsureBackgroundSource();
            ApplyThemeToSource();
            UpdateBackgroundPlayback();
        }

        private void EnsureBackgroundSource()
        {
            if (backgroundSource == null)
                backgroundSource = GetComponent<AudioSource>();

            if (backgroundSource == null)
                backgroundSource = gameObject.AddComponent<AudioSource>();

            backgroundSource.playOnAwake = false;
            backgroundSource.spatialBlend = 0f;
        }

        private void ApplyThemeToSource()
        {
            if (backgroundSource == null)
                return;

            AudioClip clip = audioTheme != null ? audioTheme.BackgroundMusic : null;

            if (backgroundSource.clip != clip)
                backgroundSource.clip = clip;

            backgroundSource.loop = audioTheme == null || audioTheme.LoopBackgroundMusic;
            backgroundSource.volume = audioTheme != null ? audioTheme.BackgroundVolume : 1f;
            backgroundSource.mute = CurrentAudio == AudioType.Off;
        }

        private void UpdateBackgroundPlayback()
        {
            if (backgroundSource == null)
                return;

            bool shouldPlay =
                CurrentAudio == AudioType.On &&
                audioTheme != null &&
                audioTheme.PlayBackgroundMusic &&
                audioTheme.HasBackgroundMusic;

            backgroundSource.mute = !shouldPlay;

            if (shouldPlay)
            {
                backgroundSource.volume = audioTheme.BackgroundVolume;

                if (!backgroundSource.isPlaying)
                    backgroundSource.Play();
            }
            else if (backgroundSource.isPlaying)
            {
                backgroundSource.Stop();
            }
        }
    }
}
