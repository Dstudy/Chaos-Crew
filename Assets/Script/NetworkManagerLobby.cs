using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerLobby : NetworkManager
{
    [SerializeField] private int minPlayers = 2;
    [SerializeField]private string menuScene = string.Empty;
    public static Action onClientConnected;
    public static Action onClientDisconnected;
    public static Action<NetworkConnectionToClient> OnServerReadied;
    public static Action OnServerStopped;
    
    // [Header("Maps")]
    // [SerializeField] private int numberOfRounds = 1;
    // [SerializeField] private MapSet mapSet = null;
    
    [Header("Room")]
    [SerializeField] private NetworkRoomPlayerLobby lobbyPrefab = null;
    
    [Header("Game")]
    [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
    [SerializeField] private GameObject playerSpawnSystem = null;
    
    [SerializeField] private string gameScene = string.Empty;
    
    public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
    public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();

    public override void OnClientConnect()
    {
        Debug.Log("OnClientConnect");
        
        base.OnClientConnect();
        
        onClientConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        
        onClientDisconnected?.Invoke();
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (numPlayers >= maxConnections)
        {
            conn.Disconnect();
            return;
        }

        if (SceneManager.GetActiveScene().name != menuScene)
        {
            Debug.Log(SceneManager.GetActiveScene().name + " is not " + menuScene);
            conn.Disconnect();
            return;
        }
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkRoomPlayerLobby>();

            RoomPlayers.Remove(player);

            NotifyPlayersOfReadyState();
        }

        base.OnServerDisconnect(conn);
    }
    
    public override void OnStopServer()
    {
        OnServerStopped?.Invoke();

        RoomPlayers.Clear();
        GamePlayers.Clear();
    }
    
    public void NotifyPlayersOfReadyState()
    {
        foreach (var player in RoomPlayers)
        {
            player.HandleReadyToStart(IsReadyToStart());
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("ServerAddPlayer");
        if (SceneManager.GetActiveScene().name == menuScene)
        {
            Debug.Log("Server deter leader");
            bool isLeader = RoomPlayers.Count == 0;
            
            NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(lobbyPrefab);

            roomPlayerInstance.IsLeader = isLeader;
            
            NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
        }
        
    }
    
    private bool IsReadyToStart()
    {
        if (numPlayers < minPlayers) { return false; }

        return true;
    }
    
    public void StartGame()
    {
        if (SceneManager.GetActiveScene().name == menuScene)
        {
            if (!IsReadyToStart()) { return; }
            ServerChangeScene(gameScene);
        }
    }
    
    public override void ServerChangeScene(string newSceneName)
    {
        string newSceneText = System.IO.Path.GetFileNameWithoutExtension(newSceneName);
        // From menu to game
        if (SceneManager.GetActiveScene().name == menuScene && newSceneText.StartsWith("Scene_Map"))
        {
            for (int i = RoomPlayers.Count - 1; i >= 0; i--)
            {
                var conn = RoomPlayers[i].connectionToClient;
                NetworkServer.Destroy(conn.identity.gameObject);
            }
        }

        base.ServerChangeScene(newSceneName);
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName.StartsWith("Scene_Map"))
        {
            Debug.Log($"OnServerSceneChanged: {sceneName}");
            GameObject playerSpawnSystemInstance = Instantiate(playerSpawnSystem);
            NetworkServer.Spawn(playerSpawnSystemInstance);
        }
    }
    
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        OnServerReadied?.Invoke(conn);
    }

    public override void OnStartHost()
    {
        Debug.Log("OnStartHost");
    }
}
