using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomPlayerLobby : NetworkBehaviour
{
    
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] private TextMeshProUGUI numText = null;
    [SerializeField] private Button startGameButton = null;
    
    [SyncVar(hook = nameof(HandleReadyStatusChanged))]
    public bool IsReady = false;
    
    private bool isLeader;
    public bool IsLeader
    {
        set
        {
            Debug.Log($"IsLeader: {value}");
            isLeader = value;
            startGameButton.gameObject.SetActive(value);
        }
    }
    
    private NetworkManagerLobby room;
    public NetworkManagerLobby Room
    {
        get
        {
            if (room != null) { return room; }
            return room = NetworkManager.singleton as NetworkManagerLobby;
        }  
    }
    
    public void HandleReadyStatusChanged(bool oldValue, bool newValue) => UpdateDisplay();
    
    private void UpdateDisplay(){}
    
    public override void OnStartAuthority()
    {
        lobbyUI.SetActive(true);
    }

    public override void OnStartClient()
    {
        Room.RoomPlayers.Add(this);
    }

    public override void OnStopClient()
    {
        Room.RoomPlayers.Remove(this);
    }
    
    
    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) { return; }

        startGameButton.interactable = readyToStart;
    }
    
    [Command]
    public void CmdStartGame()
    {
        Debug.Log(isLeader);
        if (Room.RoomPlayers[0].connectionToClient != connectionToClient) { return; }
        Room.StartGame();
    }
}
