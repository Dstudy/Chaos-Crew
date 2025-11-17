using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class JoinLobbyMenu : MonoBehaviour
{
    [SerializeField] private NetworkManagerLobby networkManager;
    
    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel;
    [SerializeField] private TMP_InputField ipAddressInputField;
    [SerializeField] private Button joinButton;

    private void OnEnable()
    {
        NetworkManagerLobby.onClientConnected += HandleClientConnected;
        NetworkManagerLobby.onClientDisconnected += HandleClientDisconnected;
        
        ipAddressInputField.text = "localhost";
    }

    private void OnDisable()
    {
        NetworkManagerLobby.onClientConnected -= HandleClientConnected;
        NetworkManagerLobby.onClientDisconnected -= HandleClientDisconnected;
    }

    private void HandleClientConnected()
    {
        Debug.Log("Connected to server");
        joinButton.interactable = true;
        
        gameObject.SetActive(false);
        landingPagePanel.SetActive(false);
    }

    private void HandleClientDisconnected()
    {
        joinButton.interactable = true;
    }

    public void JoinLobby()
    {
        string ipAddress = ipAddressInputField.text;
        
        networkManager.networkAddress = ipAddress;
        networkManager.StartClient();
        
        Debug.Log("Joining lobby");
        
        joinButton.interactable = false;
    }
}
