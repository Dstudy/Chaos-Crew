using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [FormerlySerializedAs("networkGamePlayer")] [SerializeField] private NetworkManagerLobby networkManager = null;

    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;
    [SerializeField] private GameObject settingsPanel = null;
    [SerializeField] private GameObject enterIpBackground = null;
    [SerializeField] private GameObject enterIpPanel = null;
    [SerializeField] private Button settingButton = null;
    [SerializeField] private Slider soundSlider = null;
    [SerializeField] private Slider musicSlider = null;
    [SerializeField] private AudioSource musicSource = null;


    private AudioManager audioManager;

    public void HostLobby()
    {
        var manager = EnsureNetworkManager();
        if (manager == null)
        {
            Debug.LogWarning("MainMenu: NetworkManagerLobby missing; cannot host.");
            return;
        }

        manager.StartHost();

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
        UnsubscribeNetworkCallbacks();
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    private void SetupSettingsUI()
    {
        RestoreMainMenuUI();

        SetupSettingsButton();

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
        if (enterIpPanel != null) enterIpPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
