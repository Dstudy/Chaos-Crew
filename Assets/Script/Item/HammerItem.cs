using System.Collections;
using System.Collections.Generic;
using Mirror;
using Script.Enemy;
using UnityEngine;
using static CONST;

public class HammerItem : AttackItem
{
    public HammerItem(int id, string name, int damage, Element element) : base(id, name, damage, element)
    {
        
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

            EnemyPattern enemyPattern = enemy.GetComponent<EnemyPattern>();
            if (enemyPattern != null)
            {
                // enemyPattern.ApplyStun();
                Debug.Log(enemy.name);
                Player facingPlayer = enemy.GetFacingPlayer();
                if (facingPlayer != null && facingPlayer.connectionToClient != null)
                {
                    enemy.TargetStunEnemy(facingPlayer.connectionToClient);
                }
            }
            else
            {
                Debug.LogWarning($"Could not find EnemyPattern component on {enemy.name}");
            }

            Debug.Log($"Used {name} on {enemy.name} - Dealt {damage} damage and Stunned");
        }
        else if (target is DraggableItem item && item.GetItem() is Augment augment)
        {
            Combine(augment);
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
