using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.Examples;
using Script.Enemy;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DraggableItem : NetworkBehaviour
{
    // --- THIS IS THE CRITICAL CHANGE ---
    // 1. Sync the ID (an int), not the whole BaseItem.
    // 2. The hook is now named 'OnItemIdChanged' to avoid conflicts.
    [SyncVar(hook = nameof(OnItemIdChanged))]
    private int itemID =-1; // -1 means "no item"

    // This is now a LOCAL variable. It is not synced.
    // It's a cache for the item data we look up from the itemID.
    public BaseItem itemData; 

    private SpriteRenderer spriteRenderer;
    private TeleportItem teleportItem;

    private bool isBeingDragged = false;
    private TargetJoint2D joint;
    
    [Header("Authority Debug Info")]
    [SerializeField] private string currentAuthorityOwner = "None";
    [SerializeField] private Player authorityPlayer = null;
    
    
    public string GetPoolID()
    {
        return CONST.DRAGGABLE_ITEM;
    }

    private void Start()
    {
        teleportItem = TeleportItem.Instance;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = false;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.drag = 1.0f;
        rb.angularDrag = 1.0f;
        
        
        // Update visuals on Awake to handle -1 ID
        UpdateVisuals(itemID); 
    }
    
    private void OnItemIdChanged(int oldId, int newId)
    {
        UpdateVisuals(newId);
    }

    // --- SERVER-ONLY FUNCTION ---
    // Your 'SpawnItemForPlayer' calls this on the SERVER.
    public void SetItem(BaseItem item)
    {
        if (item == null)
        {
            this.itemID = -1;
        }
        else
        {
            // This is the only line that matters.
            // By setting the SyncVar, you trigger the hook on all clients.
            this.itemID = item.id; // Assuming BaseItem has an 'id' property
        }
    }
    
    // This is now a local-only helper function
    public BaseItem GetItem() 
    {
        return itemData;
    }
    
    [ClientRpc]
    public void RpcShoot(Vector2 direction, float force)
    {
        // This code will now run on ALL clients + the server
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }
    
    public void Shoot(Vector2 direction, float force)
    {
        // This code will now run on ALL clients + the server
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }
    
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        UpdateAuthorityInfo();
    }
    
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        UpdateAuthorityInfo();
    }
    
    private void UpdateAuthorityInfo()
    {
        NetworkIdentity netIdentity = GetComponent<NetworkIdentity>();
        if (netIdentity == null)
        {
            currentAuthorityOwner = "No NetworkIdentity";
            authorityPlayer = null;
            return;
        }
        
        if (netIdentity.connectionToClient != null)
        {
            // Find the Player component from the connection
            GameObject playerObj = netIdentity.connectionToClient.identity?.gameObject;
            if (playerObj != null)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null)
                {
                    authorityPlayer = player;
                    currentAuthorityOwner = $"Player {player.id} (Connection {netIdentity.connectionToClient.connectionId})";
                }
                else
                {
                    authorityPlayer = null;
                    currentAuthorityOwner = $"Connection {netIdentity.connectionToClient.connectionId} (No Player component)";
                }
            }
            else
            {
                authorityPlayer = null;
                currentAuthorityOwner = $"Connection {netIdentity.connectionToClient.connectionId}";
            }
        }
        else
        {
            currentAuthorityOwner = "No Authority (Server Only)";
            authorityPlayer = null;
        }
    }
    
    // Call this periodically or when you want to refresh the display
    private void Update()
    {
        joint = GetComponent<TargetJoint2D>();
        isBeingDragged = (joint != null);
        
        // Update authority info in editor/play mode for debugging
        #if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UpdateAuthorityInfo();
        }
        #endif
    }

    // --- VISUALS FUNCTION ---
    // This runs on all clients to update the sprite.
    public void UpdateVisuals(int currentId){
    itemData = null; // Clear old data

        if(spriteRenderer == null) return;

        if (currentId != -1)
        {
            itemData = ItemManager.Instance.GetItemById(currentId);
        }

        if (itemData != null)
        {
            spriteRenderer.sprite = itemData.icon;
            spriteRenderer.enabled = true;
            // You should also update the collider here
            // GetComponent<PolygonCollider2D>().points = _localItemData.collider2D.points;
        }
        else
        {
            spriteRenderer.enabled = false;
        }
    }
    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        GameObject other = collision2D.gameObject;
        if (!isBeingDragged) return;

        if (itemData == null) return;

        MonoBehaviour target = other.GetComponent<Player>();
        if (target == null) target = other.transform.parent.GetComponent<Enemy>();
        if (target == null) target = other.GetComponent<DraggableItem>();
        
        if(target == null) return;

        if (itemData is AttackItem attackItem && target is Enemy enemy && attackItem.element == enemy.element)
        {
            itemData.UseOn(target);
            NetworkServer.UnSpawn(gameObject);
            PrefabPool.singleton.Return(gameObject);
        }
        else if (itemData is SupportItem && target is Player)
        {
            itemData.UseOn(target);
            NetworkServer.UnSpawn(gameObject);
            PrefabPool.singleton.Return(gameObject);
        }
        else if((itemData is AttackItem || itemData is SupportItem) && target is DraggableItem item && item.GetItem() is Augment)
        {
            itemData.UseOn(target);
            NetworkServer.UnSpawn(gameObject);
            PrefabPool.singleton.Return(gameObject);
        }
        else
        {
            //Augment and they cannot use on anything just put here :))
            itemData.UseOn(target);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {   
        // Only process on clients (not server-only)
        // if (!isClient) return;
        
        GameObject localPlayerObj = NetworkClient.localPlayer?.gameObject;
        if (localPlayerObj == null) return;
        int playerId = int.Parse(localPlayerObj.GetComponent<Player>().id);
        NetworkGamePlayerLobby player = localPlayerObj.GetComponent<NetworkGamePlayerLobby>();
        if (player == null) return;
        if (player.localPlayer == null) return;
        
        if (collision.tag == "RightCollider")
        {
            player.CmdTeleportItem(this.gameObject, 1, playerId);
            Debug.Log("PLayer id " + playerId);
            Debug.Log("Object fly to the right");
        }
        else if (collision.tag == "LeftCollider")
        {
            player.CmdTeleportItem(this.gameObject, -1, playerId);
            Debug.Log("PLayer id " + playerId);
            Debug.Log("Object fly to the left");
        }
    }
    
}
