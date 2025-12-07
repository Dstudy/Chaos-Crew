using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Script.Enemy;
using Script.UI;
using UnityEngine;
using UnityEngine.Serialization;
using static CONST;

    public class Player: BaseEntity
    {
        public PlayerMap playerMap;
        public Enemy enemy;
        private bool isDead;
        
        // Reset per-round state so players are alive and topped up
        public void ResetForRound(RoundPlayerSettings settings)
        {
            isDead = false;
            maxHealth = settings.maxHealth;
            Health = settings.maxHealth;
            maxShield = settings.maxShield;
            Shield = settings.maxShield;
        }

        private void OnEnable()
        {
            ObserverManager.Register(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)HandleEnemyDamage);
        }

        private void OnDisable()
        {
            ObserverManager.Unregister(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)HandleEnemyDamage);
        }

        private void HandleEnemyDamage(int damage, int pos)
        {
            if (isDead || Health - damage <= 0)
            {
                return;
            }
            
            if(Pos == pos)
            {
                Debug.Log("Get hit " + damage);
                ObserverManager.InvokeEvent(PLAYER_GET_HIT, this);
                HandleEnemyDamageCmd(damage);
                
                if(Health <= 0 && !isDead)
                {
                    isDead = true;
                    Health = 0;
                    Debug.Log("Player dead");
                    ObserverManager.InvokeEvent(PLAYER_DIED, this);
                    ObserverManager.InvokeEvent(GAME_LOST, this);
                    if (isServer && PlayerSpawnSystem.instance != null && PlayerSpawnSystem.instance.isActiveAndEnabled)
                    {
                        PlayerSpawnSystem.instance.ServerBroadcastGameLost(this);
                    }
                }
            }   
        }

        [Command]
        private void HandleEnemyDamageCmd(int damage)
        {
            int newDamage = damage;
            if (Shield > 0)
            {
                newDamage -= Shield;
                Shield -= damage;
            }
            if(newDamage<=0) newDamage = 0;
            this.Health -= newDamage;
            Debug.Log("Take damage: " + damage);
        }

        [TargetRpc]
        private void TargetInvokePlayerHeal(NetworkConnectionToClient conn)
        {
            ObserverManager.InvokeEvent(PLAYER_HEAL);
        }

        [TargetRpc]
        private void TargetInvokePlayerShield(NetworkConnectionToClient conn)
        {
            ObserverManager.InvokeEvent(PLAYER_SHIELD);
        }

        [ClientRpc]
        public void RpcSetMap(GameObject mapGameObject)
        {
            gameObject.name = "Player " + position;
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }
            playerMap = mapGameObject.GetComponent<PlayerMap>();
            Debug.Log("Set map" + playerMap.name);
        }

        [ClientRpc]
        public void RPCSetEnemy(GameObject enemyGameObject)
        {
            enemyGameObject.name = "Enemy " + position;
            enemy = enemyGameObject.GetComponent<Enemy>();
        }

        protected override void Start()
        {
            base.Start();
            Shield = shield;
        }

    public void ApplyEffect(SupportEffect effect, int value)
    {
        switch (effect)
        {
            case SupportEffect.Shield:
                Shield += value;
                TargetInvokePlayerShield(this.connectionToClient);
                Debug.Log($"Gained {value} Shield. Current Shield: {Shield}");
                break;
            case SupportEffect.Heal:
                Health += value;
                TargetInvokePlayerHeal(this.connectionToClient);
                Debug.Log($"Gained {value} Health. Current Health: {Health}");
                break;
        }
        
    }
    
    // Client-side item tracking
    private Dictionary<int, DraggableItem> localItemInstances = new Dictionary<int, DraggableItem>();
    
    public DraggableItem GetLocalItemInstance(int instanceId)
    {
        localItemInstances.TryGetValue(instanceId, out DraggableItem item);
        return item;
    }
    
    public void RegisterLocalItemInstance(int instanceId, DraggableItem item)
    {
        localItemInstances[instanceId] = item;
    }
    
    public void UnregisterLocalItemInstance(int instanceId)
    {
        localItemInstances.Remove(instanceId);
    }
    
    [TargetRpc]
    public void TargetRemoveItemLocal(NetworkConnectionToClient conn, int instanceId)
    {
        // Remove the local visual for this item
        if (localItemInstances.TryGetValue(instanceId, out DraggableItem item))
        {
            if (item != null && item.gameObject != null)
            {
                if (LocalItemPool.singleton != null)
                {
                    LocalItemPool.singleton.Return(item.gameObject);
                }
                else
                {
                    Destroy(item.gameObject);
                }
            }
            localItemInstances.Remove(instanceId);
            Debug.Log($"Client removed local item instance {instanceId}");
        }
    }
    
    [TargetRpc]
public void TargetReceiveTeleportedItem(NetworkConnectionToClient conn, int instanceId, int itemId, Vector3 position, int spawnPointIndex, Vector2 shootDirection, int charges, Element element)
{
    // Client receives a teleported item - spawn it locally
    if (SpawnSystem.singleton == null)
    {
        Debug.LogError("SpawnSystem.singleton is null!");
        return;
    }
    
    GameObject draggableItemPrefab = SpawnSystem.singleton.draggableItemPrefab;
    if (draggableItemPrefab == null)
    {
        Debug.LogError("draggableItemPrefab is null!");
        return;
    }
    
    // Get item from local pool
    if (LocalItemPool.singleton == null)
    {
        Debug.LogError("LocalItemPool.singleton is null! Make sure LocalItemPool is in the scene.");
        return;
    }
    
    // Find a safe position that doesn't overlap with existing items
    Vector3 safePosition = FindSafeSpawnPosition(position, shootDirection);
    
    LocalItemPool.singleton.SetPrefab(draggableItemPrefab);
    GameObject dragItem = LocalItemPool.singleton.Get(safePosition, Quaternion.identity);
    DraggableItem draggableItem = dragItem.GetComponent<DraggableItem>();
    
    if (draggableItem != null)
    {
        BaseItem itemData = ItemManager.Instance.GetItemById(itemId);
        if (itemData == null)
        {
            Debug.LogError($"Could not find item with id {itemId} in ItemManager!");
            LocalItemPool.singleton.Return(dragItem);
            return;
        }
        
        draggableItem.SetItemLocal(instanceId, itemData, charges, element);
        Rigidbody2D rb = dragItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.WakeUp(); // Ensure the rigidbody is awake
        }
        draggableItem.gameObject.name = $"{itemData.name} - Instance {instanceId} (Teleported)";
        draggableItem.transform.position = safePosition;
        StartCoroutine(ApplyShootForceDelayed(draggableItem, shootDirection, 5f));
        
        RegisterLocalItemInstance(instanceId, draggableItem);
        Debug.Log($"Client received teleported item instance {instanceId}");
    }
}

    private Vector3 FindSafeSpawnPosition(Vector3 originalPosition, Vector2 shootDirection)
    {
        // Check if there's an item at the original position
        float checkRadius = 0.5f; // Adjust based on your item size
        Collider2D overlap = Physics2D.OverlapCircle(originalPosition, checkRadius);
        
        // If no overlap, use original position
        if (overlap == null || overlap.GetComponent<DraggableItem>() == null)
        {
            return originalPosition;
        }
        
        // Try offsetting in the shoot direction first
        float offsetDistance = 0.6f; // Distance to offset
        Vector3 offsetPosition = originalPosition + (Vector3)(shootDirection.normalized * offsetDistance);
        
        // Check if offset position is safe
        Collider2D offsetOverlap = Physics2D.OverlapCircle(offsetPosition, checkRadius);
        if (offsetOverlap == null || offsetOverlap.GetComponent<DraggableItem>() == null)
        {
            return offsetPosition;
        }
        
        // If shoot direction offset didn't work, try perpendicular directions
        Vector2 perpendicular1 = new Vector2(-shootDirection.y, shootDirection.x).normalized;
        Vector2 perpendicular2 = new Vector2(shootDirection.y, -shootDirection.x).normalized;
        
        Vector3 perp1Position = originalPosition + (Vector3)(perpendicular1 * offsetDistance);
        Collider2D perp1Overlap = Physics2D.OverlapCircle(perp1Position, checkRadius);
        if (perp1Overlap == null || perp1Overlap.GetComponent<DraggableItem>() == null)
        {
            return perp1Position;
        }
        
        Vector3 perp2Position = originalPosition + (Vector3)(perpendicular2 * offsetDistance);
        Collider2D perp2Overlap = Physics2D.OverlapCircle(perp2Position, checkRadius);
        if (perp2Overlap == null || perp2Overlap.GetComponent<DraggableItem>() == null)
        {
            return perp2Position;
        }
        
        // If all positions are occupied, offset further in shoot direction
        return originalPosition + (Vector3)(shootDirection.normalized * (offsetDistance * 1.5f));
    }
    
    private IEnumerator ApplyShootForceDelayed(DraggableItem draggableItem, Vector2 shootDirection, float force)
    {
        // Wait one frame to ensure Rigidbody2D is fully initialized
        yield return null;
    
        if (draggableItem != null)
        {
            draggableItem.Shoot(shootDirection, force);
        }
    }
    
    [Command]
    public void CmdTeleportItem(int itemInstanceId, int direction)
    {
        // Server validates and processes teleport
        if (SpawnSystem.singleton == null)
        {
            Debug.LogError("SpawnSystem.singleton is null!");
            return;
        }
        
        ServerItemInstance itemInstance = SpawnSystem.singleton.GetItemInstance(itemInstanceId);
        if (itemInstance == null)
        {
            Debug.LogError($"Item instance {itemInstanceId} not found!");
            return;
        }
        
        // Validate ownership
        if (itemInstance.ownerPlayerId != this.id)
        {
            Debug.LogWarning($"Player {this.id} tried to teleport item {itemInstanceId} owned by {itemInstance.ownerPlayerId}");
            return;
        }
        
        // Find target player
        PlayerManager playerManager = PlayerManager.instance;
        if (playerManager == null || playerManager.players == null)
        {
            Debug.LogError("PlayerManager or players list is null!");
            return;
        }
        
        int currentPlayerIndex = -1;
        for (int i = 0; i < playerManager.players.Count; i++)
        {
            Player p = playerManager.players[i].GetComponent<Player>();
            if (p != null && p.id == this.id)
            {
                currentPlayerIndex = i;
                break;
            }
        }
        
        if (currentPlayerIndex < 0)
        {
            Debug.LogError($"Could not find current player index for {this.id}");
            return;
        }
        
        int targetPlayerIndex = (currentPlayerIndex + direction + playerManager.players.Count) % playerManager.players.Count;
        Player targetPlayer = playerManager.players[targetPlayerIndex].GetComponent<Player>();
        
        if (targetPlayer == null)
        {
            Debug.LogError($"Target player at index {targetPlayerIndex} is null!");
            return;
        }
        
        // Update item instance ownership
        string oldOwnerId = itemInstance.ownerPlayerId;
        itemInstance.ownerPlayerId = targetPlayer.id;
        
        // Remove from old owner's list and add to new owner's list
        SpawnSystem.singleton.UnregisterItemInstance(itemInstanceId);
        SpawnSystem.singleton.RegisterItemInstance(itemInstance);
        
        // Get spawn position for target player
        Transform telePosition = direction == -1 ? targetPlayer.playerMap.rightSpawnPoint : targetPlayer.playerMap.leftSpawnPoint;
        itemInstance.spawnPosition = telePosition.position;
        
        // Get state data
        int charges = itemInstance.charges;
        Element element = itemInstance.currentElement;
        
        // Remove from source player's client
        NetworkConnectionToClient sourceConn = connectionToClient;
        if (sourceConn != null)
        {
            TargetRemoveItemLocal(sourceConn, itemInstanceId);
        }
        
        // Spawn on target player's client
        NetworkConnectionToClient targetConn = targetPlayer.connectionToClient;
        if (targetConn != null)
        {
            Vector2 shootDirection = direction == -1 ? Vector2.left : Vector2.right;
            targetPlayer.TargetReceiveTeleportedItem(targetConn, itemInstanceId, itemInstance.itemId, telePosition.position, 0, shootDirection, charges, element);
        }
        
        Debug.Log($"Teleported item {itemInstanceId} from player {oldOwnerId} to player {targetPlayer.id}");
    }
    
        [Command]
        public void CmdUseItem(int itemInstanceId, string targetType, int targetId)
        {
            // Server validates and processes item use
            if (SpawnSystem.singleton == null)
        {
            Debug.LogError("SpawnSystem.singleton is null!");
            return;
        }
        
        ServerItemInstance itemInstance = SpawnSystem.singleton.GetItemInstance(itemInstanceId);
        if (itemInstance == null)
        {
            Debug.LogError($"Item instance {itemInstanceId} not found!");
            return;
        }
        
        // Validate ownership
        if (itemInstance.ownerPlayerId != this.id)
        {
            Debug.LogWarning($"Player {this.id} tried to use item {itemInstanceId} owned by {itemInstance.ownerPlayerId}");
            return;
        }
        
        BaseItem item = itemInstance.itemData;
        if (item == null)
        {
            Debug.LogError($"Item data is null for instance {itemInstanceId}");
            return;
        }
        
        // Find target based on type
        MonoBehaviour target = null;
        
        if (targetType == "Player")
        {
            PlayerManager playerManager = PlayerManager.instance;
            if (playerManager != null && playerManager.players != null)
            {
                // target = playerManager.localPlayer;
                foreach (GameObject playerObj in playerManager.players)
                {
                    Player p = playerObj.GetComponent<Player>();
                    if (p != null && p.id == targetId.ToString())
                    {
                        target = p;
                        break;
                    }
                }
            }
        }
        else if (targetType == "Enemy")
        {
            // Find enemy by position
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Pos == targetId)
                {
                    target = enemy;
                    break;
                }
            }
        }
        else if (targetType == "Item")
        {
            // Find item instance
            ServerItemInstance targetItemInstance = SpawnSystem.singleton.GetItemInstance(targetId);
            if (targetItemInstance != null && targetItemInstance.ownerPlayerId == this.id)
            {
                // Get local item visual from this player
                DraggableItem localItem = GetLocalItemInstance(targetId);
                if (localItem != null)
                {
                    target = localItem;
                }
            }
        }
        
        if (target == null)
        {
            Debug.LogWarning($"Could not find target {targetType} with id {targetId}");
            return;
        }
        
        // Use the item
        bool shouldRemove = false;
        
        if (item is StaffItem staffItem)
        {
            if (target is Enemy enemy && itemInstance.currentElement == enemy.element && itemInstance.charges > 0)
            {
                Debug.Log("Local staff: " + staffItem.element + " server Staff" + itemInstance.currentElement);
                // Update staff item state before use
                staffItem.element = itemInstance.currentElement;
                staffItem.charges = itemInstance.charges;
                
                staffItem.UseOn(target);
                itemInstance.charges--;
                Debug.Log("Before change: " + itemInstance.currentElement + " " + staffItem.element);
                itemInstance.currentElement = staffItem.element; // May change after use
                
                // Update charges on client
                NetworkConnectionToClient conn = connectionToClient;
                if (conn != null)
                {
                    Debug.Log("Update item");
                    TargetUpdateItemState(conn, itemInstanceId, itemInstance.charges, itemInstance.currentElement);
                }
                
                if (itemInstance.charges <= 0)
                {
                    shouldRemove = true;
                }
            }
        }
        else if (item is AttackItem attackItem)
        {
            if (target is Enemy enemy && attackItem.element == enemy.element)
            {
                item.UseOn(target);
                shouldRemove = true;
            }
        }
        else if (item is SupportItem)
        {
            if (target is Player)
            {
                item.UseOn(target);
                Debug.Log(target.name);
                shouldRemove = true;
            }
        }
        else
        {
            // Augment or other item types
            item.UseOn(target);
        }

        // Remove item if needed
        if (shouldRemove)
        {
            SpawnSystem.singleton.UnregisterItemInstance(itemInstanceId);
            NetworkConnectionToClient conn = connectionToClient;
            if (conn != null)
            {
                TargetRemoveItemLocal(conn, itemInstanceId);
            }
        }
    }

    [Command]
    public void CmdRequestNextRound()
    {
        if (RoundManager.instance == null)
        {
            Debug.LogWarning("CmdRequestNextRound: RoundManager instance is null on server.");
            return;
        }

        RoundManager.instance.ServerRequestNextRound();
    }

    [Command]
    public void CmdRequestShutdown()
    {
        var lobby = NetworkManager.singleton as NetworkManagerLobby;
        lobby?.ShutdownAndReturnToMenu();
    }

    [Command]
    public void CmdReturnToMenu()
    {
        var lobby = NetworkManager.singleton as NetworkManagerLobby;
        lobby?.ReturnToMenuForAll();
    }

    [TargetRpc]
    public void TargetUpdateItemState(NetworkConnectionToClient conn, int instanceId, int charges, Element element)
    {
        // Update local item state
        if (localItemInstances.TryGetValue(instanceId, out DraggableItem item))
        {
            if (item != null)
            {
                item.currentCharges = charges;
                item.currentElement = element;
                Debug.Log(item.currentElement);
                // Update visuals if needed (e.g., staff element change)
                BaseItem baseItem = item.GetItem();
                if (baseItem is StaffItem staffItem)
                {
                    staffItem.element = element;
                    Debug.Log(staffItem.element);
                    staffItem.charges = charges;
                    item.UpdateStaffSprite(staffItem);
                }
            }
        }
    }

}
