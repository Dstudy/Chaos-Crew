using Mirror;
using Mirror.Discovery;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Script.UI
{
    public class JoinLobbyMenu : MonoBehaviour
    {
        [SerializeField] private NetworkManagerLobby networkManager;
    
        [Header("UI")]
        [SerializeField] private GameObject landingPagePanel;
        [SerializeField] private TMP_InputField ipAddressInputField;
        [SerializeField] private Button joinButton;

        private bool isDiscovering = false;
        private bool hasFoundServer = false;

        private void OnEnable()
        {
            EnsureNetworkManager();
            NetworkManagerLobby.onClientConnected += HandleClientConnected;
            NetworkManagerLobby.onClientDisconnected += HandleClientDisconnected;
        
            // Don't auto-fill with local IP - leave empty for discovery or manual entry
            ipAddressInputField.text = "";
            
            // Setup discovery listener if available
            if (networkManager != null && networkManager.networkDiscovery != null)
            {
                networkManager.networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
            }
        }

        private void OnDisable()
        {
            NetworkManagerLobby.onClientConnected -= HandleClientConnected;
            NetworkManagerLobby.onClientDisconnected -= HandleClientDisconnected;
            
            if (networkManager != null && networkManager.networkDiscovery != null)
            {
                networkManager.networkDiscovery.OnServerFound.RemoveListener(OnDiscoveredServer);
                StopDiscovery();
            }
        }

        private void HandleClientConnected()
        {
            Debug.Log("Connected to server");
            joinButton.interactable = true;
            StopDiscovery();
        
            gameObject.SetActive(false);
            landingPagePanel.SetActive(false);
        }

        private void HandleClientDisconnected()
        {
            joinButton.interactable = true;
        }

        // Called when a server is discovered via network discovery
        private void OnDiscoveredServer(ServerResponse response)
        {
            if (hasFoundServer)
            {
                return; // Already connecting to a server
            }

            hasFoundServer = true;
            StopDiscovery();

            // Extract IP address from the discovered server
            string serverIP = response.EndPoint.Address.ToString();

            // Update the input field with discovered IP
            ipAddressInputField.text = serverIP;
            
            // Connect to the discovered server
            ConnectToServer(response);
        }

        // Connect to a server using the ServerResponse
        private void ConnectToServer(ServerResponse serverInfo)
        {
            var manager = EnsureNetworkManager();
            if (manager == null)
            {
                Debug.LogWarning("JoinLobbyMenu: NetworkManagerLobby missing; cannot join.");
                return;
            }
            
            joinButton.interactable = false;
            
            // Connect using the URI from discovery (includes IP and port)
            manager.StartClient(serverInfo.uri);
        }

        public void JoinLobby()
        {
            Debug.Log("Joining Lobby");
            var manager = EnsureNetworkManager();
            if (manager == null)
            {
                Debug.LogWarning("JoinLobbyMenu: NetworkManagerLobby missing; cannot join.");
                return;
            }

            // Check if network discovery is available
            if (manager.networkDiscovery != null)
            {
                // Start discovery to find servers automatically
                StartDiscovery();
                
                // Wait a few seconds for discovery, then fall back to manual IP if needed
                StartCoroutine(DiscoveryTimeoutCoroutine());
            }
            else
            {
                Debug.Log("join chay");
                // Fallback to manual IP entry if discovery is not available
                JoinWithManualIP();
            }
        }

        private void StartDiscovery()
        {
            if (isDiscovering || hasFoundServer)
            {
                return;
            }

            isDiscovering = true;
            hasFoundServer = false;

            if (networkManager.networkDiscovery != null)
            {
                Debug.Log("=== STARTING SERVER DISCOVERY ===");
                networkManager.networkDiscovery.StartDiscovery();
            }
        }

        private void StopDiscovery()
        {
            if (networkManager != null && networkManager.networkDiscovery != null && isDiscovering)
            {
                networkManager.networkDiscovery.StopDiscovery();
                isDiscovering = false;
            }
        }

        private IEnumerator DiscoveryTimeoutCoroutine()
        {
            // Wait up to 3 seconds for discovery
            float timeout = 3f;
            float elapsed = 0f;

            while (elapsed < timeout && !hasFoundServer)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            StopDiscovery();

            if (!hasFoundServer)
            {
                Debug.Log("No server found via discovery. Attempting manual connection with entered IP...");
                JoinWithManualIP();
            }
        }

        private void JoinWithManualIP()
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
                Debug.LogError("IP address cannot be empty! Please enter a server IP address.");
                joinButton.interactable = true;
                return;
            }
        
            manager.networkAddress = ipAddress;
            
            // Get port for logging
            int port = 7777;
            if (manager.transport is Mirror.PortTransport portTransport)
            {
                port = portTransport.Port;
            }
            
            
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