using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Mirror;
using Script.UI;
using static CONST;

public class UIController : MonoBehaviour
{
    public static UIController instance;
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button exitUI;
    
    private bool gameEnded;
    private bool exitHooked;
    private bool warnedMissingExit;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        WireExitButton();
    }

    private void OnEnable()
    {
        ObserverManager.Register(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Register(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Register(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Register(GAME_LOST, (Action<Player>)HandlePlayerDied);
        WireExitButton();
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Unregister(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_LOST, (Action<Player>)HandlePlayerDied);
        exitHooked = false;
    }

    public void ShowGameOverUI()
    {
        gameOverUI?.SetActive(true);
        Debug.Log("Game Over UI shown");
    }

    public void ShowWinUI()
    {
        winUI?.SetActive(true);
        Debug.Log("Win UI shown");
    }

    private void HandlePlayerDied(Player deadPlayer)
    {
        if (gameEnded) return;
        gameEnded = true;
        ShowGameOverUI();
    }

    private void HandleGameWon()
    {
        if (gameEnded) return;
        gameEnded = true;
        ShowWinUI();
    }

    private void WireExitButton()
    {
        if (exitUI == null)
        {
            if (!warnedMissingExit)
            {
                TryAutoAssignExitButton();
                if (exitUI == null)
                {
                    Debug.LogWarning("UIController: exitUI is not assigned.");
                    warnedMissingExit = true;
                }
            }
            return;
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("UIController: No EventSystem in scene; UI clicks will be blocked.");
        }

        exitUI.onClick.RemoveAllListeners();
        exitUI.onClick.AddListener(ExitToMenu);
        exitHooked = true;
        Debug.Log($"UIController: exit button wired to ExitToMenu. Active={exitUI.gameObject.activeInHierarchy}, Enabled={exitUI.enabled}, Interactable={exitUI.interactable}");
    }

    private void TryAutoAssignExitButton()
    {
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        var match = buttons.FirstOrDefault(b =>
            b != null &&
            (b.CompareTag("ExitButton") || b.name.IndexOf("exit", StringComparison.OrdinalIgnoreCase) >= 0));

        if (match != null)
        {
            exitUI = match;
            warnedMissingExit = false;
            Debug.Log($"UIController: auto-assigned exit button: {match.name}");
        }
    }

    public void ExitToMenu()
    {
        Debug.Log("UIController: ExitToMenu clicked.");

        if (PlayerManager.instance != null && PlayerManager.instance.localPlayer != null)
        {
            PlayerManager.instance.localPlayer.CmdRequestShutdown();
            return;
        }

        var lobby = NetworkManager.singleton as NetworkManagerLobby;
        if (lobby != null)
        {
            lobby.ShutdownAndReturnToMenu();
            return;
        }

        SceneManager.LoadScene(0);
    }
}
