using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    private Player player;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    
    private void Awake()
    {
        player = gameObject.GetComponent<Player>();

        player.onHealthChanged += (health, maxHealth) => UpdateHealthBar(health, maxHealth);
        player.OnShieldChanged += (shield, maxShield) => UpdateShield(shield, maxShield);
    }

    private void UpdateHealthBar(int health, int maxHealth)
    {
        healthBar.fillAmount = ((float)health / maxHealth)/2f;
    }

    private void UpdateShield(int shield, int maxShield)
    {
        shieldBar.fillAmount = ((float)shield/maxShield)/2f;
    }
}
