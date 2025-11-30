using Mirror;
using Script.Enemy;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DraggableItem : MonoBehaviour
{
    // Local-only item instance tracking
    public int instanceId = -1; // Server-assigned instance ID
    
    // Local item data (not synced, set by client)
    [SerializeReference] [SerializeField] private BaseItem itemData; 

    private SpriteRenderer spriteRenderer;

    private bool isBeingDragged;
    private TargetJoint2D joint;
    
    [Header("Authority Debug Info")]
    [SerializeField] private string currentAuthorityOwner = "None";
    [SerializeField] private Player authorityPlayer;
    
    
    public string GetPoolID()
    {
        return CONST.DRAGGABLE_ITEM;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = false;
        
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.drag = 1.0f;
        rb.angularDrag = 1.0f;
    }
    
    // Called by client to set up local item
    public void SetItemLocal(int instanceId, BaseItem item, int charges, Element element)
    {
        this.instanceId = instanceId;
        this.itemData = item;
        this.currentCharges = charges;
        this.currentElement = element;

        if (item is StaffItem staffItem)
        {
            itemData.name = staffItem.name;
            staffItem.element = element;
            staffItem.charges = charges;
            itemData.setIcon(staffItem.elementSprites[element]);
        }
        
        // Update visuals
        UpdateVisualsLocal();
        
        // Register with local player if available
        if (NetworkClient.localPlayer != null)
        {
            Player player = NetworkClient.localPlayer.GetComponent<Player>();
            if (player != null)
            {
                player.RegisterLocalItemInstance(instanceId, this);
            }
        }
    }
    
    // Local-only helper function
    public BaseItem GetItem() 
    {
        return itemData;
    }
    
    public void Shoot(Vector2 direction, float force)
    {
        // Local-only shooting
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }
    
    private void Update()
    {
        joint = GetComponent<TargetJoint2D>();
        isBeingDragged = (joint != null);
    }

    // --- VISUALS FUNCTION ---
    // Local-only visual update
    private void UpdateVisualsLocal()
    {
        if(spriteRenderer == null) return;

        if (itemData != null)
        {
            gameObject.name = itemData.name;
            spriteRenderer.sprite = itemData.icon;
            spriteRenderer.enabled = true;
            GetComponent<PolygonCollider2D>().points = itemData.collider2D.points;
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

        if (itemData == null || instanceId < 0) return;

        MonoBehaviour target = other.transform.parent.GetComponent<Player>();
        if (target == null) target = other.transform.parent.GetComponent<Enemy>();
        if (target == null) target = other.GetComponent<DraggableItem>();
        
        if(target == null) return;

        // Send command to server to handle item use
        if (NetworkClient.localPlayer != null)
        {
            Player player = NetworkClient.localPlayer.GetComponent<Player>();
            if (player != null)
            {
                // Determine target type and ID
                int targetId = -1;
                string targetType = "";
                
                if (target is Player targetPlayer)
                {
                    targetId = int.Parse(targetPlayer.id);
                    targetType = "Player";
                }
                else if (target is Enemy targetEnemy)
                {
                    targetId = targetEnemy.Pos;
                    targetType = "Enemy";
                }
                else if (target is DraggableItem targetItem)
                {
                    targetId = targetItem.instanceId;
                    targetType = "Item";
                }
                
                if (targetId >= 0)
                {
                    player.CmdUseItem(instanceId, targetType, targetId);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {   
        // Only process on clients
        if (instanceId < 0) return;
        
        GameObject localPlayerObj = NetworkClient.localPlayer?.gameObject;
        if (localPlayerObj == null) return;
        
        Player player = localPlayerObj.GetComponent<Player>();
        if (player == null) return;
        
        if (collision.CompareTag("RightCollider"))
        {
            player.CmdTeleportItem(instanceId, 1);
            Debug.Log($"Teleporting item {instanceId} to the right");
        }
        else if (collision.CompareTag("LeftCollider"))
        {
            player.CmdTeleportItem(instanceId, -1);
            Debug.Log($"Teleporting item {instanceId} to the left");
        }
    }
    
    public void UpdateStaffSprite(StaffItem staffItem)
    {
        if (staffItem == null || spriteRenderer == null) return;
        
        // Try to get sprite from staff's element sprite mapping
        Sprite newSprite = staffItem.GetSpriteForElement(staffItem.element);
        
        if (newSprite != null)
        {
            staffItem.setIcon(newSprite);
            spriteRenderer.sprite = newSprite;
            Debug.Log($"Updated staff sprite to {staffItem.element} element.");
        }
        else
        {
            // Fallback: Try to get sprite from ItemManager by element
            Debug.Log("No sprite huhu");
            // UpdateSpriteByElement(staffItem.element);
        }
    }
    
    // Public fields for state tracking
    public int currentCharges ;
    public Element currentElement = Element.None;
}
