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
    
    private bool isServerReady = false;
    
    public static void AddSpawnPoint(Transform transform) => spawnPoints.Add(transform);
    public static void RemoveSpawnPoint(Transform transform) => spawnPoints.Remove(transform);

    public override void OnStartServer()
    {
        // Reset nextIndex when server starts to ensure proper indexing
        nextIndex = 0;
        Debug.Log($"PlayerSpawnSystem: Server started. Spawn points available: {spawnPoints.Count}, nextIndex reset to 0");
        NetworkManagerLobby.OnServerReadied += SpawnPlayer;
    }

    public override void OnStartClient()
    {
        StartCoroutine(WaitForLocalPlayer());
    }

    [ServerCallback]
    private void OnDestroy() => NetworkManagerLobby.OnServerReadied -= SpawnPlayer;
    
    
    private IEnumerator WaitForLocalPlayer()
    {
        // Wait until localPlayer is available AND it's the new game player (has Player component)
        float timeout = 10f;
        float elapsed = 0f;
    
        // Wait for localPlayer to exist AND have Player component (new game player, not old lobby player)
        while (elapsed < timeout)
        {
            // Check if we have a localPlayer AND it's the new game player (has Player component)
            if (NetworkClient.localPlayer != null)
            {
                Player player = NetworkClient.localPlayer.GetComponent<Player>();
                if (player != null && player.map!=null)
                {
                    // This is the new game player, not the old lobby player!
                    NetworkGamePlayerLobby networkGamePlayer = NetworkClient.localPlayer.GetComponent<NetworkGamePlayerLobby>();
                    if (networkGamePlayer != null)
                    {
                        networkGamePlayer.localPlayer = player;
                        networkGamePlayer.EnableMapCollider();
                        Debug.Log("LocalPlayer stored: " + NetworkClient.localPlayer.name);
                        yield break; // Exit the coroutine
                    }
                    else
                    {
                        Debug.LogError("NetworkGamePlayerLobby component not found!");
                        yield break;
                    }
                }
            }
        
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
    
        // Timeout reached
        Debug.LogError("Timeout waiting for localPlayer with Player component!");
    }
    
    

    [Server]
    public void SpawnPlayer(NetworkConnectionToClient conn)
    {
        // Prevent duplicate spawning
        if (conn.identity != null && conn.identity.GetComponent<Player>() != null)
        {
            Debug.LogWarning($"Player already exists for connection {conn.connectionId}, skipping spawn.");
            return;
        }
        
        Debug.Log($"SpawnPlayer called for connection {conn.connectionId}, nextIndex: {nextIndex}, spawnPoints.Count: {spawnPoints.Count}");
        
        enemyManager = EnemyManager.instance;
        playerManager = PlayerManager.instance;
        
        // Validate that we have enough spawn points
        if (spawnPoints.Count == 0)
        {
            Debug.LogError($"No spawn points available! Make sure PlayerSpawnPoint objects exist in the scene.");
            return;
        }
        
        if (nextIndex >= spawnPoints.Count)
        {
            Debug.LogError($"Not enough spawn points! Player index {nextIndex} but only {spawnPoints.Count} spawn points available.");
            return;
        }
        
        Transform spawnPoint = spawnPoints[nextIndex];

        if (spawnPoint == null)
        {
            Debug.LogError($"Spawn point at index {nextIndex} is null!");
            return;
        }
        
        Debug.Log($"Spawning player at index {nextIndex} using spawn point: {spawnPoint.name} (sibling index: {spawnPoint.GetSiblingIndex()})");

        GameObject playerInstance =
            Instantiate(playerPrefab, spawnPoint.position + Vector3.up * 2.8f, spawnPoint.rotation);
        
        Player playerStat = playerInstance.GetComponent<Player>();
        playerStat.gameObject.name = $"Player {nextIndex}";
        playerStat.id = nextIndex.ToString();
        playerStat.Pos = nextIndex;
        
        bool success = NetworkServer.ReplacePlayerForConnection(conn, playerInstance, ReplacePlayerOptions.KeepActive);
    
        if (!success)
        {
            Debug.LogError($"Failed to replace player for connection {conn.connectionId}");
            Destroy(playerInstance);
            return;
        }
        
        GameObject map = Instantiate(mapPrefab, spawnPoint.position, spawnPoint.rotation);
        map.name = $"Map {nextIndex}";
        NetworkServer.Spawn(map, conn);
        
        MapManager mapManager = map.GetComponent<MapManager>();
        if (mapManager != null)
        {
            playerStat.map = mapManager; // Set on server
        }
        
        playerStat.RpcSetMap(map);

        
        playerManager.AddPlayer(playerStat);
        
        GameObject enemy = Instantiate(enemyManager.GetEnemy(), spawnPoint.position, spawnPoint.rotation);
        Enemy enemyStat = enemy.GetComponent<Enemy>();
        enemyStat.name = $"Enemy {nextIndex}";
        enemyStat.id = nextIndex.ToString();
        NetworkServer.Spawn(enemy, conn);
        
        nextIndex++;
    }

}
