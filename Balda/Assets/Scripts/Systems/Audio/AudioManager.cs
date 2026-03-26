using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public enum AudioType
{
    On,
    Off
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioType CurrentAudio { get; private set; }

    public static event Action AudioChanged;

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

    public void Apply(AudioType audio)
    {
        if (CurrentAudio == audio)
            return;

        CurrentAudio = audio;
        AudioChanged?.Invoke();
    }

}