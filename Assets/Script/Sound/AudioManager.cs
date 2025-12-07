using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static CONST;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectSource;
    // [SerializeField] private AudioSource audioSource;
    
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectSlider;
    
    [Header("UI Click")]
    [SerializeField] private List<AudioClip> clickSounds;

    [Header("Sound List")]
    [SerializeField] private List<AudioClip> collideSounds;
    [SerializeField] private List<AudioClip> swordSounds;
    [SerializeField] private List<AudioClip> staffSounds;
    [SerializeField] private AudioClip hammerSound;
    [SerializeField] private AudioClip useHealSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip shieldUpSound;
    [SerializeField] private AudioClip shieldBlockSound;

    [SerializeField] private AudioClip playerDefeated;
    
    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;

    private void OnEnable()
    {
        ObserverManager.Register(PLAYER_DIED, (Action<Player>)PlayPlayerDieSound);
        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterAllButtons();
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(PLAYER_DIED, (Action<Player>)PlayPlayerDieSound);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    public float SoundVolume { get; private set; } = 1f;
    public float MusicVolume { get; private set; } = 1f;

    private const string SoundVolumeKey = "SoundVolume";
    private const string MusicVolumeKey = "MusicVolume";
    

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

    private void Start()
    {
        StartBackgroundMusic();
    }

    private void StartBackgroundMusic()
    {
        musicSource.clip = backgroundMusic;
        musicSource.Play();
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
    }

    public void SetMusicVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);
        if (Mathf.Approximately(newValue, MusicVolume)) return;

        MusicVolume = newValue;
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        ApplyMusicVolume();
    }

    private void ApplyVolumes()
    {
        if (effectSource != null)
        {
            effectSource.volume = SoundVolume;
        }
        ApplyMusicVolume();
    }

    private void ApplyMusicVolume()
    {
        if (musicSource != null)
        {
            musicSource.volume = MusicVolume;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllButtons();
    }

    public void RegisterAllButtons()
    {
        // Find all buttons in the scene (including inactive) and hook the click sound.
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (Button btn in allButtons)
        {
            if (btn == null) continue;

            btn.onClick.RemoveListener(PlayClickSound);
            btn.onClick.AddListener(PlayClickSound);
        }
    }

    public void PlayClickSound()
    {
        if (clickSounds == null || clickSounds.Count == 0) return;

        AudioSource audioSource = effectSource;
        if (audioSource == null) return;

        AudioClip clip = clickSounds[UnityEngine.Random.Range(0, clickSounds.Count)];
        audioSource.PlayOneShot(clip, audioSource.volume);
    }

    public void PlayCollideSound(AudioSource source = null)
    {
        if (collideSounds == null || collideSounds.Count == 0) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        AudioClip clip = collideSounds[UnityEngine.Random.Range(0, collideSounds.Count)];
        audioSource.PlayOneShot(clip, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlaySwordSound(AudioSource source = null)
    {
        if (swordSounds == null || swordSounds.Count == 0) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        AudioClip clip = swordSounds[UnityEngine.Random.Range(0, swordSounds.Count)];
        audioSource.PlayOneShot(clip, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayStaffSound(AudioSource source = null)
    {
        if (staffSounds == null || staffSounds.Count == 0) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        AudioClip clip = staffSounds[UnityEngine.Random.Range(0, staffSounds.Count)];
        audioSource.PlayOneShot(clip, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayHammerSound(AudioSource source = null)
    {
        if (hammerSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(hammerSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayUseHealSound(AudioSource source = null)
    {
        if (healSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(useHealSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayHealSound(AudioSource source = null)
    {
        if (healSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(healSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayEnemyHitSound(AudioSource source = null)
    {
        if (enemyHitSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(enemyHitSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayShieldUpSound(AudioSource source = null)
    {
        if (shieldUpSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(shieldUpSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayShieldBlockSound(AudioSource source = null)
    {
        if (shieldBlockSound == null) return;
        
        AudioSource audioSource = source != null ? source : effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(shieldBlockSound, effectSource != null ? effectSource.volume : SoundVolume);
    }
    
    public void PlayPlayerDieSound(Player _)
    {
        if (healSound == null) return;
        
        AudioSource audioSource = effectSource;
        if (audioSource == null) return;
        
        audioSource.PlayOneShot(playerDefeated, effectSource != null ? effectSource.volume : SoundVolume);
    }
}
