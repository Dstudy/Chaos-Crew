using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class BaseEntity : NetworkBehaviour
{
    [SyncVar]
    public string id;
    [SyncVar]
    public int health;
    public int maxHealth;
    public int shield;
    public int maxShield;
    
    [SyncVar]
    [SerializeField] public int position; 
    
    public event Action<int, int> onHealthChanged;
    public event Action<int, int> OnShieldChanged;

    public virtual int Health
    {
        get{ return health; }
        set
        {
            int newHealth = Mathf.Clamp(value, 0, maxHealth);
            if (health != newHealth)
            {
                health = newHealth;
                onHealthChanged?.Invoke(health, maxHealth);
            }
        }
    }
    
    public virtual int Shield
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
    
    public int Pos
    {
        get => position;
        set
        {
            position = value;
        }
    }

    protected virtual void Start()
    {
        Health = health;
    }
    
}
