using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseEntity : MonoBehaviour
{
    public string id;
    public int health;
    public int maxHealth;
    
    public event Action<int, int> onHealthChanged;

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

    protected virtual void Start()
    {
        Health = health;
    }
    
    
}
