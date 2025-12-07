using System;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;
using Mirror;
using Script.UI;
using static CONST;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    public GameObject gameOverUI;
    public GameObject winUI;
    public Button exitUI;
    public Button homeUI;
    public Button nextUI;

    [Header("Round Notices")]
    public TextMeshProUGUI winNoticeText;
    public TextMeshProUGUI loseNoticeText;
    public List<string> winNotices;
    public List<string> loseNotices;

    [Header("Timing")]
    [SerializeField] private float uiShowDelaySeconds = 3f;

    [Header("Pop-up Animation")]
    [SerializeField] private float popupStartScale = 0.5f;
    [SerializeField] private float popupEndScale = 1f;
    [SerializeField] private float popupDuration = 0.35f;
    [SerializeField] private Ease popupEase = Ease.OutBack;

    private bool gameEnded;
    private bool warnedMissingExit;
    private bool warnedMissingNext;
    private Coroutine winUICoroutine;
    private Coroutine gameOverUICoroutine;

    private void Awake()
    {
        EnsureUIReferences();
        ResetUIState();
        WireExitButton();
    }
    public void OnHomeButtonClicked()
    {
        ExitToMenu();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureUIReferences();
        ResetUIState();
        RoundManager.OnRoundEndedClient += HandleRoundEnded;
        RoundManager.OnRoundStartedClient += HandleRoundStarted;
        ObserverManager.Register(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Register(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Register(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Register(GAME_LOST, (Action<Player>)HandlePlayerDied);
        WireExitButton();
        WireNextButton();
    }


    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RoundManager.OnRoundEndedClient -= HandleRoundEnded;
        RoundManager.OnRoundStartedClient -= HandleRoundStarted;
        StopPendingUIRoutines();
        ObserverManager.Unregister(PLAYER_DIED, (Action<Player>)HandlePlayerDied);
        ObserverManager.Unregister(ALL_ENEMIES_DEFEATED, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_WON, (Action)HandleGameWon);
        ObserverManager.Unregister(GAME_LOST, (Action<Player>)HandlePlayerDied);
    }

    public void ShowGameOverUI()
    {
        EnsureUIReferences();
        if (gameOverUI == null)
        {
            Debug.LogWarning("UIController: gameOverUI is not assigned, cannot show Game Over.");
            return;
        }

        RestartGameOverRoutine();
    }

    private void RestartGameOverRoutine()
    {
        if (gameOverUICoroutine != null)
        {
            StopCoroutine(gameOverUICoroutine);
        }
        gameOverUICoroutine = StartCoroutine(ShowGameOverUIDelayed());
    }

    private IEnumerator ShowGameOverUIDelayed()
    {
        yield return new WaitForSeconds(uiShowDelaySeconds);
        ToggleNextButton(false);
        ToggleExitButton(true);
        PlayPopup(gameOverUI);
        Debug.Log("UIController: Game Over UI shown");
        gameOverUICoroutine = null;
    }

    public void ShowWinUI(bool showNextButton)
    {
        EnsureUIReferences();
        WireNextButton();
        if (winUI == null)
        {
            Debug.LogWarning("UIController: winUI is not assigned, cannot show Win UI.");
            return;
        }

        RestartWinRoutine(showNextButton);
    }

    private void RestartWinRoutine(bool showNextButton)
    {
        if (winUICoroutine != null)
        {
            StopCoroutine(winUICoroutine);
        }
        winUICoroutine = StartCoroutine(ShowWinUIDelayed(showNextButton));
    }

    private IEnumerator ShowWinUIDelayed(bool showNextButton)
    {
        yield return new WaitForSeconds(uiShowDelaySeconds);
        // Next button = bat khi con round
        ToggleNextButton(showNextButton);

        // Home button = bat khi khong con round
        if (homeUI != null)
            homeUI.gameObject.SetActive(!showNextButton);

        PlayPopup(winUI);
        Debug.Log($"UIController: ShowWinUI called. showNextButton={showNextButton} active={winUI.activeSelf} name={winUI.name}");

        winUICoroutine = null;
    }

    private string GetRandomNotice(List<string> list)
    {
        if (list == null || list.Count == 0) return "";
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    private void HandlePlayerDied(Player deadPlayer)
    {
        Debug.Log($"UIController: HandlePlayerDied received for {deadPlayer?.name ?? "unknown"}; gameEnded={gameEnded}");
        if (gameEnded) return;
        gameEnded = true;

        if (loseNoticeText != null)
        loseNoticeText.text = GetRandomNotice(loseNotices);
        
        ShowGameOverUI();
    }

    private void HandleGameWon()
    {
        Debug.Log($"UIController: HandleGameWon received; gameEnded={gameEnded}");
        // Even if we already marked gameEnded, still try to show the win UI in case it never appeared.
        bool hasNext = RoundManager.instance != null && RoundManager.instance.HasNextRound();
        gameEnded = true;

        if (winNoticeText != null)
            winNoticeText.text = GetRandomNotice(winNotices);

        ShowWinUI(hasNext);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureUIReferences();
        ResetUIState();
        WireExitButton();
        WireNextButton();

        Debug.Log($"UIController: Scene loaded ({scene.name}). winUI active={winUI?.activeSelf} gameOver active={gameOverUI?.activeSelf}");
    }

    private void ResetUIState()
    {
        gameEnded = false;
        StopPendingUIRoutines();
        if (gameOverUI != null && gameOverUI.activeSelf)
        {
            gameOverUI.SetActive(false);
        }
        if (winUI != null && winUI.activeSelf)
        {
            winUI.SetActive(false);
        }
        ToggleNextButton(false);
    }

    private void StopPendingUIRoutines()
    {
        if (winUICoroutine != null)
        {
            StopCoroutine(winUICoroutine);
            winUICoroutine = null;
        }

        if (gameOverUICoroutine != null)
        {
            StopCoroutine(gameOverUICoroutine);
            gameOverUICoroutine = null;
        }

        KillPopupTween(winUI);
        KillPopupTween(gameOverUI);
    }

    private void PlayPopup(GameObject panel)
    {
        if (panel == null) return;

        var target = panel.transform;
        target.DOKill(true);
        target.localScale = Vector3.one * popupStartScale;
        panel.SetActive(true);
        target.DOScale(Vector3.one * popupEndScale, popupDuration).SetEase(popupEase);
    }

    private void KillPopupTween(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.DOKill();
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
        Debug.Log($"UIController: exit button wired to ExitToMenu. Active={exitUI.gameObject.activeInHierarchy}, Enabled={exitUI.enabled}, Interactable={exitUI.interactable}");
    }

    private void WireNextButton()
    {
        if (nextUI == null)
        {
            if (!warnedMissingNext)
            {
                TryAssignNextFromWinUI();
                TryAutoAssignNextButton();
                if (nextUI == null)
                {
                    Debug.LogWarning("UIController: nextUI is not assigned.");
                    warnedMissingNext = true;
                }
            }
            return;
        }

        nextUI.onClick.RemoveAllListeners();
        nextUI.onClick.AddListener(RequestNextRound);
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

    private void TryAutoAssignNextButton()
    {
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        var match = buttons.FirstOrDefault(b =>
            b != null &&
            (b.CompareTag("NextButton") || b.name.IndexOf("next", StringComparison.OrdinalIgnoreCase) >= 0));

        if (match != null)
        {
            nextUI = match;
            warnedMissingNext = false;
            Debug.Log($"UIController: auto-assigned next button: {match.name}");
        }
    }

    // Prefer a next button under the win UI if one exists
    private void TryAssignNextFromWinUI()
    {
        if (winUI == null) return;
        var candidate = winUI.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(b => b != null && (b.CompareTag("NextButton") || b.name.IndexOf("next", StringComparison.OrdinalIgnoreCase) >= 0));
        if (candidate != null)
        {
            nextUI = candidate;
            warnedMissingNext = false;
            Debug.Log($"UIController: assigned next button from Win UI: {candidate.name}");
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

    private void RequestNextRound()
    {
        Debug.Log("UIController: Next round clicked.");
        if (RoundManager.instance != null)
        {
            RoundManager.instance.RequestNextRound();
            return;
        }

        if (PlayerManager.instance != null && PlayerManager.instance.localPlayer != null)
        {
            PlayerManager.instance.localPlayer.CmdRequestNextRound();
            return;
        }

        Debug.LogWarning("UIController: Could not find RoundManager or local Player to request next round.");
    }

    private void HandleRoundStarted(RoundStartClientData _)
    {
        gameEnded = false;
        ResetUIState();
    }

    private void HandleRoundEnded(RoundEndClientData data)
    {
        Debug.Log($"UIController: HandleRoundEnded received. won={data.won} hasNext={data.hasNextRound} gameEnded(before)={gameEnded}");
        gameEnded = true;
        if (data.won)
        {
            ShowWinUI(data.hasNextRound);
        }
        else
        {
            ShowGameOverUI();
        }
    }

    private void ToggleNextButton(bool show)
    {
        if (nextUI != null)
        {
            nextUI.gameObject.SetActive(show);
            nextUI.enabled = show;
            nextUI.interactable = show;
        }
    }

    private void ToggleExitButton(bool show)
    {
        if (exitUI != null)
        {
            exitUI.gameObject.SetActive(show);
            exitUI.interactable = show;
        }
    }
}
