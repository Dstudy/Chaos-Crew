using Mirror;
using Mirror.Discovery;
using Script.UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [FormerlySerializedAs("networkGamePlayer")]
    [SerializeField]
    private NetworkManagerLobby networkManager = null;

    [Header("UI")][SerializeField] private GameObject landingPagePanel = null;
    [SerializeField] private GameObject settingsPanel = null;
    [SerializeField] private GameObject enterIpBackground = null;
    [SerializeField] private GameObject enterIpPanel = null;
    [SerializeField] private Button settingButton = null;
    [SerializeField] private Button exitSettingButton = null;
    [SerializeField] private Slider soundSlider = null;
    [SerializeField] private Slider musicSlider = null;
    [SerializeField] private AudioSource musicSource = null;

    [Header("Settings Animation")]
    [SerializeField] private float settingsTweenDuration = 0.4f;
    [SerializeField] private Ease settingsEase = Ease.OutCubic;
    [SerializeField] private float settingsOffscreenPadding = 80f;

    private AudioManager audioManager;
    private RectTransform settingsRect;
    private Vector2 settingsVisiblePos;
    private Vector2 settingsHiddenPos;
    private Tween settingsTween;

    public void HostLobby()
    {
        var manager = EnsureNetworkManager();
        if (manager == null)
        {
            Debug.LogWarning("MainMenu: NetworkManagerLobby missing; cannot host.");
            return;
        }
    
        // NetworkManager.singleton.StartHost();
        manager.StartHost();
        networkManager.networkDiscovery.AdvertiseServer();
        // NetworkManager.singleton.StartServer();
        

        if (landingPagePanel != null)
        {
            landingPagePanel.SetActive(false);
        }

        ShowHostLobbyUI();
    }


    private void OnEnable()
    {
        EnsureNetworkManager();
        audioManager = AudioManager.EnsureExists();
        CacheSettingsPanel();
        RestoreMainMenuUI();
        SetupSettingsUI();
        RegisterMusicSource();
        SubscribeNetworkCallbacks();
    }

    private void OnDisable()
    {
        if (soundSlider != null) soundSlider.onValueChanged.RemoveAllListeners();
        if (musicSlider != null) musicSlider.onValueChanged.RemoveAllListeners();
        if (settingButton != null) settingButton.onClick.RemoveListener(ToggleSettings);
        if (exitSettingButton != null) exitSettingButton.onClick.RemoveListener(CloseSettings);
        settingsTween?.Kill();

        UnsubscribeNetworkCallbacks();
    }

    public void ToggleSettings()
    {
        SetSettingsVisible(!IsSettingsVisible(), false);
    }

    public void CloseSettings()
    {
        SetSettingsVisible(false, false);
    }

    private void SetupSettingsUI()
    {
        RestoreMainMenuUI();

        SetupSettingsButton();

        if (exitSettingButton != null)
        {
            exitSettingButton.onClick.RemoveAllListeners();
            exitSettingButton.onClick.AddListener(CloseSettings);
        }

        if (audioManager == null) return;

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.value = audioManager.SoundVolume;
            soundSlider.onValueChanged.AddListener(audioManager.SetSoundVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.value = audioManager.MusicVolume;
            musicSlider.onValueChanged.AddListener(audioManager.SetMusicVolume);
        }
    }

    private void RegisterMusicSource()
    {
        if (audioManager != null && musicSource != null)
        {
            audioManager.RegisterMusicSource(musicSource, true);
        }
    }

    private void SetupSettingsButton()
    {
        if (settingButton == null) return;

        settingButton.onClick.RemoveListener(ToggleSettings);
        settingButton.onClick.AddListener(ToggleSettings);
    }

    private void RestoreMainMenuUI()
    {
        if (landingPagePanel != null) landingPagePanel.SetActive(true);
        if (enterIpBackground != null) enterIpBackground.SetActive(false);
        // if (enterIpPanel != null) enterIpPanel.SetActive(false);
        SetSettingsVisible(false, true);
    }

    private void SubscribeNetworkCallbacks()
    {
        NetworkManagerLobby.onClientDisconnected -= HandleNetworkReset;
        NetworkManagerLobby.onClientDisconnected += HandleNetworkReset;

        NetworkManagerLobby.OnServerStopped -= HandleNetworkReset;
        NetworkManagerLobby.OnServerStopped += HandleNetworkReset;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        NetworkManagerLobby.onClientDisconnected -= HandleNetworkReset;
        NetworkManagerLobby.OnServerStopped -= HandleNetworkReset;
    }

    private void HandleNetworkReset()
    {
        RestoreMainMenuUI();
    }

    private void ShowHostLobbyUI()
    {
        if (enterIpBackground != null) enterIpBackground.SetActive(true);
        if (enterIpPanel != null) enterIpPanel.SetActive(false);
    }

    private void CacheSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsRect = settingsPanel.GetComponent<RectTransform>();
        if (settingsRect == null) return;

        settingsVisiblePos = settingsRect.anchoredPosition;
        var offset = (settingsRect.rect.height > 0 ? settingsRect.rect.height : 800f) + settingsOffscreenPadding;
        settingsHiddenPos = settingsVisiblePos + Vector2.up * offset;
    }

    private bool IsSettingsVisible()
    {
        return settingsPanel != null && settingsPanel.activeSelf && !(settingsTween != null && settingsTween.IsActive() && settingsTween.IsPlaying() && settingsRect != null && settingsRect.anchoredPosition == settingsHiddenPos);
    }

    private void SetSettingsVisible(bool show, bool instant)
    {
        if (settingsRect == null) CacheSettingsPanel();

        if (settingsPanel == null || settingsRect == null)
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(show);
            return;
        }

        settingsTween?.Kill();

        if (show)
        {
            settingsPanel.SetActive(true);
            if (instant)
            {
                settingsRect.anchoredPosition = settingsVisiblePos;
                return;
            }

            settingsRect.anchoredPosition = settingsHiddenPos;
            settingsTween = settingsRect.DOAnchorPos(settingsVisiblePos, settingsTweenDuration)
                .SetEase(settingsEase);
        }
        else
        {
            if (instant)
            {
                settingsRect.anchoredPosition = settingsHiddenPos;
                settingsPanel.SetActive(false);
                return;
            }

            settingsTween = settingsRect.DOAnchorPos(settingsHiddenPos, settingsTweenDuration)
                .SetEase(settingsEase)
                .OnComplete(() => settingsPanel.SetActive(false));
        }
    }

    private NetworkManagerLobby EnsureNetworkManager()
    {
        if (networkManager != null && networkManager.gameObject != null) return networkManager;

        networkManager = NetworkManager.singleton as NetworkManagerLobby;
        if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManagerLobby>();
        }

        return networkManager;
    }
}
