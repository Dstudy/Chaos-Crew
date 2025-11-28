using System;
using Mirror;
using Script.Enemy;
using UnityEngine;
using UnityEngine.Serialization;
using static CONST;

    public class Player: BaseEntity
    {
        public PlayerMap playerMap;
        public Enemy enemy;

        private void OnEnable()
        {
            ObserverManager.Register(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)HandleEnemyDamage);
        }

        private void OnDisable()
        {
            ObserverManager.Unregister(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)HandleEnemyDamage);
        }

        private void HandleEnemyDamage(int damage, int pos)
        {
            if(Pos == pos)
            {
                Debug.Log("Get hit " + damage);
                Health -= damage;
            }   
        }
        

        [ClientRpc]
        public void RpcSetMap(GameObject mapGameObject)
        {
            if (!NetworkClient.ready)
            {
                NetworkClient.Ready();
            }
            playerMap = mapGameObject.GetComponent<PlayerMap>();
            Debug.Log("Set map" + playerMap.name);
        }

        [ClientRpc]
        public void RPCSetEnemy(GameObject enemyGameObject)
        {
            enemy = enemyGameObject.GetComponent<Enemy>();
        }

        protected override void Start()
        {
            base.Start();
            Shield = shield;
        }

        public void ApplyEffect(SupportEffect effect, int value)
        {
            switch (effect)
            {
                case SupportEffect.Shield:
                    Shield += value;
                    Debug.Log($"Gained {value} Shield. Current Shield: {Shield}");
                    break;
                case SupportEffect.Heal:
                    Health += value;
                    Debug.Log($"Gained {value} Health. Current Health: {Health}");
                    break;
            }
            
        }
        
    }
