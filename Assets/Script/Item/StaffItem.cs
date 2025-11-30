using System.Collections;
using System.Collections.Generic;
using Script.Enemy;
using UnityEngine;
using static CONST;

public class StaffItem : AttackItem
{
    public int charges;
    public int maxCharges;
    
    public Dictionary<Element, Sprite> elementSprites;
    
    public StaffItem(int id, string name, int damage, Element element, int maxCharges) : base(id, name, damage, element)
    {
        this.maxCharges = maxCharges;
        this.charges = maxCharges;
        this.elementSprites = new Dictionary<Element, Sprite>();
    }

    public override void UseOn(MonoBehaviour target)
    {
        if (target is Enemy enemy)
        {
            Debug.Log(element + "  " + enemy.element);
            if (element == enemy.element)
            {
                enemy.TakeDamage(damage, this.element);
                
                Debug.Log($"Used {name} on {enemy.name} - Dealt {damage} damage.");
                
                ChangeToRandomElement(enemy.element);

                charges--;
                
                if (charges <= 0)
                {
                    Debug.Log($"{name} has run out of charges!");
                }
            }
        }
        else
        {
            Debug.Log($"Can't use {name} on {target.name}.");
        }
    }

    public bool HasCharges()
    {
        return charges > 0;
    }

    private void ChangeToRandomElement(Element element)
    {
        List<Element> availableElements = new List<Element>();
        availableElements = EnemyManager.instance.GetElements();
        availableElements.Remove(element);
        if (availableElements.Count > 0)
        {
            Element oldElement = this.element;
            this.element = availableElements[Random.Range(0, availableElements.Count)];
            Debug.Log($"Staff element changed from {oldElement} to {this.element}.");
            
            // Notify that element changed (so sprite can be updated)
            ObserverManager.InvokeEvent(STAFF_CHANGE_ELEMENT, this, this.element);
        }
        else
        {
            // Fallback: if no other elements available, keep current element
            Debug.LogWarning("No available elements to change to!");
        }
    }
    
    public Sprite GetSpriteForElement(Element elem)
    {
        if (elementSprites != null && elementSprites.ContainsKey(elem))
        {
            return elementSprites[elem];
        }
        // Fallback to default prefab sprite
        return icon;
    }

}
