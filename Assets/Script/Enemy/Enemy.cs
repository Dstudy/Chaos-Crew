using System;
using Mirror;
using UnityEngine;
using static CONST;
namespace Script.Enemy
{
    public class Enemy : BaseEntity
    {
        public Element element;
        
        public bool isLocalEnemy = false;
        private bool isDead;

    public void TakeDamage(int damage, Element element, BaseItem item)
        {
            if (this.element != element)
            {
                return;
            }
            
            int newDamage = damage;
            if (Shield > 0)
            {
                newDamage -= Shield;
                Shield -= damage;
            }
            if(newDamage<=0) newDamage = 0;
            this.Health -= newDamage;
            Debug.Log("Take damage: " + damage);
            
            Player facingPlayer = GetFacingPlayer();
            if (facingPlayer != null && facingPlayer.connectionToClient != null)
            {
                bool hitShield = false;
                if(newDamage == 0)
                    hitShield = true;
                int itemID = item.id;
                TargetInvokeEnemyHit(facingPlayer.connectionToClient, itemID, hitShield);
            }

            if (Health <= 0 && !isDead)
            {
                isDead = true;
                Debug.Log($"{name} has been defeated!");
                
                if (isServer)
                {
                    // Call server-side spawn logic directly
                    if (SpawnSystem.singleton != null)
                    {
                        SpawnSystem.singleton.OnEnemyDefeatedServer(this);
                    }
        
                    // Invoke event on client that faces this enemy (for client-side observers)
                   
                    if (facingPlayer != null && facingPlayer.connectionToClient != null)
                    {
                        TargetInvokeEnemyDefeated(facingPlayer.connectionToClient);
                    }
        
                    // DisableEnemy();
                }
            }
        }
    
        [Server]
        public Player GetFacingPlayer()
        {
            if (PlayerManager.instance == null || PlayerManager.instance.players == null)
                return null;
    
            foreach (GameObject playerObj in PlayerManager.instance.players)
            {
                Player player = playerObj.GetComponent<Player>();
                if (player != null && player.enemy == this)
                {
                    return player;
                }
            }
            return null;
        }

        [TargetRpc]
        private void TargetInvokeEnemyDefeated(NetworkConnectionToClient conn)
        {
            // This runs only on the client that faces this enemy
            // Instantiate(SpawnSystem.singleton.meow, transform.position, transform.rotation);
            ObserverManager.InvokeEvent(ENEMY_DEFEATED, this);
            // DisableEnemy();
            
        }

        [TargetRpc]
        private void TargetInvokeEnemyHit(NetworkConnectionToClient conn, int itemID, bool hitShield)
        {
            BaseItem item = ItemManager.Instance.GetItemById(itemID);
            ObserverManager.InvokeEvent(ENEMY_GET_HIT, this, item, hitShield);
        }
        
        [TargetRpc]
        public void TargetStunEnemy(NetworkConnectionToClient conn)
        {
            EnemyPattern enemyPattern = GetComponent<EnemyPattern>();
            if (enemyPattern != null)
            {
                enemyPattern.ApplyStun();              // play stun animation / state
            }

            ObserverManager.InvokeEvent(ENEMY_GET_STUNNED);
        }

        public void DisableEnemy()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);   
            }
        }

        public void DoAttack(int damage, int pos)
        {
            if(!isLocalEnemy) return;
            ObserverManager.InvokeEvent(ENEMY_CAST_NORMAL_ATTACK, damage, pos);
        }

        public void DoShield(int shield)
        {
            if(!isLocalEnemy) return;
            ObserverManager.InvokeEvent(ENEMY_CAST_SHIELD, this);
            Debug.Log("Shield + " +  shield);
            ShieldUp(shield);
        }

        [Command]
        private void ShieldUp(int shield)
        {
            Shield += shield;
        }

        public override void OnEntityCreated(string _, string id)
        {
            int _id = Int32.Parse(id);
            Element assignedElement = EnemyManager.instance.elementList[_id];
            element = assignedElement;
            gameObject.GetComponent<EnemyUI>().EnemyHead.sprite = EnemyManager.instance.GetSpriteForElement(assignedElement);
        }
        
        
    }
}
