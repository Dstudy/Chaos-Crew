using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Staff Item", menuName = "Items/Staff Item Data")]
public class StaffItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public int damage;
    public Element initialElement;
    public int maxCharges;
    public GameObject prefab;
    
    [Header("Element Sprites")]
    [SerializeField] private Sprite fireSprite;
    [SerializeField] private Sprite waterSprite;
    [SerializeField] private Sprite earthSprite;
    [SerializeField] private Sprite airSprite;
    [SerializeField] private Sprite chaosSprite;
    
    public StaffItem CreateStaffItem()
    {
        var item = new StaffItem(id, itemName, damage, initialElement, maxCharges);
        item.prefab = prefab;
        
        // Map sprites to elements
        item.elementSprites[Element.Fire] = fireSprite;
        item.elementSprites[Element.Water] = waterSprite;
        item.elementSprites[Element.Earth] = earthSprite;
        item.elementSprites[Element.Air] = airSprite;
        item.elementSprites[Element.Chaos] = chaosSprite;
        
        return item;
    }
}