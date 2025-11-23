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
    
    private void Start()
    {
        playerManager = PlayerManager.instance;
    }

    public void EnableMapCollider()
    {
        if (localPlayer == null)
            return;
        if (localPlayer.map == null)
        {
            Debug.Log("mapp null roi");
            return;
        }
        localPlayer.map.EnableCollider(true);
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
    public void CmdAssignAuthority(GameObject objectToOwn, NetworkConnectionToClient conn = null)
    {
        Debug.Log("Thang " + conn.connectionId + " gui yeu cau");
        NetworkIdentity objectIdentity = objectToOwn.GetComponent<NetworkIdentity>();

        if (objectIdentity == null)
        {
            Debug.Log("Object doesn't have a NetworkIdentity.");
            return;
        }

        // Check if authority is already assigned to this connection
        if (objectIdentity.connectionToClient == connectionToClient)
        {
            Debug.Log("Authority already assigned to this connection: " + connectionToClient.connectionId);
            return;
        }

        // Remove authority from current owner if it exists
        if (objectIdentity.isOwned && objectIdentity.connectionToClient != null)
        {
            objectIdentity.RemoveClientAuthority();
        }
        if(objectIdentity.isOwned)
            Debug.Log("Tai sao van dang duoc su dung?");
        objectIdentity.AssignClientAuthority(connectionToClient);
        Debug.Log($"Gave ownership of {objectToOwn.name} to {connectionToClient.connectionId}");
        
        Reply(objectToOwn, connectionToClient.connectionId);
    }

    [TargetRpc]
    private void Reply(GameObject obj, int id)
    { 
        Debug.Log("Tao da dua cho m " + obj.name + " va id cua m la: " +id.ToString());
    }
}
