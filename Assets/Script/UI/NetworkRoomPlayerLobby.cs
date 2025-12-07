using System.Collections;
using System.Collections.Generic;
using Mirror;
using Script.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomPlayerLobby : NetworkBehaviour
{
    
    [Header("UI")]
    [SerializeField] private GameObject lobbyUI = null;
    [SerializeField] internal TextMeshProUGUI numText = null;
    [SerializeField] private Button startGameButton = null;
    private int playerReadyNumber = 0;
    
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

    private void UpdateDisplay()
    {
        if (numText != null && Room != null)
        {
            numText.text = Room.RoomPlayers.Count.ToString();
        }
    }
    
    public override void OnStartAuthority()
    {
        if(isLocalPlayer)
        {
            lobbyUI.SetActive(true);
            if (Room != null)
            {
                Room.UpdatePlayerCountDisplay();
            }
        }
    }

    public override void OnStartClient()
    {
        Room.RoomPlayers.Add(this);
        Room.UpdatePlayerCountDisplay();
    }

    public override void OnStopClient()
    {
        Room.RoomPlayers.Remove(this);
        Room.UpdatePlayerCountDisplay();
    }
    
    
    public void HandleReadyToStart(bool readyToStart)
    {
        if (!isLeader) { return; }

        startGameButton.interactable = readyToStart;
    }
    
    [Command]
    public void CmdStartGame()
    {
        if (Room.RoomPlayers[0].connectionToClient != connectionToClient) { return; }
        Room.StartGame();
    }
}
