using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public List<PlayerMap> playerMaps = new List<PlayerMap>() ;
    public int spawnDistance = 50;
    
    public static MapManager instance;

    private void Awake()
    {
        instance = this;
    }

    public void AddMap(PlayerMap playerMap)
    {
        playerMaps.Add(playerMap);  
    }
}
