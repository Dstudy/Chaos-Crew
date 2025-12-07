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
            
            string ipAddress = ipAddressInputField.text.Trim();
            
            if (string.IsNullOrEmpty(ipAddress))
            {
                Debug.LogError("IP address cannot be empty!");
                return;
            }
        
            manager.networkAddress = ipAddress;
            
            // Get port for logging
            int port = 7777;
            if (manager.transport is Mirror.PortTransport portTransport)
            {
                port = portTransport.Port;
            }
            
            Debug.Log($"=== CLIENT CONNECTING ===");
            Debug.Log($"Attempting to connect to: {ipAddress}:{port}");
            Debug.Log($"Make sure:");
            Debug.Log($"  1. Server is running and listening on {ipAddress}:{port}");
            Debug.Log($"  2. Firewall allows connections on port {port}");
            Debug.Log($"  3. Both devices are on the same network");
            
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
