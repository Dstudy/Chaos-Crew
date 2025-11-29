using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Enemy;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    public List<GameObject> allEnemies;
    [SerializeField]private List<GameObject> TotalEnemies;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();

    readonly SyncList<Element> elements = new SyncList<Element>();
    
    
    public static EnemyManager instance;
    
    private void Awake()
    {
        instance = this;
        // enemies.Clear();
        TotalEnemies = new List<GameObject>();
        TotalEnemies = allEnemies.ToList();  
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
        Debug.Log(enemy);
        TotalEnemies.RemoveAt(TotalEnemies.Count - 1);
        enemies.Add(enemy.GetComponent<Enemy>());
        return enemy;
    }

    public void InitElements()
    {
        elements.Clear();
        foreach (var enemy in enemies)
        {
            elements.Add(enemy.GetComponent<Enemy>().element);
        }   
    }

    public List<Element> GetElements()
    {
        return elements.ToList();
    }
}