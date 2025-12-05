using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slash : MonoBehaviour
{
    [SerializeField] List<TrailRenderer> trails = new List<TrailRenderer>();
    [SerializeField] private SpriteRenderer sr;

    public void SetColor(Element element, BaseItem item)
    {
        ItemManager itemManager = ItemManager.Instance;
        switch (item.itemType)
        {
            case Item.Attack:
                sr.sprite = itemManager.GetSwordSprite(element);
                break;
            case Item.Hammer:
                sr.sprite = itemManager.GetHammerSprite(element);
                break;
            case Item.Staff:
                sr.sprite = itemManager.GetStaffSprite(element);
                break;
        }
        
        foreach (var trail in trails)
        {
            trail.startColor = itemManager.GetColorForElement(element);
        }
    }
}
