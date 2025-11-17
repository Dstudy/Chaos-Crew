using System;
using Mirror;
using UnityEngine;

    public class Player: BaseEntity
    {
        [SerializeField] private int position; 
        [SerializeField] private int shield;
        public int maxShield;
        public MapManager map;
        
        public event Action<int, int> OnShieldChanged;

        public int Pos
        {
            get => position;
            set
            {
                position = value;
            }
        }

        public int Shield
        {
            get => shield;
            set 
            {
                int newShield = Mathf.Clamp(value, 0, maxShield);
                if (newShield != shield)
                {
                    shield = newShield;
                    OnShieldChanged?.Invoke(shield, maxShield);
                }
            }
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
