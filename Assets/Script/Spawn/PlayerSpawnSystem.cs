using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using Script.Enemy;
using UnityEngine;
using static CONST;

public class PlayerSpawnSystem : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab = null;
    [SerializeField] private GameObject mapPrefab = null;
    private EnemyManager enemyManager;
    private PlayerManager playerManager;
    private MapManager mapManager;
    private int nextIndex = 0;
    
    private bool isServerReady = false;
    

    public override void OnStartServer()
    {
        // Reset nextIndex when server starts to ensure proper indexing
        nextIndex = 0;
        NetworkManagerLobby.OnServerReadied += SpawnPlayer;
    }

    public override void OnStartClient()
    {
        Debug.Log("Start client and register localplayer");
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
                if (player != null && player.playerMap!=null && player.enemy !=null)
                {
                    // This is the new game player, not the old lobby player!
                    NetworkGamePlayerLobby networkGamePlayer = NetworkClient.localPlayer.GetComponent<NetworkGamePlayerLobby>();
                    if (networkGamePlayer != null)
                    {
                        networkGamePlayer.localPlayer = player;
                        networkGamePlayer.EnableMapCollider(player.gameObject);
                        PlayerManager.instance.SetLocalPlayer(player);
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
        
        if(MapManager.instance==null)
            Debug.LogError("MapManager instance is null!");
        
        enemyManager = EnemyManager.instance;
        playerManager = PlayerManager.instance;
        mapManager = MapManager.instance;
        
        
        Vector3 spawnPoint = new Vector3(0 + nextIndex * mapManager.spawnDistance, 0, 0);
        
        GameObject playerInstance =
            Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
        
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
        
        GameObject map = Instantiate(mapPrefab, spawnPoint, Quaternion.identity);
        map.name = $"Map {nextIndex}";
        NetworkServer.Spawn(map, conn);
        
        PlayerMap playerMap = map.GetComponent<PlayerMap>();
        if (playerMap != null)
        {
            playerStat.playerMap = playerMap; // Set on server
        }
        playerMap.mapPos = nextIndex;
        playerMap.playerPos = spawnPoint;
        playerStat.RpcSetMap(map);
        
        playerManager.AddPlayer(playerStat);
        mapManager.AddMap(playerMap);
        
        GameObject enemy = Instantiate(enemyManager.GetEnemy(), spawnPoint, Quaternion.identity);
        Enemy enemyStat = enemy.GetComponent<Enemy>();
        enemyStat.name = $"Enemy {nextIndex}";
        enemyStat.id = nextIndex.ToString();
        enemyStat.Pos = nextIndex;
        enemyManager.InitElements();
        NetworkServer.Spawn(enemy, conn);
        
        playerStat.RPCSetEnemy(enemy);
        
        nextIndex++;
        
        ObserverManager.InvokeEvent(SPAWN_PLAYER);
    }

}
