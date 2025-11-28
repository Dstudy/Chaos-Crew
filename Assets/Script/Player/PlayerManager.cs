using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror; 

public class PlayerManager : NetworkBehaviour 
{
    //    'readonly' is important. Always initialize it.
    public readonly SyncList<GameObject> players = new SyncList<GameObject>();

    public static PlayerManager instance;

    
    private void Awake()
    {
        instance = this;
    }

    // 4. This function can ONLY be run by the server
    public void AddPlayer(Player player)
    {
        players.Add(player.gameObject);
    }

    // 5. You MUST also have a way to remove players
    [Server]
    public void RemovePlayer(Player player)
    {
        if (player != null)
        {
            players.Remove(player.gameObject);
        }
    }
}