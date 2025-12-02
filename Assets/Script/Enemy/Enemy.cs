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
            
            Player facingPlayer = GetFacingPlayer();
            if (facingPlayer != null && facingPlayer.connectionToClient != null)
            {
                TargetInvokeEnemyHit(facingPlayer.connectionToClient);
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
        private Player GetFacingPlayer()
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
            Instantiate(SpawnSystem.singleton.meow, transform.position, transform.rotation);
            ObserverManager.InvokeEvent(ENEMY_DEFEATED, this);
            DisableEnemy();
            
        }

        [TargetRpc]
        private void TargetInvokeEnemyHit(NetworkConnectionToClient conn)
        {
            ObserverManager.InvokeEvent(ENEMY_GET_HIT, this);
        }

        private void DisableEnemy()
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);   
            }
        }

        public void DoAttack(int damage, int pos)
        {
            ObserverManager.InvokeEvent(ENEMY_CAST_NORMAL_ATTACK, damage, pos);
        }

        public void DoShield(int shield)
        {
            ObserverManager.InvokeEvent(ENEMY_CAST_SHIELD, this);
            Debug.Log("Shield + " +  shield);
            Shield += shield;
        }
        
    }
}
