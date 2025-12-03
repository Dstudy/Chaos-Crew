using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.UI
{
    public class JoinLobbyMenu : MonoBehaviour
    {
        [SerializeField] private NetworkManagerLobby networkManager;
    
        [Header("UI")]
        [SerializeField] private GameObject landingPagePanel;
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private Button joinButton;

        private void OnEnable()
        {
            EnsureNetworkManager();
            NetworkManagerLobby.onClientConnected += HandleClientConnected;
            NetworkManagerLobby.onClientDisconnected += HandleClientDisconnected;
        
            ipAddressInputField.text = IPAddressManager.instance.GetLocalIPv4Address();
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
            var manager = EnsureNetworkManager();
            if (manager == null)
            {
                Debug.LogWarning("JoinLobbyMenu: NetworkManagerLobby missing; cannot join.");
                return;
            }
            
            string ipAddress = ipAddressInputField.text;
        
            manager.networkAddress = ipAddress;
            manager.StartClient();
        
            Debug.Log("Joining lobby");
        
            joinButton.interactable = false;
        }

        private NetworkManagerLobby EnsureNetworkManager()
        {
            if (networkManager != null && networkManager.gameObject != null) return networkManager;

            networkManager = NetworkManager.singleton as NetworkManagerLobby;
            if (networkManager == null)
            {
                networkManager = FindObjectOfType<NetworkManagerLobby>();
            }

            return networkManager;
        }
    }
}
