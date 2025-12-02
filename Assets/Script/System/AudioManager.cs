using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    public float SoundVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;

    private const string SoundVolumeKey = "SoundVolume";
    private const string MusicVolumeKey = "MusicVolume";

    public event Action<float> OnSoundVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;

    public static AudioManager EnsureExists()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("AudioManager");
        return go.AddComponent<AudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        ApplyVolumes();
    }

    private void LoadVolumes()
    {
        SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public void RegisterMusicSource(AudioSource source, bool playIfStopped = false)
    {
        if (source == null)
        {
            return;
        }

        musicSource = source;
        ApplyMusicVolume();

        if (playIfStopped && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void SetSoundVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(newValue, SoundVolume)) return;

        SoundVolume = newValue;
        PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        ApplyVolumes();
        OnSoundVolumeChanged?.Invoke(SoundVolume);
    }

    public void SetMusicVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(newValue, MusicVolume)) return;

        MusicVolume = newValue;
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
        OnMusicVolumeChanged?.Invoke(MusicVolume);
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = SoundVolume;
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = MusicVolume;
        }
    }
}
