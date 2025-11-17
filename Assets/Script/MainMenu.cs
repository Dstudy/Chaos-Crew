using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MainMenu : MonoBehaviour
{
    [FormerlySerializedAs("networkGamePlayer")] [SerializeField] private NetworkManagerLobby networkManager = null;
    
    [Header("UI")]
    [SerializeField] private GameObject landingPagePanel = null;

    public void HostLobby()
    {
        networkManager.StartHost();
        
        landingPagePanel.SetActive(false);
    }
}
