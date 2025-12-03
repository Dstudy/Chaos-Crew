using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Script.Enemy;
using UnityEngine;
using static CONST;


public class EnemyManager : NetworkBehaviour
{
    [SerializeField] public Sprite fireEnemySprite;
    [SerializeField] public Sprite waterEnemySprite;
    [SerializeField] public Sprite earthEnemySprite;
    [SerializeField] public Sprite ChaosEnemySprite;
    [SerializeField] public Sprite AirEnemySprite;
    [SerializeField] private GameObject enemyPrefab;
    public List<GameObject> allEnemies;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private int aliveEnemies;
    public Element[] elementList = {Element.Fire, Element.Water, Element.Earth, Element.Air, Element.Chaos};

    private readonly SyncList<Element> elements = new SyncList<Element>();

    public static EnemyManager instance;

    private int enemyCreationCounter;

    private void Awake()
    {
        instance = this;
        // elements.Clear();
    }
    
    
    public Sprite GetSpriteForElement(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                return fireEnemySprite;
            case Element.Water:
                return waterEnemySprite;
            case Element.Earth:
                return earthEnemySprite;
            case Element.Air:
                return AirEnemySprite;
            case Element.Chaos:
                return ChaosEnemySprite;
            default:
                return null;
        }
    }

    public GameObject GetEnemy()
    {
        GameObject enemy = enemyPrefab;
        Element elementForEnemy = elementList[enemyCreationCounter];
        enemy.GetComponent<Enemy>().element = elementForEnemy;
        enemy.GetComponent<EnemyUI>().EnemyHead.sprite = GetSpriteForElement(elementForEnemy);
        enemyCreationCounter++;
        NotifyEnemySpawned();
        return enemy;
    }

    public void AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
        elements.Add(enemy.element);
    }

    public void InitElements()
    {
        elements.Clear();
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
