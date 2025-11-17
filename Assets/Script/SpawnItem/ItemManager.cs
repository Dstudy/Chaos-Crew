using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;
    [SerializeField] private List<AttackItemData> attackItemList;
    
    private void Awake()
    {
        Instance = this;
    }

    public AttackItemData getAttackItem(Element element)
    {
        foreach (var attackItem in attackItemList)
        {
            if (attackItem.element == element)
                return attackItem;
        }
        return null;
    }

    public BaseItem GetItemById(int id)
    {
        for (int i = 0; i < attackItemList.Count; i++)
        {
            if(attackItemList[i].id == id)
                return attackItemList[i].CreateAttackItem();
        }
        return null;
    }
    
}
