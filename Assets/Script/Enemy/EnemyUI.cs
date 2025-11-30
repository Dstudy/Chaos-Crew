using System;
using System.Collections;
using System.Collections.Generic;
using Script.Enemy;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    private Enemy enemy;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    
    private void Awake()
    {
        enemy = gameObject.GetComponent<Enemy>();

        enemy.onHealthChanged += (health, maxHealth) => UpdateHealthBar(health, maxHealth);
        enemy.onShieldChanged += (shield, maxShield) => UpdateShield(shield, maxShield);
    }

    private void UpdateHealthBar(int health, int maxHealth)
    {
        healthBar.fillAmount = (float)health / maxHealth;
    }

    private void UpdateShield(int shield, int maxShield)
    {
        shieldBar.fillAmount = (float)shield/maxShield;
    }
    
}
