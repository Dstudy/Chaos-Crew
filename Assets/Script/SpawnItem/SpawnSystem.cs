using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;
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
            wavesStarted = true;
        }
    }
    private IEnumerator StartWaveAfterDelay()
    {
        yield return new WaitForSeconds(waveStartDelay);
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

        foreach (Player player in players)
        {
            int itemsSpawned = 0;
            foreach (Element element in uniqueElements)
            {
                if (itemsSpawned >= wave.itemsPerPlayer && wave.itemsPerPlayer > 0)
                    break;
                    
                // Find attack item data for this element
                AttackItemData itemData = ItemManager.Instance.getAttackItem(element);
                
                if (itemData == null)
                {
                    Debug.LogWarning($"No attack item data found for element {element}");
                    continue;
                }

                // Spawn item for this player
                SpawnItemForPlayer(player, itemData.CreateAttackItem(), wave.spawnOffset);
                itemsSpawned++;
                
                yield return new WaitForSeconds(wave.spawnDelay);
            }
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

        foreach (Player player in players)
        {
            int itemsSpawned = 0;
            
            // Spawn attack item
            if (itemsSpawned < wave.itemsPerPlayer || wave.itemsPerPlayer <= 0)
            {
                AttackItemData attackData = wave.attackItemData[UnityEngine.Random.Range(0, wave.attackItemData.Count)];
                if (attackData != null)
                {
                    SpawnItemForPlayer(player, attackData.CreateAttackItem(), wave.spawnOffset);
                    itemsSpawned++;
                    yield return new WaitForSeconds(wave.spawnDelay);
                }
            }

            // Spawn support item
            if (itemsSpawned < wave.itemsPerPlayer || wave.itemsPerPlayer <= 0)
            {
                SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                if (supportData != null)
                {
                    SpawnItemForPlayer(player, supportData.CreateSupportItem(), wave.spawnOffset);
                    itemsSpawned++;
                    yield return new WaitForSeconds(wave.spawnDelay);
                }
            }
            
            // Fill remaining slots with random attack or support items
            while ((wave.itemsPerPlayer > 0 && itemsSpawned < wave.itemsPerPlayer) || (wave.itemsPerPlayer <= 0 && itemsSpawned < 2))
            {
                bool spawnAttack = UnityEngine.Random.Range(0, 2) == 0;
                
                if (spawnAttack && wave.attackItemData != null && wave.attackItemData.Count > 0)
                {
                    AttackItemData attackData = wave.attackItemData[UnityEngine.Random.Range(0, wave.attackItemData.Count)];
                    if (attackData != null)
                    {
                        SpawnItemForPlayer(player, attackData.CreateAttackItem(), wave.spawnOffset);
                        itemsSpawned++;
                        yield return new WaitForSeconds(wave.spawnDelay);
                        continue;
                    }
                }
                
                if (wave.supportItemData != null && wave.supportItemData.Count > 0)
                {
                    SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                    if (supportData != null)
                    {
                        SpawnItemForPlayer(player, supportData.CreateSupportItem(), wave.spawnOffset);
                        itemsSpawned++;
                        yield return new WaitForSeconds(wave.spawnDelay);
                        continue;
                    }
                }
                
                break;
            }
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

        foreach (Player player in players)
        {
            for (int i = 0; i < wave.itemsPerPlayer; i++)
            {
                AugmentData augmentData = wave.augmentData[UnityEngine.Random.Range(0, wave.augmentData.Count)];
                if (augmentData != null)
                {
                    SpawnItemForPlayer(player, augmentData.CreateAugment(), wave.spawnOffset);
                    yield return new WaitForSeconds(wave.spawnDelay);
                }
            }
        }
    }

    [Server]
    private void SpawnItemForPlayer(Player player, BaseItem item, Vector3 offset)
    {
        
        if (player == null || item == null || draggableItemPrefab == null)
        {
            Debug.LogError("Cannot spawn item: player, item, or prefab is null!");
            return;
        }

        Transform spawnPosition = GetSpawnPoint(player.map);
        
        // Get item from pool or create new one
        DraggableItem draggableItem = ObjectPoolManager.instance.Get<DraggableItem>(
            CONST.DRAGGABLE_ITEM,
            null,
            () =>
            {
                GameObject itemObj = Instantiate(draggableItemPrefab, spawnPosition.position, Quaternion.identity);
                //Thu ca 2 ne
                // ShootSpawnObject(itemObj, player.map);
                return itemObj.GetComponent<DraggableItem>();
            }
        );

        if (draggableItem != null)
        {
            draggableItem.GetComponent<PolygonCollider2D>().points = item.collider2D.points;
            draggableItem.transform.position = spawnPosition.position;
            
            // If DraggableItem has NetworkIdentity, spawn it on network
            // Otherwise, it will be visible to all clients if scene is shared
            if (draggableItem.TryGetComponent(out NetworkIdentity netIdentity))
            {
                if (NetworkServer.active && !NetworkServer.spawned.ContainsKey(netIdentity.netId))
                {
                    NetworkServer.Spawn(draggableItem.gameObject);
                    //Thu ca 2 ne
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
