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
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button exitUI;
    
    private bool gameEnded;
    private bool exitHooked;
    private bool warnedMissingExit;

    private void Awake()
    {
        EnsureUIReferences();
        ResetUIState();
        WireExitButton();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureUIReferences();
        ResetUIState();
        ObserverManager.Register(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Register(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Register(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Register(GAME_LOST, (Action<Player>)HandlePlayerDied);
        WireExitButton();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ObserverManager.Unregister(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Unregister(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_LOST, (Action<Player>)HandlePlayerDied);
        exitHooked = false;
    }

    public void ShowGameOverUI()
    {
        EnsureUIReferences();
        if (gameOverUI == null)
        {
            Debug.LogWarning("UIController: gameOverUI is not assigned, cannot show Game Over.");
            return;
        }

        gameOverUI.SetActive(true);
        Debug.Log("UIController: Game Over UI shown");
    }

    public void ShowWinUI()
    {
        EnsureUIReferences();
        if (winUI == null)
        {
            Debug.LogWarning("UIController: winUI is not assigned, cannot show Win UI.");
            return;
        }

        winUI.SetActive(true);
        Debug.Log("UIController: Win UI shown");
    }

    private void HandlePlayerDied(Player deadPlayer)
    {
        Debug.Log($"UIController: HandlePlayerDied received for {deadPlayer?.name ?? "unknown"}; gameEnded={gameEnded}");
        if (gameEnded) return;
        gameEnded = true;
        ShowGameOverUI();
    }

    private void HandleGameWon()
    {
        Debug.Log($"UIController: HandleGameWon received; gameEnded={gameEnded}");
        if (gameEnded) return;
        gameEnded = true;
        ShowWinUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUIReferences();
        ResetUIState();
        WireExitButton();
    }

    private void ResetUIState()
    {
        gameEnded = false;
        if (gameOverUI != null && gameOverUI.activeSelf)
        {
            gameOverUI.SetActive(false);
        }
        if (winUI != null && winUI.activeSelf)
        {
            winUI.SetActive(false);
        }
    }

    private void EnsureUIReferences()
    {
        TryAutoAssignPanel(ref gameOverUI, "GameOverUI", "gameover");
        TryAutoAssignPanel(ref winUI, "WinUI", "win");
    }

    private void TryAutoAssignPanel(ref GameObject target, string tagName, string nameHint)
    {
        if (target != null) return;

        var candidates = Resources.FindObjectsOfTypeAll<GameObject>();
        var match = candidates.FirstOrDefault(go =>
            go != null &&
            (go.CompareTag(tagName) ||
             go.name.IndexOf(nameHint, StringComparison.OrdinalIgnoreCase) >= 0));

        if (match != null)
        {
            target = match;
            Debug.Log($"UIController: auto-assigned {nameHint} panel: {match.name}");
        }
        else
        {
            Debug.LogWarning($"UIController: Could not auto-assign {nameHint} panel (tag {tagName}).");
        }
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
