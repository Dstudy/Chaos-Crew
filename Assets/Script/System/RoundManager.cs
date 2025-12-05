using System;
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

    [SyncVar] private int currentRoundIndex = -1;
    private bool roundResolved;

    public static event Action<RoundStartClientData> OnRoundStartedClient;
    public static event Action<RoundEndClientData> OnRoundEndedClient;

    private bool roundsInitialized;
    private int pendingRoundIndex = -1;
    private bool sceneReloadInProgress;

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
        base.OnStartServer();

        ObserverManager.Register(GAME_WON, (Action)HandleRoundWonServer);
        ObserverManager.Register(GAME_LOST, (Action<Player>)HandleRoundLostServer);
        ObserverManager.Register(SPAWN_PLAYER, (Action)HandlePlayerSpawned);

        SceneManager.sceneLoaded += OnSceneLoadedServer;

        TryStartRoundsForActiveScene();
    }

    public override void OnStopServer()
    {
        ObserverManager.Unregister(GAME_WON, (Action)HandleRoundWonServer);
        ObserverManager.Unregister(GAME_LOST, (Action<Player>)HandleRoundLostServer);
        ObserverManager.Unregister(SPAWN_PLAYER, (Action)HandlePlayerSpawned);
        SceneManager.sceneLoaded -= OnSceneLoadedServer;
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
        bool hasNext = HasNextRound();
        RpcRoundEnded(true, !hasNext, hasNext);
    }

    [Server]
    private void HandleRoundLostServer(Player _)
    {
        if (roundResolved) return;

        roundResolved = true;
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

        PrepareEnemies(round.enemySettings);
        PreparePlayers(round.playerSettings);
        PrepareWaves(round);

        RpcRoundStarted(currentRoundIndex, string.IsNullOrWhiteSpace(round.roundName) ? $"Round {currentRoundIndex + 1}" : round.roundName, currentRoundIndex >= roundConfig.RoundCount - 1);

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
    }

    [Server]
    private void PrepareEnemies(RoundEnemySettings enemySettings)
    {
        if (!TryResolveEnemyManager())
        {
            Debug.LogWarning("RoundManager: EnemyManager not found.");
            return;
        }

        enemyManager.ResetRoundState();

        if (PlayerManager.instance == null || PlayerManager.instance.players == null)
        {
            Debug.LogWarning("RoundManager: No players available to spawn enemies for.");
            return;
        }

        foreach (GameObject playerObject in PlayerManager.instance.players)
        {
            Player player = playerObject != null ? playerObject.GetComponent<Player>() : null;
            if (player == null) continue;

            SpawnEnemyForPlayer(player, enemySettings);
        }
    }

    [Server]
    private void SpawnEnemyForPlayer(Player player, RoundEnemySettings enemySettings)
    {
        if (player == null || !TryResolveEnemyManager()) return;

        if (player.enemy != null && player.enemy.gameObject != null)
        {
            NetworkServer.Destroy(player.enemy.gameObject);
            player.enemy = null;
        }

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
        OnRoundStartedClient?.Invoke(new RoundStartClientData(roundIndex, roundName, isFinalRound));
    }

    [ClientRpc]
    private void RpcRoundEnded(bool won, bool isFinalRound, bool hasNextRound)
    {
        OnRoundEndedClient?.Invoke(new RoundEndClientData(won, isFinalRound, hasNextRound));
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
