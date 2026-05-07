using System;
using UnityEngine;

namespace Balda.Infrastructure.Audio
{
    [Serializable]
    public class AudioTheme
    {
        [Header("Background music")]
        [Tooltip("Фоновая музыка/звук приложения. Перетащи сюда свой AudioClip.")]
        [SerializeField] private AudioClip backgroundMusic;

        [Tooltip("Громкость фоновой музыки при включённом звуке.")]
        [SerializeField, Range(0f, 1f)] private float backgroundVolume = 0.35f;

        [Tooltip("Зациклить фоновую музыку.")]
        [SerializeField] private bool loopBackgroundMusic = true;

        [Tooltip("Запускать фоновую музыку, когда настройка звука включена.")]
        [SerializeField] private bool playBackgroundMusic = true;

        public AudioClip BackgroundMusic => backgroundMusic;
        public float BackgroundVolume => Mathf.Clamp01(backgroundVolume);
        public bool LoopBackgroundMusic => loopBackgroundMusic;
        public bool PlayBackgroundMusic => playBackgroundMusic;
        public bool HasBackgroundMusic => backgroundMusic != null;
    }
}
