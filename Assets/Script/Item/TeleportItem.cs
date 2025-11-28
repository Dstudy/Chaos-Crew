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
    
    [ClientRpc]
    public void Teleport(GameObject itemToTeleport, int direction, Vector2 spawnPoint)
    {
        // Now, actually move the item.
        // You must set the position on the server for it to sync.
        // Make sure the item has a NetworkTransform component.
        itemToTeleport.transform.position = spawnPoint;

        Vector2Int vector = new Vector2Int(1, 1);

        // If you want to shoot it (like in SpawnSystem), you must call an Rpc
        // from the item itself.
        Rigidbody2D rb = itemToTeleport.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero; // Stop it first
    
        DraggableItem item = itemToTeleport.GetComponent<DraggableItem>();
        if (item != null)
        {
            // Assuming you have an RpcShoot like we built before
            item.Shoot(vector * direction, 10f);
        }
    }
}
