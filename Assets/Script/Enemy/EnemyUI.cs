using System;
using System.Collections;
using System.Collections.Generic;
using Script.Enemy;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

public class EnemyUI : MonoBehaviour
{
    private Enemy enemy;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;

    [SerializeField] private GameObject hitFace;
    [SerializeField] private GameObject normalFace;
    
    [SerializeField] private EntityEffect enemyEffect;
    
    private void Awake()
    {
        enemy = gameObject.GetComponent<Enemy>();

        enemy.onHealthChanged += (health, maxHealth) => UpdateHealthBar(health, maxHealth);
        enemy.onShieldChanged += (shield, maxShield) => UpdateShield(shield, maxShield);
        
        ObserverManager.Register(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Register(ENEMY_GET_HIT, (Action<Enemy>)GetHit);
    }
    
    

    private void GainShield(Enemy target)
    {
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor(new Color(0,0.2f,0.8f,1f));
    }

    private void GetHit(Enemy target)
    {
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor((new Color(1, 0, 0, 1f)));
        hitFace.SetActive(true);
        normalFace.SetActive(false);
    }

    IEnumerator HitAnimation()
    {
        yield return new WaitForSeconds(0.5f);
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
