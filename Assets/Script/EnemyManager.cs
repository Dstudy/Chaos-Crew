using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public List<GameObject> allEnemies;
    [SerializeField]private List<GameObject> TotalEnemies;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private List<Element> elements = new List<Element>();
    
    
    public static EnemyManager instance;
    
    private void Awake()
    {
        Debug.Log("Enemy manager awake");
        instance = this;
        enemies.Clear();
        TotalEnemies = new List<GameObject>();
        TotalEnemies = allEnemies.ToList();  
        Debug.Log(TotalEnemies.Count);
        Debug.Log(enemies.Count);
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

    public List<Element> GetElements()
    {
        elements.Clear();
        foreach (var enemy in enemies)
        {
            elements.Add(enemy.GetComponent<Enemy>().element);
        }    
        return elements;
    }
}