using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkGamePlayerLobby : NetworkBehaviour
{
    private MapManager map;
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
    
    private void OnClientConnect()
    {
        Debug.Log("Enable map collider lai");
        localPlayer.map.EnableCollider(true);
    }
    
    private void Start()
    {
        playerManager = PlayerManager.instance;
    }

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        Room.GamePlayers.Add(this);
        NetworkManagerLobby.onClientConnected += OnClientConnect;
    }

    public override void OnStopClient()
    {
        Room.GamePlayers.Remove(this);
    }
    

    [Command]
    public void CmdTeleportItem(GameObject itemToTeleport, int direction)
    {
        GameObject player = gameObject;
        playerManager = PlayerManager.instance;
        if (playerManager == null)
            Debug.Log("PlayerManager is null");
        if (itemToTeleport == null)
            Debug.Log("ItemToTeleport is null");
        if (playerManager.players == null)
            Debug.Log("Players is null");
        Debug.Log($"PlayerManager: {playerManager.players.IndexOf(player)}");
        int index = playerManager.players.IndexOf(player);



        int teleIndex = (index + direction + playerManager.players.Count) % playerManager.players.Count;

        Debug.Log("Teleport item from player: " + index + " to player: " + teleIndex);

        Player playerScript = playerManager.players[teleIndex].GetComponent<Player>();

        if (TeleportItem.Instance == null)
            Debug.Log("TeleportItemn is NULL");


        TeleportItem.Instance.ServerTeleport(itemToTeleport, direction, playerScript.map);
    }

    [Command]
    public void CmdAssignAuthority(GameObject objectToOwn)
    {
        AssignAuthority(objectToOwn);
    }


    [Server]
    public void AssignAuthority(GameObject objectToOwn)
    {
        NetworkIdentity objectIdentity = objectToOwn.GetComponent<NetworkIdentity>();

        if (objectIdentity == null)
        {
            Debug.Log("Object doesn't have a NetworkIdentity.");
            return;
        }

        if (objectIdentity.connectionToClient == connectionToClient)
        {
            Debug.Log("Cung la cai ong nay: " + connectionToClient.connectionId);
            return;
        }

        // Remove authority from current owner if it exists
        if (objectIdentity.isOwned && objectIdentity.connectionToClient != null)
        {
            objectIdentity.RemoveClientAuthority();
        }

        objectIdentity.AssignClientAuthority(connectionToClient);

        Debug.Log($"Gave ownership of {objectToOwn.name} to {connectionToClient.connectionId}");
    }
}
