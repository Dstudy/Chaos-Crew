using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackItem : BaseItem
{
    public int damage;
    public Element element;
        
    public AttackItem(int id, string name, int damage, Element element) : base(id, name, ItemType.Attack)
    {
        this.damage = damage;
        this.element = element;
    }


    public override string GetStats()
    {
        return $"{damage} {element} DMG";
    }

    public override void UseOn(MonoBehaviour target)
    {
        if (target is Enemy enemy)
        {
            if (element != enemy.element)
            {
                Debug.Log("Not right element");
                return;
            }
            enemy.TakeDamage(this.damage, this.element);
            Debug.Log($"Used {name} on {enemy.name}.");
        }
        else if (target is DraggableItem item && item.GetItem() is Augment augment)
        {
            this.Combine(augment);
            Debug.Log($"Combined {name} with {augment.name}.");
        }
        else
        {
            Debug.Log($"Can't use {name} on {target.name}.");
        }
    }

    private void Combine(Augment augment)
    {
        if (augment.augmentType is AugmentType.Add)
        {
            damage += augment.bonusValue;
        }
        else if (augment.augmentType is AugmentType.Multiple)
        {
            damage *= augment.bonusValue;
        }
    }
}
