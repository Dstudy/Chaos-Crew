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
    [SerializeField] private List<BaseItem> items = new List<BaseItem>();
    private Dictionary<int, BaseItem> itemLookup;
    private void Awake()
    {
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
    
}
