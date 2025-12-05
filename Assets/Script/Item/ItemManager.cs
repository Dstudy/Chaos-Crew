using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    public List<AttackItemData> AttackItemDatas;
    public List<AugmentData> AugmentDatas;
    public List<SupportItemData> SupportItemDatas;
    public List<HammerData> HammerDatas;
    public List<StaffItemData> StaffItemDatas;
    [SerializeField] private List<BaseItem> items = new List<BaseItem>();
    private Dictionary<int, BaseItem> itemLookup;
    
    public Dictionary<Element, Sprite> staffElementSprites;
    public Dictionary<Element, Sprite> swordElementSprites;
    public Dictionary<Element, Sprite> hammerElementSprites;
    
    [Header("Staff Element Sprites")]
    [SerializeField] private Sprite staffFireSprite;
    [SerializeField] private Sprite staffWaterSprite;
    [SerializeField] private Sprite staffEarthSprite;
    [SerializeField] private Sprite staffAirSprite;
    [SerializeField] private Sprite staffChaosSprite;

    [Header("Sword Element Sprites")]
    [SerializeField] private Sprite swordFireSprite;
    [SerializeField] private Sprite swordWaterSprite;
    [SerializeField] private Sprite swordEarthSprite;
    [SerializeField] private Sprite swordAirSprite;
    [SerializeField] private Sprite swordChaosSprite;

    [Header("Hammer Element Sprites")]
    [SerializeField] private Sprite hammerFireSprite;
    [SerializeField] private Sprite hammerWaterSprite;
    [SerializeField] private Sprite hammerEarthSprite;
    [SerializeField] private Sprite hammerAirSprite;
    [SerializeField] private Sprite hammerChaosSprite;
    
    private void Awake()
    {
        staffElementSprites = new Dictionary<Element, Sprite>
        {
            { Element.Fire, staffFireSprite },
            { Element.Water, staffWaterSprite },
            { Element.Earth, staffEarthSprite },
            { Element.Air, staffAirSprite },
            { Element.Chaos, staffChaosSprite }
        };

        swordElementSprites = new Dictionary<Element, Sprite>
        {
            { Element.Fire, swordFireSprite },
            { Element.Water, swordWaterSprite },
            { Element.Earth, swordEarthSprite },
            { Element.Air, swordAirSprite },
            { Element.Chaos, swordChaosSprite }
        };

        hammerElementSprites = new Dictionary<Element, Sprite>
        {
            { Element.Fire, hammerFireSprite },
            { Element.Water, hammerWaterSprite },
            { Element.Earth, hammerEarthSprite },
            { Element.Air, hammerAirSprite },
            { Element.Chaos, hammerChaosSprite }
        };
        
        
        Instance = this;
        foreach (AttackItemData attackItemData in AttackItemDatas)
        {
            items.Add(attackItemData.CreateAttackItem());
        }

        foreach (AugmentData augmentData in AugmentDatas)
        {
            items.Add(augmentData.CreateAugment());
        }

        foreach (SupportItemData supportItemData in SupportItemDatas)
        {
            items.Add(supportItemData.CreateSupportItem());
        }

        foreach (HammerData hammerData in HammerDatas)
        {
            items.Add(hammerData.CreateHammerItem());
        }

        foreach (StaffItemData staffItemData in StaffItemDatas)
        {
            items.Add(staffItemData.CreateStaffItem()); 
        }
        
        BuildItemLookup();
    }
    
    public BaseItem GetItemById(int id)
    {
        if(itemLookup == null)
            Debug.Log("Item lookup null roi");
        if (itemLookup != null && itemLookup.TryGetValue(id, out var item))
            return item;
        return null;
    }

    private void BuildItemLookup()
    {
        if (items == null || items.Count == 0)
        {
            itemLookup = new Dictionary<int, BaseItem>();
            return;
        }

        itemLookup = new Dictionary<int, BaseItem>(items.Count);
        foreach (var item in items)
        {
            if (item == null)
                continue;

            if (itemLookup.ContainsKey(item.id))
            {
                Debug.LogWarning($"Duplicate item id {item.id} detected on {item.name}");
                continue;
            }

            itemLookup.Add(item.id, item);
        }
    }
    
    
    public Sprite GetStaffSprite(Element elem)
    {
        if (staffElementSprites != null && staffElementSprites.ContainsKey(elem))
            return staffElementSprites[elem];
        return null;
    }

    public Sprite GetSwordSprite(Element elem)
    {
        if (swordElementSprites != null && swordElementSprites.ContainsKey(elem))
            return swordElementSprites[elem];
        return null;
    }

    public Sprite GetHammerSprite(Element elem)
    {
        if (hammerElementSprites != null && hammerElementSprites.ContainsKey(elem))
            return hammerElementSprites[elem];
        return null;
    }

    public Color GetColorForElement(Element elem)
    {
        switch (elem)
        {
            case Element.Fire:
                return new Color(1f, 0.3f, 0.1f);        // orange-red
            case Element.Water:
                return new Color(0.2f, 0.5f, 1f);        // blue
            case Element.Earth:
                return new Color(0.4f, 0.25f, 0.1f);     // brown
            case Element.Air:
                return new Color(0.8f, 0.9f, 1f);        // light sky
            case Element.Chaos:
                return new Color(0.7f, 0f, 1f);          // purple
            default:
                return Color.white;
        }
    }

}
