using System;
using UnityEngine;
using static CONST;
namespace Script.Enemy
{
    public class Enemy : BaseEntity
    {
        public Element element;
        
        public bool isLocalEnemy = false;
        // private void Awake()
        // {
        //     ObserverManager.Register(SPAWN_PLAYER, (Action)HandlePlayerSpawn);
        // }
        //
        // private void HandlePlayerSpawn()
        // {
        //     if()
        // }

    public void TakeDamage(int damage, Element element)
        {
            if (this.element != element)
            {
                return;
            }
            
            this.Health -= damage;
            Debug.Log("Take damage: " + damage);

            if (Health <= 0)
            {
                Debug.Log($"{name} has been defeated!");
                Destroy(this.gameObject, 1f);
            }
        }

        public void DoAttack(int damage, int pos)
        {
            ObserverManager.InvokeEvent(ENEMY_CAST_NORMAL_ATTACK, damage, pos);
        }

        public void DoShield(int shield)
        {
            Debug.Log("Shield + " +  shield);
            Shield += shield;
        }
        
    }
}
