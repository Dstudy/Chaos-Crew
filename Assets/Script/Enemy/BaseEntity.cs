using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using static CONST;
public abstract class BaseEntity : NetworkBehaviour
{
    [SyncVar (hook = nameof(OnEntityCreated))]
    public string id;
    [SyncVar (hook = nameof(OnHealthChanged))]
    public int health;
    public int maxHealth;
    [SyncVar (hook = nameof(OnShieldChanged))]
    public int shield;
    public int maxShield;
    
    [SyncVar]
    [SerializeField] public int position; 
    
    public event Action<int, int> onHealthChanged;
    public event Action<int, int> onShieldChanged;

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"{gameObject.name} is [OnHealthChanged] {(isServer ? "SERVER" : "CLIENT")}  {oldHealth} -> {newHealth}");
        // if(newHealth > oldHealth)
        //     ObserverManager.InvokeEvent(PLAYER_HEAL);
        onHealthChanged?.Invoke(newHealth, oldHealth);
    }

    private void OnShieldChanged(int oldShield, int newShield)
    {
        Debug.Log($"{gameObject.name} is [OnShieldChanged] {(isServer ? "SERVER" : "CLIENT")}  {oldShield} -> {newShield}");
        onShieldChanged?.Invoke(newShield, oldShield);
    }

    public virtual void OnEntityCreated(string _, string id)
    {
    }
    
    public virtual int Health
    {
        get{ return health; }
        set
        {
            int newHealth = Mathf.Clamp(value, 0, maxHealth);
            if (health != newHealth)
            {
                health = newHealth;
                // onHealthChanged?.Invoke(health, maxHealth);
            }
        }
    }
    
    public virtual int Shield
    {
        get => shield;
        set 
        {
            int newShield = Mathf.Clamp(value, 0, maxShield);
            // onShieldChanged?.Invoke(newShield, shield);
            if (newShield != shield)
            {
                shield = newShield;
                // onShieldChanged?.Invoke(shield, maxShield);
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
