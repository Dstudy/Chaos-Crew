using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ServerGameManager : NetworkBehaviour
{
    public static ServerGameManager Instance { get; private set; }

    // Track all game entities
    [SyncVar] private int totalPlayers = 0;
    [SyncVar] private int totalEnemies = 0;
    [SyncVar] private int totalMaps = 0;
    [SyncVar] private int totalItems = 0;

    // Server-only lists to track all entities
    private readonly List<GameObject> allMaps = new List<GameObject>();
    private readonly List<GameObject> allPlayers = new List<GameObject>();
    private readonly List<GameObject> allEnemies = new List<GameObject>();
    private readonly List<GameObject> allItems = new List<GameObject>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        Debug.Log("ServerGameManager: Server started, ready to manage all game entities");
    }

    // Map Management
    [Server]
    public void RegisterMap(GameObject map)
    {
        if (map != null && !allMaps.Contains(map))
        {
            allMaps.Add(map);
            totalMaps = allMaps.Count;
            Debug.Log($"ServerGameManager: Map registered. Total maps: {totalMaps}");
        }
    }

    [Server]
    public void UnregisterMap(GameObject map)
    {
        if (allMaps.Remove(map))
        {
            totalMaps = allMaps.Count;
            Debug.Log($"ServerGameManager: Map unregistered. Total maps: {totalMaps}");
        }
    }

    [Server]
    public List<GameObject> GetAllMaps()
    {
        return new List<GameObject>(allMaps);
    }

    // Player Management (delegates to PlayerManager but tracks here too)
    [Server]
    public void RegisterPlayer(GameObject player)
    {
        if (player != null && !allPlayers.Contains(player))
        {
            allPlayers.Add(player);
            totalPlayers = allPlayers.Count;
            Debug.Log($"ServerGameManager: Player registered. Total players: {totalPlayers}");
        }
    }

    [Server]
    public void UnregisterPlayer(GameObject player)
    {
        if (allPlayers.Remove(player))
        {
            totalPlayers = allPlayers.Count;
            Debug.Log($"ServerGameManager: Player unregistered. Total players: {totalPlayers}");
        }
    }

    [Server]
    public List<GameObject> GetAllPlayers()
    {
        return new List<GameObject>(allPlayers);
    }

    // Enemy Management
    [Server]
    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null && !allEnemies.Contains(enemy))
        {
            allEnemies.Add(enemy);
            totalEnemies = allEnemies.Count;
            Debug.Log($"ServerGameManager: Enemy registered. Total enemies: {totalEnemies}");
        }
    }

    [Server]
    public void UnregisterEnemy(GameObject enemy)
    {
        if (allEnemies.Remove(enemy))
        {
            totalEnemies = allEnemies.Count;
            Debug.Log($"ServerGameManager: Enemy unregistered. Total enemies: {totalEnemies}");
        }
    }

    [Server]
    public List<GameObject> GetAllEnemies()
    {
        return new List<GameObject>(allEnemies);
    }

    // Item Management
    [Server]
    public void RegisterItem(GameObject item)
    {
        if (item != null && !allItems.Contains(item))
        {
            allItems.Add(item);
            totalItems = allItems.Count;
            Debug.Log($"ServerGameManager: Item registered. Total items: {totalItems}");
        }
    }

    [Server]
    public void UnregisterItem(GameObject item)
    {
        if (allItems.Remove(item))
        {
            totalItems = allItems.Count;
            Debug.Log($"ServerGameManager: Item unregistered. Total items: {totalItems}");
        }
    }

    [Server]
    public List<GameObject> GetAllItems()
    {
        return new List<GameObject>(allItems);
    }

    // Cleanup when objects are destroyed
    [Server]
    public void OnEntityDestroyed(GameObject entity)
    {
        if (entity == null) return;

        if (entity.GetComponent<MapManager>() != null)
            UnregisterMap(entity);
        else if (entity.GetComponent<Player>() != null)
            UnregisterPlayer(entity);
        else if (entity.GetComponent<Enemy>() != null)
            UnregisterEnemy(entity);
        else if (entity.GetComponent<DraggableItem>() != null)
            UnregisterItem(entity);
    }

    // Get statistics (can be called from clients via RPC if needed)
    [Server]
    public void GetGameStatistics(out int players, out int enemies, out int maps, out int items)
    {
        players = totalPlayers;
        enemies = totalEnemies;
        maps = totalMaps;
        items = totalItems;
    }
}
