using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEngine;

public class PlayerSpawnSystem : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab = null;
    [SerializeField] private GameObject mapPrefab = null;
    private EnemyManager enemyManager;
    private PlayerManager playerManager;

    private static List<Transform> spawnPoints = new List<Transform>();

    private int nextIndex = 0;
    
    

    public static void AddSpawnPoint(Transform transform)
    {
        spawnPoints.Add(transform);

        spawnPoints = spawnPoints.OrderBy(x => x.GetSiblingIndex()).ToList();
    }

    public static void RemoveSpawnPoint(Transform transform) => spawnPoints.Remove(transform);

    public override void OnStartServer() => NetworkManagerLobby.OnServerReadied += SpawnPlayer;

    public override void OnStartClient()
    {
        Debug.Log("OnStartClient");
        NetworkManagerLobby.OnServerReadied += OnClientSeverReadied;
    }

    [ServerCallback]
    private void OnDestroy() => NetworkManagerLobby.OnServerReadied -= SpawnPlayer;

    private void OnClientSeverReadied(NetworkConnection conn)
    {
        StartCoroutine(WaitForLocalPlayer());
    }
    
    private IEnumerator WaitForLocalPlayer()
    {
        // Wait until localPlayer is available (with timeout)
        float timeout = 10f;
        float elapsed = 0f;
        
        while (NetworkClient.localPlayer == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (NetworkClient.localPlayer != null)
        {
            Player player = NetworkClient.localPlayer.GetComponent<Player>();
            if (player != null)
            {
                NetworkGamePlayerLobby networkGamePlayer = NetworkClient.localPlayer.GetComponent<NetworkGamePlayerLobby>();
                if (networkGamePlayer != null)
                {
                    networkGamePlayer.localPlayer = player;
                    Debug.Log("LocalPlayer stored: " + NetworkClient.localPlayer.name);
                }
                else
                {
                    Debug.LogError("PlayerManager component not found!");
                }
            }
            else
            {
                Debug.LogWarning("Local player doesn't have Player component!");
            }
        }
        else
        {
            Debug.LogError("Timeout waiting for localPlayer!");
        }
    }

    [Server]
    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        enemyManager = EnemyManager.instance;
        playerManager = PlayerManager.instance;
        Transform spawnPoint = spawnPoints.ElementAtOrDefault(nextIndex);

        if (spawnPoint == null)
        {
            Debug.LogError($"Missing spawn point for player {nextIndex}");
            return;
        }

        GameObject playerInstance =
            Instantiate(playerPrefab, spawnPoints[nextIndex].position + Vector3.up * 2.8f, spawnPoints[nextIndex].rotation);
        
        Player playerStat = playerInstance.GetComponent<Player>();
        playerStat.name = $"Player {nextIndex}";
        playerStat.id = nextIndex.ToString();
        playerStat.Pos = nextIndex;
        
        NetworkServer.ReplacePlayerForConnection(conn, playerInstance);
        
        GameObject map = Instantiate(mapPrefab, spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);
        playerStat.map = map.GetComponent<MapManager>();
        NetworkServer.Spawn(map, conn);
        
        playerManager.AddPlayer(playerStat);
        
        GameObject enemy = Instantiate(enemyManager.GetEnemy(), spawnPoints[nextIndex].position, spawnPoints[nextIndex].rotation);
        Enemy enemyStat = enemy.GetComponent<Enemy>();
        enemyStat.name = $"Enemy {nextIndex}";
        enemyStat.id = nextIndex.ToString();
        NetworkServer.Spawn(enemy, conn);
        
        nextIndex++;
    }

}
