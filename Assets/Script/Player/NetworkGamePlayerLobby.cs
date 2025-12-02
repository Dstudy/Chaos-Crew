using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Script.UI;
using UnityEngine;
using static CONST;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    private PlayerMap _playerMap;
    private PlayerManager playerManager;
    [SyncVar]
    private string displayName = "Loading...";

    private NetworkManagerLobby room;
    private NetworkManagerLobby Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }
    }

    public Player localPlayer;

    private void OnEnable()
    {
        ObserverManager.Register(MAP_ENABLED, (Action<PlayerMap, bool, GameObject>) HandleMapEnabled);
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(MAP_ENABLED, (Action<PlayerMap, bool, GameObject>) HandleMapEnabled);
    }

    private void Start()
    {
        playerManager = PlayerManager.instance;
    }

    public void EnableMapCollider(GameObject player)
    {
        if (localPlayer == null)
            return;
        if (localPlayer.playerMap == null)
        {
            Debug.Log("mapp null roi");
            return;
        }
        localPlayer.playerMap.EnableCollider(true, localPlayer.gameObject);
        if (localPlayer.enemy == null)
        {
            Debug.Log("Enemy null roi");
            return;
        }
        localPlayer.enemy.isLocalEnemy = true;
        localPlayer.enemy.gameObject.GetComponent<EnemyPattern>().StartEnemyPattern();
        Debug.Log("Set local enemy");
    }

    private void HandleMapEnabled(PlayerMap playerMap, bool enabled, GameObject owner)
    {
        if (!enabled) return;
        if (localPlayer == null || owner != localPlayer.gameObject) return;
        if (localPlayer.enemy == null) return;

        localPlayer.enemy.gameObject.SetActive(true);
        Debug.Log($"Enemy activated because map {playerMap.name} finished enabling.");
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        Room.GamePlayers.Add(this);
    }
    public override void OnStopClient()
    {
        Room.GamePlayers.Remove(this);
    }
    
    
    //
    // [Command]
    // public void CmdTeleportItem(GameObject itemToTeleport, int direction, int index)
    // {
    //     playerManager = PlayerManager.instance;
    //     if (playerManager == null)
    //         Debug.Log("PlayerManager is null");
    //     if (itemToTeleport == null)
    //         Debug.Log("ItemToTeleport is null");
    //     if (playerManager.players == null)
    //         Debug.Log("Players is null");
    //     Debug.Log($"PlayerManager: " + index);
    //     
    //     int teleIndex = (index + direction + playerManager.players.Count) % playerManager.players.Count;
    //
    //     Debug.Log("Teleport item from player: " + index + " to player: " + teleIndex);
    //
    //     Player playerScript = playerManager.players[teleIndex].GetComponent<Player>();
    //
    //     if (TeleportItem.Instance == null)
    //         Debug.Log("TeleportItemn is NULL");
    //     
    //     Transform telePosition = direction == -1 ? playerScript.playerMap.rightSpawnPoint : playerScript.playerMap.leftSpawnPoint;
    //     
    //     TeleportItem.Instance.Teleport(itemToTeleport, direction, telePosition.position);
    //     
    // }
    
}
