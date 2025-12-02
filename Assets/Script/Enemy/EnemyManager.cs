using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Enemy;
using UnityEngine;
using static CONST;

public class EnemyManager : NetworkBehaviour
{
    public List<GameObject> allEnemies;
    [SerializeField] private List<GameObject> TotalEnemies;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private int aliveEnemies;

    private readonly SyncList<Element> elements = new SyncList<Element>();

    public static EnemyManager instance;

    private void Awake()
    {
        instance = this;
        TotalEnemies = new List<GameObject>(allEnemies);
    }

    public GameObject GetEnemy()
    {
        Debug.Log("TotalEnemies: " + TotalEnemies.Count);
        if (TotalEnemies.Count == 0)
        {
            Debug.LogError("Tried to get an enemy, but the TotalEnemies list is empty!");
            return null;
        }

        GameObject enemy = TotalEnemies[TotalEnemies.Count - 1];
        TotalEnemies.RemoveAt(TotalEnemies.Count - 1);
        enemies.Add(enemy.GetComponent<Enemy>());
        NotifyEnemySpawned();
        return enemy;
    }

    public void InitElements()
    {
        elements.Clear();
        foreach (var enemy in enemies)
        {
            elements.Add(enemy.element);
        }
    }

    public List<Element> GetElements()
    {
        return elements.ToList();
    }

    public void NotifyEnemySpawned()
    {
        aliveEnemies++;
        Debug.Log($"EnemyManager: Enemy spawned. Alive enemies: {aliveEnemies}");
    }

    [Server]
    public void NotifyEnemyDefeated()
    {
        if (aliveEnemies <= 0) return;

        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        Debug.Log($"EnemyManager: Enemy defeated. Remaining enemies: {aliveEnemies}");

        if (aliveEnemies == 0)
        {
            ObserverManager.InvokeEvent(ALL_ENEMIES_DEFEATED);
            ObserverManager.InvokeEvent(GAME_WON);
            if (PlayerSpawnSystem.instance != null && PlayerSpawnSystem.instance.isActiveAndEnabled)
            {
                PlayerSpawnSystem.instance.ServerBroadcastGameWon();
            }
        }
    }
}
