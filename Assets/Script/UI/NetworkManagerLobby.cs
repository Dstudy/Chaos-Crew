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
        [SerializeField] private NetworkGamePlayerLobby gamePlayerPrefab = null;
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
            Debug.Log("OnStartHost");
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
    }
}
