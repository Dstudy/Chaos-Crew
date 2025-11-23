using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;
using Mirror.Examples;
using Random = System.Random;

public class SpawnSystem : NetworkBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private List<WaveSpawn> waves = new List<WaveSpawn>();
    [SerializeField] private GameObject draggableItemPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float waveStartDelay = 1f;
    [SerializeField] private bool autoStartWaves = true;
    
    private int currentWaveIndex = 0;
    private bool isSpawning = false;

    [SerializeField] private float shootForce = 10f;
    
    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    public static event Action OnAllWavesCompleted;

    private int localID = 0;

    private int readyPlayersCount;
    private bool wavesStarted;

    private int itemSpawnCounter = 0;

    public override void OnStartServer()
    {
        Debug.Log("Spawn system started on server");
        NetworkManagerLobby.OnServerReadied += OnPlayerReady;
        readyPlayersCount = 0;
        wavesStarted = false;
    }

    public override void OnStopServer()
    {
        NetworkManagerLobby.OnServerReadied -= OnPlayerReady;
    }

    private void OnPlayerReady(NetworkConnectionToClient conn)
    {
        if (!isServer || wavesStarted) return;
        
        readyPlayersCount++;
        Debug.Log($"Player ready: {readyPlayersCount}/{NetworkServer.connections.Count}");
        
        // Wait for all connections to be ready
        if (readyPlayersCount >= NetworkServer.connections.Count && NetworkServer.connections.Count > 0)
        {
            StartCoroutine(StartWaveAfterDelay());
            // StartNextWave();
            wavesStarted = true;
        }
    }
    private IEnumerator StartWaveAfterDelay()
    {
        Debug.Log("SpawnStart");
        yield return new WaitForSeconds(2f);
        StartNextWave();
    }

    [Server]
    public void StartNextWave()
    {
        if (isSpawning)
        {
            Debug.LogWarning("Wave spawning is already in progress.");
            return;
        }

        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("All waves completed!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        WaveSpawn currentWave = waves[currentWaveIndex];
        if (currentWave == null)
        {
            Debug.LogError($"Wave {currentWaveIndex} is null!");
            currentWaveIndex++;
            return;
        }

        StartCoroutine(SpawnWave(currentWave));
    }

    [Server]
    private IEnumerator SpawnWave(WaveSpawn wave)
    {
        isSpawning = true;
        OnWaveStarted?.Invoke(wave.waveNumber);
        
        Debug.Log($"Starting wave {wave.waveNumber} with spawn type: {wave.spawnType}");

        List<Player> players = GetAllPlayers();
        
        if (players.Count == 0)
        {
            Debug.LogWarning("No players found to spawn items for!");
            isSpawning = false;
            yield break;
        }

        switch (wave.spawnType)
        {
            case SpawnType.AllAttackItemsPerElement:
                yield return StartCoroutine(SpawnAllAttackItemsPerElement(wave, players));
                break;
            case SpawnType.AttackAndSupport:
                yield return StartCoroutine(SpawnAttackAndSupport(wave, players));
                break;
            case SpawnType.Augment:
                yield return StartCoroutine(SpawnAugment(wave, players));
                break;
        }

        isSpawning = false;
        OnWaveCompleted?.Invoke(wave.waveNumber);
        currentWaveIndex++;
    }

    [Server]
    private List<Player> GetAllPlayers()
    {
        List<Player> players = new List<Player>();
        
        if (NetworkManager.singleton is NetworkManagerLobby lobby)
        {
            foreach (var gamePlayer in lobby.GamePlayers)
            {
                if (gamePlayer != null && gamePlayer.gameObject != null)
                {
                    // NetworkGamePlayerLobby might be a separate object, try to find Player component
                    Player player = gamePlayer.GetComponent<Player>();
                    if (player == null)
                    {
                        // Try to find Player in children or parent
                        player = gamePlayer.GetComponentInChildren<Player>();
                        if (player == null)
                        {
                            player = gamePlayer.GetComponentInParent<Player>();
                        }
                    }
                    
                    if (player != null)
                    {
                        players.Add(player);
                    }
                }
            }
        }
        
        // Fallback: Find all Player objects in scene
        if (players.Count == 0)
        {
            Player[] allPlayers = FindObjectsOfType<Player>();
            players.AddRange(allPlayers);
        }
        
        return players;
    }

    [Server]
    private IEnumerator SpawnAllAttackItemsPerElement(WaveSpawn wave, List<Player> players)
    {
        if (EnemyManager.instance == null)
        {
            Debug.LogError("EnemyManager instance is null!");
            yield break;
        }

        if (wave.attackItemData == null || wave.attackItemData.Count == 0)
        {
            Debug.LogWarning("No attack item data in wave!");
            yield break;
        }

        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }

        // Get unique elements
        HashSet<Element> uniqueElements = new HashSet<Element>(elements);
        uniqueElements.Remove(Element.None); // Remove None element

        // Convert to list for easier iteration
        List<Element> elementsList = new List<Element>(uniqueElements);
        
        // Determine how many items to spawn per player
        int itemsPerPlayer = wave.itemsPerPlayer > 0 ? wave.itemsPerPlayer : elementsList.Count;
        int totalItemsToSpawn = Mathf.Min(itemsPerPlayer, elementsList.Count);

        // Spawn items for all players simultaneously
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            Element element = elementsList[itemIndex];
            
            // Find attack item data for this element
            AttackItemData itemData = ItemManager.Instance.getAttackItem(element);
            
            if (itemData == null)
            {
                Debug.LogWarning($"No attack item data found for element {element}");
                continue;
            }

            // Spawn item for ALL players at the same time
            foreach (Player player in players)
            {
                SpawnItemForPlayer(player, itemData.CreateAttackItem(), wave.spawnOffset);
            }
            
            // Wait after spawning this item for all players
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

    [Server]
    private IEnumerator SpawnAttackAndSupport(WaveSpawn wave, List<Player> players)
    {
        if (wave.attackItemData == null || wave.attackItemData.Count == 0)
        {
            Debug.LogWarning("No attack item data in wave!");
            yield break;
        }

        if (wave.supportItemData == null || wave.supportItemData.Count == 0)
        {
            Debug.LogWarning("No support item data in wave!");
            yield break;
        }

        // Determine how many items to spawn per player
        int itemsPerPlayer = wave.itemsPerPlayer > 0 ? wave.itemsPerPlayer : 2;
        
        // Prepare item lists for each player (different items per player)
        Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            
            // First item: Attack
            AttackItemData attackData = wave.attackItemData[UnityEngine.Random.Range(0, wave.attackItemData.Count)];
            if (attackData != null)
            {
                itemsToSpawn.Add(attackData.CreateAttackItem());
            }

            // Second item: Support
            SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
            if (supportData != null)
            {
                itemsToSpawn.Add(supportData.CreateSupportItem());
            }
            
            // Fill remaining slots with random attack or support items
            while (itemsToSpawn.Count < itemsPerPlayer)
            {
                bool spawnAttack = UnityEngine.Random.Range(0, 2) == 0;
                
                if (spawnAttack && wave.attackItemData != null && wave.attackItemData.Count > 0)
                {
                    AttackItemData randomAttackData = wave.attackItemData[UnityEngine.Random.Range(0, wave.attackItemData.Count)];
                    if (randomAttackData != null)
                    {
                        itemsToSpawn.Add(randomAttackData.CreateAttackItem());
                        continue;
                    }
                }
                
                if (wave.supportItemData != null && wave.supportItemData.Count > 0)
                {
                    SupportItemData randomSupportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                    if (randomSupportData != null)
                    {
                        itemsToSpawn.Add(randomSupportData.CreateSupportItem());
                        continue;
                    }
                }
                
                break;
            }
            
            playerItems[player] = itemsToSpawn;
        }

        // Find the maximum number of items any player will receive
        int maxItems = 0;
        foreach (var kvp in playerItems)
        {
            if (kvp.Value.Count > maxItems)
                maxItems = kvp.Value.Count;
        }

        // Spawn all items for all players simultaneously
        for (int itemIndex = 0; itemIndex < maxItems; itemIndex++)
        {
            // Spawn this item index for ALL players at the same time
            foreach (Player player in players)
            {
                if (playerItems.ContainsKey(player) && itemIndex < playerItems[player].Count)
                {
                    BaseItem item = playerItems[player][itemIndex];
                    SpawnItemForPlayer(player, item, wave.spawnOffset);
                }
            }
            
            // Wait after spawning this item for all players
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

    [Server]
    private IEnumerator SpawnAugment(WaveSpawn wave, List<Player> players)
    {
        if (wave.augmentData == null || wave.augmentData.Count == 0)
        {
            Debug.LogWarning("No augment data in wave!");
            yield break;
        }

        // Prepare augment items for each player (different augments per player)
        Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            
            for (int i = 0; i < wave.itemsPerPlayer; i++)
            {
                AugmentData augmentData = wave.augmentData[UnityEngine.Random.Range(0, wave.augmentData.Count)];
                if (augmentData != null)
                {
                    itemsToSpawn.Add(augmentData.CreateAugment());
                }
            }
            
            playerItems[player] = itemsToSpawn;
        }

        // Spawn all augments for all players simultaneously
        for (int itemIndex = 0; itemIndex < wave.itemsPerPlayer; itemIndex++)
        {
            // Spawn this augment index for ALL players at the same time
            foreach (Player player in players)
            {
                if (playerItems.ContainsKey(player) && itemIndex < playerItems[player].Count)
                {
                    BaseItem item = playerItems[player][itemIndex];
                    SpawnItemForPlayer(player, item, wave.spawnOffset);
                }
            }
            
            // Wait after spawning this augment for all players
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

    [Server]
    private void SpawnItemForPlayer(Player player, BaseItem item, Vector3 offset)
    {
        if (player == null || item == null || draggableItemPrefab == null)
        {
            Debug.LogError("Cannot spawn item: player, item, or prefab or map is null!");
            return;
        }

        Transform spawnPosition = GetSpawnPoint(player.map);
        
        // Get item from pool or create new one
        GameObject dragItem = PrefabPool.singleton.Get(spawnPosition.position, spawnPosition.rotation);
        DraggableItem draggableItem = dragItem.GetComponent<DraggableItem>();

        if (draggableItem != null)
        {
            itemSpawnCounter++;
            string itemName = $"{item.name} - Wave {currentWaveIndex + 1} - #{itemSpawnCounter}";
            if (player != null)
            {
                itemName = $"{item.name} - Player {player.id} - Wave {currentWaveIndex + 1}";
            }
            draggableItem.gameObject.name = itemName;
            draggableItem.GetComponent<PolygonCollider2D>().points = item.collider2D.points;
            draggableItem.transform.position = spawnPosition.position;
            
            if (draggableItem.TryGetComponent(out NetworkIdentity netIdentity))
            {
                if (NetworkServer.active && !NetworkServer.spawned.ContainsKey(netIdentity.netId))
                {
                    NetworkServer.Spawn(draggableItem.gameObject);
                    Debug.Log("Da spawn object " + draggableItem.gameObject.name);
                    Vector2 shootDirection = spawnPosition.up;
                    draggableItem.RpcShoot(shootDirection, shootForce);
                    draggableItem.SetItem(item);
                }
            }
        }
        else
        {
            Debug.LogError("Failed to get or create DraggableItem from pool!");
        }
    }
    

    private Transform GetSpawnPoint(MapManager map)
    {
        Random random = new Random();
        int randomIndex = random.Next(map.spawnItemPoints.Count);
        Transform spawnPoint = map.spawnItemPoints[randomIndex];
        return spawnPoint;
    }

    [Server]
    public void SetWaves(List<WaveSpawn> newWaves)
    {
        waves = newWaves;
        currentWaveIndex = 0;
    }

    [Server]
    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    [Server]
    public int GetTotalWaves()
    {
        return waves.Count;
    }
}
