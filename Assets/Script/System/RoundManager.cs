using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using static CONST;
using Script.Enemy;
using UnityEngine.SceneManagement;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager instance;

    [SerializeField] private RoundConfig roundConfig;
    [SerializeField] private SpawnSystem spawnSystem;
    [SerializeField] private EnemyManager enemyManager;

    [SyncVar(hook = nameof(OnRoundIndexChanged))] private int currentRoundIndex = -1;
    private bool roundResolved;
    private bool enemiesPreparedForCurrentRound;
    private int enemiesPreparedForPlayerCount;

    public static event Action<RoundStartClientData> OnRoundStartedClient;
    public static event Action<RoundEndClientData> OnRoundEndedClient;

    public static void RaiseRoundEndedClient(RoundEndClientData data)
    {
        OnRoundEndedClient?.Invoke(data);
    }

    private bool roundsInitialized;
    private int pendingRoundIndex = -1;
    private bool sceneReloadInProgress;
    private Coroutine applyBackgroundRoutine;
    private int lastBackgroundWarnRound = -1;
    private Sprite lastRoundBackground;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {

        ObserverManager.Register(GAME_WON, (Action)HandleRoundWonServer);
        ObserverManager.Register(GAME_LOST, (Action<Player>)HandleRoundLostServer);
        ObserverManager.Register(ALL_ENEMIES_DEFEATED, (Action)HandleRoundWonServer);
        ObserverManager.Register(SPAWN_PLAYER, (Action)HandlePlayerSpawned);

        SceneManager.sceneLoaded += OnSceneLoadedServer;

        TryStartRoundsForActiveScene();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyRoundBackgroundClient(currentRoundIndex);
    }

    private void OnEnable()
    {
        if (isClient)
        {
            ObserverManager.Register(MAP_ENABLED, (Action<PlayerMap, bool, GameObject>)HandleMapEnabledClient);
        }
    }

    public override void OnStopServer()
    {
        ObserverManager.Unregister(GAME_WON, (Action)HandleRoundWonServer);
        ObserverManager.Unregister(GAME_LOST, (Action<Player>)HandleRoundLostServer);
        ObserverManager.Unregister(ALL_ENEMIES_DEFEATED, (Action)HandleRoundWonServer);
        ObserverManager.Unregister(SPAWN_PLAYER, (Action)HandlePlayerSpawned);
        SceneManager.sceneLoaded -= OnSceneLoadedServer;
    }

    private void OnDisable()
    {
        if (isClient)
        {
            ObserverManager.Unregister(MAP_ENABLED, (Action<PlayerMap, bool, GameObject>)HandleMapEnabledClient);
        }
    }

    private void OnSceneLoadedServer(Scene scene, LoadSceneMode mode)
    {
        roundsInitialized = false;
        sceneReloadInProgress = false;
        TryStartRoundsForActiveScene();
    }

    [Server]
    private void TryStartRoundsForActiveScene()
    {
        if (!isServer) return;

        if (!IsGameplayScene(SceneManager.GetActiveScene()))
        {
            return;
        }

        if (roundsInitialized)
        {
            return;
        }

        if (roundConfig == null || roundConfig.RoundCount == 0)
        {
            Debug.LogWarning("RoundManager: roundConfig is missing or empty; rounds will not start.");
            return;
        }

        int targetRound = pendingRoundIndex >= 0 ? pendingRoundIndex : 0;
        pendingRoundIndex = -1;

        StartRoundInternal(targetRound);
        roundsInitialized = true;
    }

    private bool IsGameplayScene(Scene scene)
    {
        return scene.name.StartsWith("Scene_Map", StringComparison.OrdinalIgnoreCase);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    [Server]
    private void HandleRoundWonServer()
    {
        if (roundResolved) return;

        roundResolved = true;
        if (enemyManager != null) enemyManager.SetWinCheckEnabled(false);
        bool hasNext = HasNextRound();
        RpcRoundEnded(true, !hasNext, hasNext);
        Debug.Log($"RoundManager: HandleRoundWonServer -> RpcRoundEnded(won=true, finalRound={!hasNext}, hasNext={hasNext})");
    }

    [ServerCallback]
    private void Update()
    {
        // Fallback: if all enemies are gone but the win event never fired, trigger it.
        if (!roundResolved && NetworkServer.active && IsGameplayScene(SceneManager.GetActiveScene()) && enemiesPreparedForCurrentRound && enemyManager != null && enemyManager.AliveEnemyCount > 0)
        {
            if (!enemyManager.HasLivingActiveEnemies())
            {
                Debug.Log("RoundManager: No enemies found in scene, triggering win fallback.");
                HandleRoundWonServer();
            }
        }
    }

    [Server]
    private void HandleRoundLostServer(Player _)
    {
        if (roundResolved) return;

        roundResolved = true;
        if (enemyManager != null) enemyManager.SetWinCheckEnabled(false);
        RpcRoundEnded(false, true, false);
    }

    [Server]
    private void StartRoundInternal(int roundIndex)
    {
        if (roundConfig == null || roundConfig.RoundCount == 0)
        {
            Debug.LogWarning("RoundManager: No round config assigned.");
            return;
        }

        if (roundIndex < 0 || roundIndex >= roundConfig.RoundCount)
        {
            Debug.LogWarning($"RoundManager: Round index {roundIndex} is out of range.");
            return;
        }

        if (!TryResolveSpawnSystem())
        {
            Debug.LogWarning("RoundManager: SpawnSystem not found or inactive.");
            return;
        }

        if (!TryResolveEnemyManager())
        {
            Debug.LogWarning("RoundManager: EnemyManager not found.");
            return;
        }

        RoundDefinition round = roundConfig.GetRound(roundIndex);
        if (round == null)
        {
            Debug.LogWarning($"RoundManager: Round {roundIndex} data is null.");
            return;
        }

        roundResolved = false;
        currentRoundIndex = roundIndex;
        enemiesPreparedForCurrentRound = false;
        enemiesPreparedForPlayerCount = 0;

        bool enemiesReady = PrepareEnemies(round.enemySettings);
        PreparePlayers(round.playerSettings);
        PrepareWaves(round);

        RpcRoundStarted(currentRoundIndex, string.IsNullOrWhiteSpace(round.roundName) ? $"Round {currentRoundIndex + 1}" : round.roundName, currentRoundIndex >= roundConfig.RoundCount - 1);
        Debug.Log($"RoundManager: RpcRoundStarted server roundIndex={currentRoundIndex}");

        if (enemiesReady && enemyManager != null)
        {
            enemyManager.SetWinCheckEnabled(true);
        }

        if (round.autoStartWaves)
        {
            spawnSystem?.StartRoundWaves();
        }
    }

    [Server]
    private void PrepareWaves(RoundDefinition round)
    {
        if (!TryResolveSpawnSystem())
        {
            Debug.LogWarning("RoundManager: SpawnSystem not found.");
            return;
        }

        spawnSystem.ConfigureRound(round.waves, round.rewardWaves, round.waveStartDelay, round.autoStartWaves);
    }

    [Server]
    private void PreparePlayers(RoundPlayerSettings playerSettings)
    {
        if (PlayerManager.instance == null || PlayerManager.instance.players == null)
        {
            Debug.LogWarning("RoundManager: PlayerManager not ready to configure players.");
            return;
        }

        foreach (GameObject playerObject in PlayerManager.instance.players)
        {
            Player player = playerObject != null ? playerObject.GetComponent<Player>() : null;
            if (player == null) continue;

            player.ResetForRound(playerSettings);
        }
    }

    [Server]
    private void HandlePlayerSpawned()
    {
        RoundDefinition round = roundConfig != null ? roundConfig.GetRound(currentRoundIndex) : null;
        if (round == null) return;

        PreparePlayers(round.playerSettings);

        int playerCount = PlayerManager.instance != null && PlayerManager.instance.players != null
            ? PlayerManager.instance.players.Count
            : 0;

        // If enemies were deferred because players weren't spawned yet (or new players joined), configure them once everyone is in.
        if (( !enemiesPreparedForCurrentRound || playerCount > enemiesPreparedForPlayerCount) && AllPlayersSpawned())
        {
            bool enemiesReady = PrepareEnemies(round.enemySettings);
            if (enemiesReady && enemyManager != null)
            {
                enemyManager.SetWinCheckEnabled(true);
            }
        }
    }

    [Server]
    private bool AllPlayersSpawned()
    {
        if (PlayerManager.instance == null || PlayerManager.instance.players == null) return false;

        int spawnedPlayers = PlayerManager.instance.players.Count;
        int expectedPlayers = NetworkServer.connections.Count;

        return spawnedPlayers > 0 && expectedPlayers > 0 && spawnedPlayers >= expectedPlayers;
    }

    [Server]
    private bool PrepareEnemies(RoundEnemySettings enemySettings)
    {
        if (!TryResolveEnemyManager())
        {
            Debug.LogWarning("RoundManager: EnemyManager not found.");
            return false;
        }

        // Clear tracking without destroying existing enemies so we can re-use ones spawned by PlayerSpawnSystem.
        enemyManager.ResetRoundState(false);

        if (PlayerManager.instance == null || PlayerManager.instance.players == null || PlayerManager.instance.players.Count == 0)
        {
            Debug.LogWarning("RoundManager: No players available to spawn enemies for. Enemy setup will be retried when players spawn.");
            return false;
        }

        foreach (GameObject playerObject in PlayerManager.instance.players)
        {
            Player player = playerObject != null ? playerObject.GetComponent<Player>() : null;
            if (player == null) continue;

            // If an enemy already exists for this player, just update its stats; otherwise create one.
            if (player.enemy != null && player.enemy.gameObject != null)
            {
                Enemy enemyComponent = player.enemy;
                enemyComponent.ServerResetForRound(enemySettings);
                enemyComponent.gameObject.SetActive(true);
                enemyManager.AddEnemy(enemyComponent);
                enemyComponent.ApplyPatternSettings(enemySettings);
            }
            else
            {
                SpawnEnemyForPlayer(player, enemySettings);
            }
        }

        enemiesPreparedForCurrentRound = true;
        enemiesPreparedForPlayerCount = PlayerManager.instance.players.Count;
        return true;
    }

    [Server]
    private void SpawnEnemyForPlayer(Player player, RoundEnemySettings enemySettings)
    {
        if (player == null || !TryResolveEnemyManager()) return;

        Vector3 spawnPosition = player.playerMap != null ? player.playerMap.playerPos : player.transform.position;

        GameObject enemyObject = Instantiate(enemyManager.GetEnemy(), spawnPosition, Quaternion.identity);
        Enemy enemyComponent = enemyObject.GetComponent<Enemy>();
        enemyComponent.name = $"Enemy {player.Pos}";
        enemyComponent.id = player.id;
        enemyComponent.Pos = player.Pos;
        enemyComponent.maxHealth = enemySettings.maxHealth;
        enemyComponent.Health = enemySettings.maxHealth;
        enemyComponent.maxShield = enemySettings.maxShield;
        enemyComponent.Shield = enemySettings.maxShield;

        enemyManager.AddEnemy(enemyComponent);

        NetworkServer.Spawn(enemyObject, player.connectionToClient);
        player.RPCSetEnemy(enemyObject);

        enemyComponent.ApplyPatternSettings(enemySettings);
        Debug.Log($"RoundManager: SpawnEnemyForPlayer pos={player.Pos} hp={enemySettings.maxHealth} shield={enemySettings.maxShield}");
    }

    public bool HasNextRound()
    {
        if (roundConfig == null) return false;
        return currentRoundIndex + 1 < roundConfig.RoundCount;
    }

    public void RequestNextRound()
    {
        if (isServer)
        {
            ServerRequestNextRound();
        }
        else
        {
            CmdRequestNextRound();
        }
    }

    [Server]
    public void ServerRequestNextRound()
    {
        if (!roundResolved) return;
        if (!HasNextRound()) return;

        pendingRoundIndex = currentRoundIndex + 1;
        roundsInitialized = false;

        // reload the active game scene so all scene-bound objects reset for the next round
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (NetworkManager.singleton != null && NetworkManager.singleton.isActiveAndEnabled)
        {
            DestroyExistingPlayers();
            sceneReloadInProgress = true;
            NetworkManager.singleton.ServerChangeScene(activeSceneName);
        }
        else
        {
            Debug.LogWarning("RoundManager: NetworkManager missing; starting next round without scene reload.");
            StartRoundInternal(pendingRoundIndex);
        }
    }

    [Server]
    private void DestroyExistingPlayers()
    {
        // Clear server-side player list
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.players.Clear();
        }

        // Destroy current player objects so they will be respawned by PlayerSpawnSystem on scene load
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                NetworkServer.Destroy(conn.identity.gameObject);
            }
        }
    }

    [Server]
    private bool TryResolveSpawnSystem()
    {
        if (spawnSystem != null && spawnSystem.isActiveAndEnabled) return true;

        spawnSystem = SpawnSystem.singleton ?? FindObjectsOfType<SpawnSystem>(true).FirstOrDefault();
        if (spawnSystem == null) return false;

        if (!spawnSystem.gameObject.activeSelf) spawnSystem.gameObject.SetActive(true);
        if (!spawnSystem.enabled) spawnSystem.enabled = true;
        return spawnSystem.isActiveAndEnabled;
    }

    [Server]
    private bool TryResolveEnemyManager()
    {
        if (enemyManager != null) return true;
        enemyManager = EnemyManager.instance ?? FindObjectsOfType<EnemyManager>(true).FirstOrDefault();
        return enemyManager != null;
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestNextRound()
    {
        ServerRequestNextRound();
    }

    [ClientRpc]
    private void RpcRoundStarted(int roundIndex, string roundName, bool isFinalRound)
    {
        Debug.Log($"RoundManager: RpcRoundStarted received on client. roundIndex={roundIndex} isFinalRound={isFinalRound}");
        ApplyRoundBackgroundClient(roundIndex);
        OnRoundStartedClient?.Invoke(new RoundStartClientData(roundIndex, roundName, isFinalRound));
    }

    [ClientRpc]
    private void RpcRoundEnded(bool won, bool isFinalRound, bool hasNextRound)
    {
        Debug.Log($"RoundManager: RpcRoundEnded received on client. won={won} isFinalRound={isFinalRound} hasNextRound={hasNextRound}");
        OnRoundEndedClient?.Invoke(new RoundEndClientData(won, isFinalRound, hasNextRound));
    }

    private void OnRoundIndexChanged(int _, int newIndex)
    {
        // SyncVar hook to ensure late-joining/replicated clients apply the right background
        if (isClient)
        {
            ApplyRoundBackgroundClient(newIndex);
        }
    }

    [Client]
    private void ApplyRoundBackgroundClient(int roundIndex)
    {
        if (roundIndex < 0) return;
        lastRoundBackground = GetRoundBackground(roundIndex);

        if (applyBackgroundRoutine != null)
        {
            StopCoroutine(applyBackgroundRoutine);
        }

        applyBackgroundRoutine = StartCoroutine(ApplyRoundBackgroundWhenReady(roundIndex));
    }

    [Client]
    private IEnumerator ApplyRoundBackgroundWhenReady(int roundIndex)
    {
        Sprite background = GetRoundBackground(roundIndex);
        if (background == null)
        {
            if (roundConfig == null || roundConfig.RoundCount == 0)
            {
                Debug.LogWarning("RoundManager: roundConfig is missing on client; cannot set background.");
            }
            else
            {
                Debug.LogWarning($"RoundManager: Round {roundIndex} has no backgroundMap Sprite assigned.");
            }
            yield break;
        }

        float timeout = 8f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (TryApplyBackgroundToMaps(background))
            {
                yield break;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!TryApplyBackgroundToMaps(background))
        {
            Debug.LogWarning($"RoundManager: Unable to apply background for round {roundIndex}; no PlayerMap instances found.");
        }
    }

    [Client]
    private bool TryApplyBackgroundToMaps(Sprite backgroundSprite)
    {
        if (backgroundSprite == null) return true;

        IEnumerable<PlayerMap> maps = MapManager.instance != null && MapManager.instance.playerMaps != null && MapManager.instance.playerMaps.Count > 0
            ? MapManager.instance.playerMaps
            : FindObjectsOfType<PlayerMap>(true);

        bool applied = false;
        foreach (PlayerMap map in maps)
        {
            applied |= TrySetMapBackground(map, backgroundSprite);
        }

        return applied;
    }

    private bool TrySetMapBackground(PlayerMap map, Sprite sprite)
    {
        if (map == null || map.mapBackground == null) return false;

        SpriteRenderer renderer = map.mapBackground.GetComponent<SpriteRenderer>();
        if (renderer == null) return false;

        renderer.sprite = sprite;
        return true;
    }

    private Sprite GetRoundBackground(int roundIndex)
    {
        RoundDefinition round = roundConfig != null ? roundConfig.GetRound(roundIndex) : null;
        if (round == null) return null;

        if (round.backgroundMap == null && lastBackgroundWarnRound != roundIndex)
        {
            Debug.LogWarning($"RoundManager: Round {roundIndex} has no backgroundMap Sprite assigned.");
            lastBackgroundWarnRound = roundIndex;
        }

        return round.backgroundMap;
    }

    [Client]
    private void HandleMapEnabledClient(PlayerMap map, bool enable, GameObject _)
    {
        if (!enable || map == null) return;

        Sprite background = GetCurrentRoundBackground();
        if (background == null) return;

        if (!TrySetMapBackground(map, background))
        {
            Debug.LogWarning($"RoundManager: Failed to set background on map {map.name}");
        }
    }

    public Sprite GetCurrentRoundBackground()
    {
        if (lastRoundBackground != null) return lastRoundBackground;
        return GetRoundBackground(currentRoundIndex);
    }
}

public readonly struct RoundStartClientData
{
    public readonly int roundIndex;
    public readonly string roundName;
    public readonly bool isFinalRound;

    public RoundStartClientData(int roundIndex, string roundName, bool isFinalRound)
    {
        this.roundIndex = roundIndex;
        this.roundName = roundName;
        this.isFinalRound = isFinalRound;
    }
}

public readonly struct RoundEndClientData
{
    public readonly bool won;
    public readonly bool isFinalRound;
    public readonly bool hasNextRound;

    public RoundEndClientData(bool won, bool isFinalRound, bool hasNextRound)
    {
        this.won = won;
        this.isFinalRound = isFinalRound;
        this.hasNextRound = hasNextRound;
    }
}
