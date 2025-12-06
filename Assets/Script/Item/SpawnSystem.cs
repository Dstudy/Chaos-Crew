using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;
using Script.Enemy;
using Script.UI;
using Random = System.Random;
using static CONST;

// Server-side item instance tracking
[Serializable]
public class ServerItemInstance
{
    public int instanceId;
    public int itemId; // BaseItem.id
    public string ownerPlayerId; // Player.id
    public BaseItem itemData; // Full item data for server reference
    public Vector3 spawnPosition;
    public int spawnPointIndex;
    
    
    // State data that might change
    public int charges; // For items with charges (like StaffItem)
    public Element currentElement; // For items that can change element
    
    public ServerItemInstance(int instanceId, int itemId, string ownerPlayerId, BaseItem itemData, Vector3 spawnPosition, int spawnPointIndex)
    {
        this.instanceId = instanceId;
        this.itemId = itemId;
        this.ownerPlayerId = ownerPlayerId;
        this.itemData = itemData;
        this.spawnPosition = spawnPosition;
        this.spawnPointIndex = spawnPointIndex;
        
        // Initialize state from item data
        if (itemData is StaffItem staffItem)
        {
            this.charges = staffItem.charges;
            this.currentElement = staffItem.element;
        }
        else if (itemData is AttackItem attackItem)
        {
            this.currentElement = attackItem.element;
        }
    }
}

public class SpawnSystem : NetworkBehaviour
{
    public static SpawnSystem singleton;
    
    [Header("Wave Configuration")]
    [SerializeField] private List<WaveSpawn> waves = new List<WaveSpawn>();
    [SerializeField] public GameObject draggableItemPrefab;
    
    [SerializeField] private List<WaveSpawn> rewardWaves = new List<WaveSpawn>();
    
    [Header("Spawn Settings")]
    [SerializeField] private float waveStartDelay = 1f;
    [SerializeField] private bool autoStartWaves = true;
    
    private int currentWaveIndex = 0;
    private int rewardWaveIndex = 0;
    private bool isSpawning = false;

    [SerializeField] private float shootForce = 5f;
    [SerializeField] public GameObject meow;
    public static event Action<int> OnWaveStarted;
    public static event Action<int> OnWaveCompleted;
    public static event Action OnAllWavesCompleted;

    private int localID = 0;

    private int readyPlayersCount;
    private bool wavesStarted;
    
    
    // Server-side item tracking
    private Dictionary<int, ServerItemInstance> allItemInstances = new Dictionary<int, ServerItemInstance>();
    private Dictionary<string, List<int>> playerItemInstances = new Dictionary<string, List<int>>(); // playerId -> list of instanceIds
    private int nextInstanceId = 1;

    private void OnEnable()
    {
        if (isServer)
        {
            ObserverManager.Register(ENEMY_DEFEATED, (Action<Enemy>) OnEnemyDefeacted);
        }
    }

    private void OnDisable()
    {
        if (isServer)
        {
            ObserverManager.Unregister(ENEMY_DEFEATED, (Action<Enemy>) OnEnemyDefeacted);
        }
    }

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else if (singleton != this)
        {
            Debug.LogWarning("Multiple SpawnSystem instances found!");
        }
    }
    
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
    
    [Server]
    private NetworkConnectionToClient GetPlayerConnection(Player player)
    {
        if (player == null) return null;
        NetworkIdentity playerIdentity = player.GetComponent<NetworkIdentity>();
        return playerIdentity?.connectionToClient;
    }
    
    [Server]
    public void RegisterItemInstance(ServerItemInstance instance)
    {
        allItemInstances[instance.instanceId] = instance;
        
        if (!playerItemInstances.ContainsKey(instance.ownerPlayerId))
        {
            playerItemInstances[instance.ownerPlayerId] = new List<int>();
        }
        playerItemInstances[instance.ownerPlayerId].Add(instance.instanceId);
    }
    
    [Server]
    public void UnregisterItemInstance(int instanceId)
    {
        if (allItemInstances.TryGetValue(instanceId, out ServerItemInstance instance))
        {
            allItemInstances.Remove(instanceId);
            if (playerItemInstances.ContainsKey(instance.ownerPlayerId))
            {
                playerItemInstances[instance.ownerPlayerId].Remove(instanceId);
            }
        }
    }
    
    [Server]
    public ServerItemInstance GetItemInstance(int instanceId)
    {
        allItemInstances.TryGetValue(instanceId, out ServerItemInstance instance);
        return instance;
    }
    
    [Server]
    public List<int> GetPlayerItemInstances(string playerId)
    {
        if (playerItemInstances.TryGetValue(playerId, out List<int> instances))
        {
            return new List<int>(instances);
        }
        return new List<int>();
    }
    private IEnumerator StartWaveAfterDelay()
    {
        Debug.Log("SpawnStart");

        float delay = Mathf.Max(0f, waveStartDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        while (PlayerManager.instance == null || PlayerManager.instance.players == null || PlayerManager.instance.players.Count == 0)
        {
            yield return null;
        }

        StartNextWave();
    }
    
    [Server]
    public void OnEnemyDefeatedServer(Enemy enemy)
    {
        // This method is called directly from Enemy on the server
        if (EnemyManager.instance != null)
        {
            EnemyManager.instance.NotifyEnemyDefeated();
        }
        else
        {
            Debug.LogWarning("EnemyManager instance is null; cannot track remaining enemies.");
        }

        OnEnemyDefeacted(enemy);
    }

    private void OnEnemyDefeacted(Enemy enemy)
    {
        List<GameObject> players = PlayerManager.instance.players.ToList();
        Player rewardPlayer = null;
        foreach (var player in players)
        {
            if (player.GetComponent<Player>().enemy == enemy)
            {
                rewardPlayer = player.GetComponent<Player>();
            }
        }


        StartCoroutine(SpawnRewards(rewardWaves, rewardPlayer));

    }

    IEnumerator SpawnRewards(List<WaveSpawn> rewardWaves, Player player)
    {
        foreach (var rewardWave in rewardWaves)
        {
            yield return StartCoroutine(SpawnWave(rewardWave, player));
        }
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
    public void ConfigureRound(List<WaveSpawn> newWaves, List<WaveSpawn> newRewardWaves, float startDelay, bool autoStart)
    {
        waves = newWaves ?? new List<WaveSpawn>();
        rewardWaves = newRewardWaves ?? new List<WaveSpawn>();
        waveStartDelay = Mathf.Max(0f, startDelay);
        autoStartWaves = autoStart;

        currentWaveIndex = 0;
        rewardWaveIndex = 0;
        isSpawning = false;
        wavesStarted = false;
        readyPlayersCount = 0;
    }

    [Server]
    public void StartRoundWaves()
    {
        Debug.Log("Start Round Spawn");
        if (isSpawning)
        {
            Debug.LogWarning("SpawnSystem: Cannot start round waves while spawning is in progress.");
            return;
        }

        StopAllCoroutines();
        wavesStarted = true;
        StartCoroutine(StartWaveAfterDelay());
    }

    [Server]
    private IEnumerator SpawnWave(WaveSpawn wave, Player player = null, int delayTime = 0)
    {
        isSpawning = true;
        OnWaveStarted?.Invoke(wave.waveNumber);
        
        Debug.Log($"Starting wave {wave.waveNumber} with spawn type: {wave.spawnType}");

        List<Player> players = GetAllPlayers();

        foreach (var foundPlayer in players)
        {
            Debug.Log(foundPlayer.name);
        }
        
        if (players.Count == 0)
        {
            Debug.LogWarning("No players found to spawn items for!");
            isSpawning = false;
            yield break;
        }
        
        yield return new WaitForSeconds(delayTime);

        switch (wave.spawnType)
        {
            case SpawnType.AttackAndShield:
                yield return StartCoroutine(SpawnAttackAndShield(wave, players));
                break;
            case SpawnType.AttackSupportStaff:
                yield return StartCoroutine(SpawnAttackSupportStaff(wave, players));
                break;
            case SpawnType.OnlyOne:
                yield return StartCoroutine(SpawnOnlyOne(wave, players));
                break;
            case SpawnType.RandomAll:
                yield return StartCoroutine(SpawnRandomAll(wave, players));
                break;
            case SpawnType.Reward:
                yield return StartCoroutine(SpawnReward(wave, player));
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
    
        Debug.Log($"GetAllPlayers found {players.Count} players from PlayerManager");
        // Fallback: Find all Player objects in scene
        if (players.Count == 0)
        {
            Player[] allPlayers = FindObjectsOfType<Player>();
            players.AddRange(allPlayers);
        }
        
        return players;
    }

    [Server]
    private IEnumerator SpawnOnlyOne(WaveSpawn wave, List<Player> players)
    {
        Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        int totalItemsToSpawn = wave.waveCount;
        
        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }

        int index = (int)wave.itemType;
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
            {
                switch (index)
                {
                    case 0:
                    {
                        AttackItemData attackData = wave.GetAttackItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (attackData != null)
                        {
                            itemsToSpawn.Add(attackData.CreateAttackItem());
                        }

                        break;
                    }
                    case 1:
                    {
                        SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                        if (supportData != null)
                        {
                            itemsToSpawn.Add(supportData.CreateSupportItem());
                        }
                        break;
                    }
                    case 2:
                    {
                        StaffItemData staffItemData = wave.GetStaffItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (staffItemData != null)
                        {
                            itemsToSpawn.Add(staffItemData.CreateStaffItem());
                        }
                        break;
                    }
                    case 3:
                    {
                        HammerData hammerData = wave.GetHammerItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (hammerData != null)
                        {
                            itemsToSpawn.Add(hammerData.CreateHammerItem());
                        }
                        break;
                    }
                }
            }
            
            playerItems[player] = itemsToSpawn;
        }
        
        
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            foreach (Player player in players)
            {
                if (playerItems[player].Count > itemIndex)
                {
                    SpawnItemForPlayer(player, playerItems[player][itemIndex], wave.spawnOffset);
                }
            }
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

    [Server]
    private IEnumerator SpawnAttackAndShield(WaveSpawn wave, List<Player> players)
    {
         Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        int totalItemsToSpawn = wave.waveCount;
        
        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }
        
        Debug.Log("So player la: " + players.Count);
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
            {
                Item itemType = GetWeightedRandomItemType(wave);
                switch (itemType)
                {
                    case Item.Attack:
                    {
                        AttackItemData attackData = wave.GetAttackItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (attackData != null)
                        {
                            itemsToSpawn.Add(attackData.CreateAttackItem());
                        }

                        break;
                    }
                    case Item.Support:
                    {
                        //Chỉ spawn ra heal thôi
                        SupportItemData supportData = wave.supportItemData[0];
                        if (supportData != null)
                        {
                            itemsToSpawn.Add(supportData.CreateSupportItem());
                        }
                        break;
                    }
                }
            }
            
            playerItems[player] = itemsToSpawn;
        }
        
        
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            foreach (Player player in players)
            {
                if (playerItems[player].Count > itemIndex)
                {
                    SpawnItemForPlayer(player, playerItems[player][itemIndex], wave.spawnOffset);
                }
            }
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }
    
    [Server]
    private IEnumerator SpawnRandomAll(WaveSpawn wave, List<Player> players)
    {
        Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        int totalItemsToSpawn = wave.waveCount;
        
        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }
        
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
            {
                Item itemType = GetWeightedRandomItemType(wave);
                switch (itemType)
                {
                    case Item.Attack:
                    {
                        AttackItemData attackData = wave.GetAttackItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (attackData != null)
                        {
                            itemsToSpawn.Add(attackData.CreateAttackItem());
                        }

                        break;
                    }
                    case Item.Support:
                    {
                        SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                        if (supportData != null)
                        {
                            itemsToSpawn.Add(supportData.CreateSupportItem());
                        }
                        break;
                    }
                    case Item.Staff:
                    {
                        StaffItemData staffItemData = wave.GetStaffItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (staffItemData != null)
                        {
                            itemsToSpawn.Add(staffItemData.CreateStaffItem());
                        }
                        break;
                    }
                    case Item.Hammer:
                    {
                        HammerData hammerData = wave.GetHammerItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (hammerData != null)
                        {
                            itemsToSpawn.Add(hammerData.CreateHammerItem());
                        }
                        break;
                    }
                }
            }
            
            playerItems[player] = itemsToSpawn;
        }
        
        
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            foreach (Player player in players)
            {
                if (playerItems[player].Count > itemIndex)
                {
                    SpawnItemForPlayer(player, playerItems[player][itemIndex], wave.spawnOffset);
                }
            }
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }
    
    private Item GetWeightedRandomItemType(WaveSpawn wave)
    {
        float attack = Mathf.Max(0f, wave.attackProbability);
        float support = Mathf.Max(0f, wave.supportProbability);
        float staff = Mathf.Max(0f, wave.staffProbability);
        float hammer = Mathf.Max(0f, wave.hammerProbability);

        float total = attack + support + staff + hammer;
        if (total <= 0f)
        {
            // Fall back to equal probability if weights are invalid
            return (Item)UnityEngine.Random.Range(0, 4);
        }

        float roll = UnityEngine.Random.value * total;
        if (roll < attack) return Item.Attack;
        roll -= attack;
        if (roll < support) return Item.Support;
        roll -= support;
        if (roll < staff) return Item.Staff;
        return Item.Hammer;
    }

    [Server]
    private IEnumerator SpawnReward(WaveSpawn wave, Player player)
    {
        Debug.Log(wave.name + " spawned!");
        int totalItemsToSpawn = wave.waveCount;
        
        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }
        
        List<BaseItem> itemsToSpawn = new List<BaseItem>();
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            int index = UnityEngine.Random.Range(0, 4);
            switch (index)
            {
                case 0:
                {
                    AttackItemData attackData = wave.GetAttackItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                    if (attackData != null)
                    {
                        itemsToSpawn.Add(attackData.CreateAttackItem());
                    }

                    break;
                }
                case 1:
                {
                    SupportItemData supportData = wave.supportItemData[UnityEngine.Random.Range(0, wave.supportItemData.Count)];
                    if (supportData != null)
                    {
                        itemsToSpawn.Add(supportData.CreateSupportItem());
                    }
                    break;
                }
                case 2:
                {
                    StaffItemData staffItemData = wave.GetStaffItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                    if (staffItemData != null)
                    {
                        itemsToSpawn.Add(staffItemData.CreateStaffItem());
                    }
                    break;
                }
                case 3:
                {
                    HammerData hammerData = wave.GetHammerItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                    if (hammerData != null)
                    {
                        itemsToSpawn.Add(hammerData.CreateHammerItem());
                    }
                    break;
                }
            }
        }
        
        
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            SpawnRewardForPlayer(player, itemsToSpawn[itemIndex]);
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

    [Server]
    private IEnumerator SpawnAttackSupportStaff(WaveSpawn wave, List<Player> players)
    {
       Dictionary<Player, List<BaseItem>> playerItems = new Dictionary<Player, List<BaseItem>>();
        int totalItemsToSpawn = wave.waveCount;
        
        List<Element> elements = null;
        try
        {
            elements = EnemyManager.instance.GetElements();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting elements from EnemyManager: {e.Message}");
            yield break;
        }
        
        if (elements == null || elements.Count == 0)
        {
            Debug.LogWarning("No elements found from EnemyManager!");
            yield break;
        }
        
        
        foreach (Player player in players)
        {
            List<BaseItem> itemsToSpawn = new List<BaseItem>();
            for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
            {
                Item itemType = GetWeightedRandomItemType(wave);
                switch (itemType)
                {
                    case Item.Attack:
                    {
                        AttackItemData attackData = wave.GetAttackItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (attackData != null)
                        {
                            itemsToSpawn.Add(attackData.CreateAttackItem());
                        }

                        break;
                    }
                    case Item.Support:
                    {
                        //Chỉ spawn ra heal thôi
                        SupportItemData supportData = wave.supportItemData[1];
                        if (supportData != null)
                        {
                            itemsToSpawn.Add(supportData.CreateSupportItem());
                        }
                        break;
                    }
                    case Item.Staff:
                    {
                        StaffItemData staffItemData = wave.GetStaffItem(elements[UnityEngine.Random.Range(0, elements.Count)]);
                        if (staffItemData != null)
                        {
                            itemsToSpawn.Add(staffItemData.CreateStaffItem());
                        }
                        break;
                    }
                }
            }
            
            playerItems[player] = itemsToSpawn;
        }
        
        
        for (int itemIndex = 0; itemIndex < totalItemsToSpawn; itemIndex++)
        {
            foreach (Player player in players)
            {
                if (playerItems[player].Count > itemIndex)
                {
                    SpawnItemForPlayer(player, playerItems[player][itemIndex], wave.spawnOffset);
                }
            }
            yield return new WaitForSeconds(wave.spawnDelay);
        }
    }

   

    private void SpawnRewardForPlayer(Player player, BaseItem item)
    {
        if (player == null || item == null || draggableItemPrefab == null)
        {
            Debug.LogError("Cannot spawn item: player, item, or prefab or map is null!");
            return;
        }

        Vector3 spawnPosition = player.enemy.transform.position;
        
        int spawnPointIndex = player.playerMap.spawnItemPoints.IndexOf(spawnPosition);
        if (spawnPointIndex < 0) spawnPointIndex = 0;
        
        // Create server-side item instance
        int instanceId = nextInstanceId++;
        ServerItemInstance itemInstance = new ServerItemInstance(
            instanceId,
            item.id,
            player.id,
            item,
            spawnPosition ,
            spawnPointIndex
        );
        
        // Register the instance
        RegisterItemInstance(itemInstance);
        
        // Get player's connection and send TargetRpc to spawn locally
        NetworkConnectionToClient conn = GetPlayerConnection(player);
        if (conn != null)
        {
            // Get additional state data
            int charges = 0;
            Element element = Element.None;
            if (item is StaffItem staffItem)
            {
                charges = staffItem.charges;
                element = staffItem.element;
            }
            else if (item is AttackItem attackItem)
            {
                element = attackItem.element;
            }
            
            int degree = UnityEngine.Random.Range(-60, -120);
            
            Vector2 shootDirection = new Vector2(Mathf.Cos(degree * Mathf.Deg2Rad), Mathf.Sin(degree * Mathf.Deg2Rad));
            
            // Send RPC to client to spawn local visual
            TargetSpawnItemLocal(conn, instanceId, item.id, spawnPosition , spawnPointIndex, shootDirection, charges, element);
        }
        else
        {
            Debug.LogError($"Could not get connection for player {player.id}");
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

        Vector3 spawnPosition = GetSpawnPoint(player.playerMap);
        int spawnPointIndex = player.playerMap.spawnItemPoints.IndexOf(spawnPosition);
        if (spawnPointIndex < 0) spawnPointIndex = 0;
        
        // Create server-side item instance
        int instanceId = nextInstanceId++;
        ServerItemInstance itemInstance = new ServerItemInstance(
            instanceId,
            item.id,
            player.id,
            item,
            spawnPosition + offset,
            spawnPointIndex
        );
        
        // Register the instance
        RegisterItemInstance(itemInstance);
        
        // Get player's connection and send TargetRpc to spawn locally
        NetworkConnectionToClient conn = GetPlayerConnection(player);
        if (conn != null)
        {
            // Get additional state data
            int charges = 0;
            Element element = Element.None;
            if (item is StaffItem staffItem)
            {
                charges = staffItem.charges;
                element = staffItem.element;
            }
            else if (item is AttackItem attackItem)
            {
                element = attackItem.element;
            }
            
            // Send RPC to client to spawn local visual
                TargetSpawnItemLocal(conn, instanceId, item.id, spawnPosition + offset, spawnPointIndex, new Vector2(0,1), charges, element);
        }
        else
        {
            Debug.LogError($"Could not get connection for player {player.id}");
        }
    }
    
    [TargetRpc]
private void TargetSpawnItemLocal(NetworkConnectionToClient conn, int instanceId, int itemId, Vector3 position, int spawnPointIndex, Vector2 shootDirection, int charges, Element element)
{
    // This runs on the client that owns the item
    if (draggableItemPrefab == null)
    {
        Debug.LogError("draggableItemPrefab is null on client!");
        return;
    }
    
    // Get item from local pool (client-side only)
    if (LocalItemPool.singleton == null)
    {
        Debug.LogError("LocalItemPool.singleton is null! Make sure LocalItemPool is in the scene.");
        return;
    }
    
    LocalItemPool.singleton.SetPrefab(draggableItemPrefab);
    GameObject dragItem = LocalItemPool.singleton.Get(position, Quaternion.identity);
    DraggableItem draggableItem = dragItem.GetComponent<DraggableItem>();
    
    if (draggableItem != null)
    {
        // Get item data from ItemManager
        BaseItem itemData = ItemManager.Instance.GetItemById(itemId);
        if (itemData == null)
        {
            Debug.LogError($"Could not find item with id {itemId} in ItemManager!");
            LocalItemPool.singleton.Return(dragItem);
            return;
        }
        
        // Set up the local item
        draggableItem.SetItemLocal(instanceId, itemData, charges, element);
        draggableItem.gameObject.name = $"{itemData.name} - Instance {instanceId}";
        draggableItem.transform.position = position;
        
        // Reset Rigidbody2D state before applying force
        Rigidbody2D rb = dragItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.WakeUp(); // Ensure the rigidbody is awake
        }
        
        // Apply shoot force - use a coroutine to ensure Rigidbody2D is ready
        StartCoroutine(ApplyShootForceDelayed(draggableItem, shootDirection, shootForce));
        
    }
    else
    {
        Debug.LogError("Failed to get DraggableItem component from pool!");
    }
}

private IEnumerator ApplyShootForceDelayed(DraggableItem draggableItem, Vector2 shootDirection, float force)
{
    // Wait one frame to ensure Rigidbody2D is fully initialized
    yield return null;
    
    if (draggableItem != null)
    {
        draggableItem.Shoot(shootDirection, force);
    }
}
    

    private Vector3 GetSpawnPoint(PlayerMap playerMap)
    {
        Random random = new Random();
        int randomIndex = random.Next(playerMap.spawnItemPoints.Count);
        Vector3 spawnPoint = playerMap.spawnItemPoints[randomIndex];
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
