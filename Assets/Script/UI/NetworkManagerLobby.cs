using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.UI
{
    public class NetworkManagerLobby : NetworkManager
    {
        [SerializeField] private int minPlayers = 2;
        [SerializeField]private string menuScene = string.Empty;
        public static Action onClientConnected;
        public static Action onClientDisconnected;
        public static Action<NetworkConnectionToClient> OnServerReadied;
        public static Action OnServerStopped;
        private bool isShuttingDown;
        private readonly HashSet<int> pendingReadyConnections = new HashSet<int>();
    
        // [Header("Maps")]
        // [SerializeField] private int numberOfRounds = 1;
        // [SerializeField] private MapSet mapSet = null;
    
        [Header("Room")]
        [SerializeField] private NetworkRoomPlayerLobby lobbyPrefab = null;
    
        [Header("Game")]
        [SerializeField] private GameObject playerSpawnSystem = null;
        [SerializeField] private GameObject roundManagerPrefab = null;

        [SerializeField] private string gameScene = string.Empty;
    
        public List<NetworkRoomPlayerLobby> RoomPlayers { get; } = new List<NetworkRoomPlayerLobby>();
        public List<NetworkGamePlayerLobby> GamePlayers { get; } = new List<NetworkGamePlayerLobby>();
        public string MenuSceneName => menuScene;

        // Update player count display on all clients
        public void UpdatePlayerCountDisplay()
        {
            // Find all NetworkRoomPlayerLobby instances to ensure accurate count
            NetworkRoomPlayerLobby[] allPlayers = FindObjectsOfType<NetworkRoomPlayerLobby>();
            int playerCount = allPlayers.Length;
            
            // Update all player displays
            foreach (var player in allPlayers)
            {
                if (player != null && player.numText != null)
                {
                    player.numText.text = $"{playerCount.ToString()}/5";
                }
            }
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
        
            onClientConnected?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
        
            onClientDisconnected?.Invoke();
        }

        public override void OnClientError(TransportError error, string reason)
        {
            base.OnClientError(error, reason);
            
            Debug.LogError($"=== CLIENT CONNECTION ERROR ===");
            Debug.LogError($"Error: {error}");
            Debug.LogError($"Reason: {reason}");
            Debug.LogError($"Attempted to connect to: {networkAddress}");
            
            if (transport is PortTransport portTransport)
            {
                Debug.LogError($"Port: {portTransport.Port}");
            }
            
            // Detect timeout errors specifically
            bool isTimeout = error == TransportError.Timeout || 
                           (reason != null && (
                               reason.Contains("timeout") || 
                               reason.Contains("did not properly respond") ||
                               reason.Contains("failed to respond")));
            
            if (isTimeout)
            {
                Debug.LogError($"=== TIMEOUT ERROR DETECTED ===");
                Debug.LogError($"Connection attempt timed out - this usually means:");
                Debug.LogError($"1. FIREWALL IS BLOCKING (MOST LIKELY)");
                Debug.LogError($"   - Windows Firewall is silently dropping packets");
                Debug.LogError($"   - Check Windows Defender Firewall settings");
                Debug.LogError($"   - Add inbound rule for TCP port {((PortTransport)transport)?.Port ?? 7777}");
                Debug.LogError($"   - Or allow Unity/your game through firewall");
                Debug.LogError($"2. Server IP address might be wrong: {networkAddress}");
                Debug.LogError($"3. Server might not be listening on that IP interface");
                Debug.LogError($"4. Network routing issue - verify both devices are on same network");
                Debug.LogError($"");
                Debug.LogError($"QUICK FIX - Run in PowerShell (as Administrator):");
                Debug.LogError($"netsh advfirewall firewall add rule name=\"Unity Server {((PortTransport)transport)?.Port ?? 7777}\" dir=in action=allow protocol=TCP localport={((PortTransport)transport)?.Port ?? 7777}");
                Debug.LogError($"");
                Debug.LogError($"VERIFICATION STEPS:");
                Debug.LogError($"1. On the SERVER machine, verify it's listening:");
                Debug.LogError($"   netstat -an | findstr {((PortTransport)transport)?.Port ?? 7777}");
                Debug.LogError($"   Should show: 0.0.0.0:{((PortTransport)transport)?.Port ?? 7777} LISTENING");
                Debug.LogError($"2. On the CLIENT machine, try to ping the server:");
                Debug.LogError($"   ping {networkAddress}");
                Debug.LogError($"3. Check if you can reach the port (requires telnet or Test-NetConnection):");
                Debug.LogError($"   Test-NetConnection -ComputerName {networkAddress} -Port {((PortTransport)transport)?.Port ?? 7777}");
            }
            else
            {
                Debug.LogError($"Troubleshooting:");
                Debug.LogError($"1. Verify the server IP address is correct: {networkAddress}");
                Debug.LogError($"2. Check if the server is actually running and listening");
                Debug.LogError($"3. Check Windows Firewall - it may be blocking port {((PortTransport)transport)?.Port ?? 7777}");
                Debug.LogError($"4. Ensure both devices are on the same network");
                Debug.LogError($"5. Try pinging the server IP from this machine");
            }
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
                UpdatePlayerCountDisplay();
            }

            base.OnServerDisconnect(conn);
        }
    
        public override void OnStopServer()
        {
            OnServerStopped?.Invoke();

            RoomPlayers.Clear();
            GamePlayers.Clear();
            pendingReadyConnections.Clear();
        }
        public override void OnStopClient()
        {
            base.OnStopClient();
            ReturnToMenuScene();
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            ReturnToMenuScene();
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
            if (SceneManager.GetActiveScene().name == menuScene)
            {
                bool isLeader = RoomPlayers.Count == 0;
            
                NetworkRoomPlayerLobby roomPlayerInstance = Instantiate(lobbyPrefab);
                roomPlayerInstance.IsLeader = isLeader;
            
                NetworkServer.AddPlayerForConnection(conn, roomPlayerInstance.gameObject);
                
                // Update display after player is added (called from OnStartClient, but ensure it happens)
                StartCoroutine(UpdatePlayerCountNextFrame());
            }
            else
            {
                // In game scene, PlayerSpawnSystem handles player spawning
                // If a player already exists (from ReplacePlayerForConnection), ignore this call
                if (conn.identity != null)
                {
                    Debug.Log($"OnServerAddPlayer called in game scene for connection {conn.connectionId}, but player already exists. Ignoring.");
                    return;
                }
            
                // If no player exists yet, PlayerSpawnSystem will handle it via OnServerReadied
                Debug.LogWarning($"OnServerAddPlayer called in game scene for connection {conn.connectionId} but no identity exists. PlayerSpawnSystem should handle this.");
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
                EnsureRoundManagerExists();
            }
        }
    
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);

            // Only spawn game players when in a game scene
            var activeScene = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(activeScene) && activeScene.StartsWith("Scene_Map"))
            {
                if (pendingReadyConnections.Add(conn.connectionId))
                {
                    StartCoroutine(InvokeReadyNextFrame(conn));
                }
            }
            else
            {
                Debug.Log($"OnServerReady ignored in scene {activeScene}");
            }
        }

        private IEnumerator InvokeReadyNextFrame(NetworkConnectionToClient conn)
        {
            yield return null; // defer to avoid modifying collections during Mirror broadcast
            pendingReadyConnections.Remove(conn.connectionId);
            OnServerReadied?.Invoke(conn);
        }

        private IEnumerator UpdatePlayerCountNextFrame()
        {
            yield return null; // defer to ensure RoomPlayers list is updated
            UpdatePlayerCountDisplay();
        }

        private void EnsureRoundManagerExists()
        {
            if (RoundManager.instance != null)
            {
                return;
            }

            if (roundManagerPrefab == null)
            {
                Debug.LogWarning("NetworkManagerLobby: roundManagerPrefab is not assigned, cannot spawn RoundManager.");
                return;
            }

            GameObject roundManagerInstance = Instantiate(roundManagerPrefab);
            NetworkServer.Spawn(roundManagerInstance);
            Debug.Log("NetworkManagerLobby: Spawned RoundManager in game scene.");
        }

        public override void OnStartHost()
        {
            isShuttingDown = false;
            Debug.Log("=== HOST STARTING ===");
            Debug.Log($"OnStartHost called");
            base.OnStartHost();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // Debug server status
            Debug.Log($"=== SERVER STARTED ===");
            Debug.Log($"NetworkServer.active: {NetworkServer.active}");
            Debug.Log($"NetworkServer.listen: {NetworkServer.listen}");
            
            // Debug transport info
            if (transport != null)
            {
                Debug.Log($"Transport Type: {transport.GetType().Name}");
                Debug.Log($"Transport Server Active: {transport.ServerActive()}"); // Call on transport, not portTransport
                if (transport is PortTransport portTransport)
                {
                    Debug.Log($"Transport Port: {portTransport.Port}");
                }
            }
            else
            {
                Debug.LogError("Transport is NULL!");
            }
            
            // Debug IP address - get all available IPs
            if (IPAddressManager.instance != null)
            {
                string localIP = IPAddressManager.instance.GetLocalIPv4Address();
                Debug.Log($"Server IP Address: {localIP}");
                
                // Log all network interfaces for troubleshooting
                IPAddressManager.instance.LogAllIPv4Addresses();
                
                if (transport is PortTransport pt)
                {
                    Debug.Log($"=== SERVER LISTENING ===");
                    Debug.Log($"Clients should connect to: {localIP}:{pt.Port}");
                    Debug.Log($"Server is listening on ALL interfaces (0.0.0.0:{pt.Port})");
                    Debug.Log($"=== FIREWALL TROUBLESHOOTING ===");
                    Debug.Log($"If clients cannot connect, check Windows Firewall:");
                    Debug.Log($"1. Open Windows Defender Firewall");
                    Debug.Log($"2. Click 'Allow an app or feature through Windows Defender Firewall'");
                    Debug.Log($"3. Find Unity/your game and ensure both Private and Public are checked");
                    Debug.Log($"4. Or add an inbound rule for port {pt.Port} (TCP)");
                    Debug.Log($"5. Run as Administrator: netsh advfirewall firewall add rule name=\"Unity Server {pt.Port}\" dir=in action=allow protocol=TCP localport={pt.Port}");
                    Debug.Log($"=== ANDROID EMULATOR NOTE ===");
                    Debug.Log($"If hosting from Android emulator:");
                    Debug.Log($"- Emulator may use virtual network adapter");
                    Debug.Log($"- Ensure both devices are on the same physical network");
                    Debug.Log($"- Try using the host machine's IP (not emulator's virtual IP)");
                    Debug.Log($"- Verify server is listening: netstat -an | findstr {pt.Port}");
                }
            }
            else
            {
                Debug.LogWarning("IPAddressManager instance not found - cannot display server IP");
            }
            
            Debug.Log($"Max Connections: {maxConnections}");
        }

        public override void OnClientSceneChanged()
        {
            // Don't auto-create player in game scenes - PlayerSpawnSystem handles it
            if (SceneManager.GetActiveScene().name.StartsWith("Scene_Map"))
            {
                // Still need to set client ready
                if (NetworkClient.connection.isAuthenticated && !NetworkClient.ready)
                {
                    NetworkClient.Ready();
                }
                // Don't call base.OnClientSceneChanged() which would auto-add player
                return;
            }
        
            // For menu scene, use default behavior
            base.OnClientSceneChanged();
        }

        public void ShutdownAndReturnToMenu()
        {
            if (isShuttingDown) return;
            isShuttingDown = true;

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StopHost();
            }
            else if (NetworkServer.active)
            {
                StopServer();
            }
            else if (NetworkClient.isConnected)
            {
                StopClient();
            }
            else
            {
                ReturnToMenuScene();
            }
        }

        public void ReturnToMenuForAll()
        {
            if (string.IsNullOrWhiteSpace(menuScene)) return;

            // Change scene for all connected clients but keep server running
            if (NetworkServer.active)
            {
                ServerChangeScene(menuScene);
            }
            else if (!NetworkClient.active)
            {
                // fallback for offline usage
                ReturnToMenuScene();
            }
        }

        private void ReturnToMenuScene()
        {
            // allow future shutdowns after returning to menu
        isShuttingDown = false;if (string.IsNullOrWhiteSpace(menuScene))
            {
                return;
            }

            if (SceneManager.GetActiveScene().name != menuScene)
            {
                SceneManager.LoadScene(menuScene);
            }
        }

        // Add this public method to check server status anytime
        public void DebugServerStatus()
        {
            Debug.Log("=== SERVER STATUS DEBUG ===");
            Debug.Log($"NetworkServer.active: {NetworkServer.active}");
            Debug.Log($"NetworkServer.listen: {NetworkServer.listen}");
            Debug.Log($"NetworkClient.active: {NetworkClient.active}");
            Debug.Log($"NetworkClient.isConnected: {NetworkClient.isConnected}");
            Debug.Log($"Mode: {mode}");
            
            if (transport != null)
            {
                Debug.Log($"Transport: {transport.GetType().Name}");
                Debug.Log($"Transport Server Active: {transport.ServerActive()}"); // Call on transport directly
                
                if (transport is PortTransport portTransport)
                {
                    Debug.Log($"Port: {portTransport.Port}");
                }
            }
            else
            {
                Debug.LogError("Transport is NULL!");
            }
            
            if (IPAddressManager.instance != null)
            {
                Debug.Log($"Local IP: {IPAddressManager.instance.GetLocalIPv4Address()}");
            }
            
            // Check if port is actually listening
            int port = ((PortTransport)transport)?.Port ?? 7777;
            Debug.Log($"Run 'netstat -an | findstr {port}' to verify port is listening");
        }

        // Add Update method to periodically check server status (optional, for debugging)
        private void Update()
        {
            // Only log once per second to avoid spam
            if (Time.frameCount % 60 == 0 && NetworkServer.active)
            {
                if (transport is PortTransport portTransport)
                {
                    Debug.Log($"[Server Status] Active: {NetworkServer.active}, Port: {portTransport.Port}");
                }
            }
        }
    }
}
