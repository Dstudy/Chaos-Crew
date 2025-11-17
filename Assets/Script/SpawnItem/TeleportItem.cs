using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeleportItem : NetworkBehaviour
{
    private Transform telePos;
    private PlayerManager playerManager;
    public static TeleportItem Instance;
    [SerializeField] private float shootPower;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerManager = PlayerManager.instance;
    }
    
    
    public void ServerTeleport(GameObject itemToTeleport, int direction, MapManager map)
    {
        // We are on the server. We don't need the 'Transform' from the client.
        // We get the spawn point ourselves.
        Transform spawnPoint;
        
        if(direction == 1)
            spawnPoint = map.leftSpawnPoint;
        else
        {
            spawnPoint = map.rightSpawnPoint;
        }
        // Now, actually move the item.
        // You must set the position on the server for it to sync.
        // Make sure the item has a NetworkTransform component.
        itemToTeleport.transform.position = spawnPoint.position;

        // If you want to shoot it (like in SpawnSystem), you must call an Rpc
        // from the item itself.
        Rigidbody2D rb = itemToTeleport.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero; // Stop it first
    
        DraggableItem item = itemToTeleport.GetComponent<DraggableItem>();
        if (item != null)
        {
            // Assuming you have an RpcShoot like we built before
            // item.RpcShoot(spawnPoint.up * direction, 10f); // 10f is just an example force
        }
    }
}
