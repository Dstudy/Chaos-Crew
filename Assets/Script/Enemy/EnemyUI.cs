using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Script.Enemy;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

public class EnemyUI : MonoBehaviour
{
    private Enemy enemy;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;

    [SerializeField] public SpriteRenderer EnemyHead;
    [SerializeField] private SpriteRenderer EnemyFace;
    [SerializeField] private Sprite hitFace;
    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite happyFace;
    [SerializeField] private Sprite shieldFace;
    [SerializeField] private Sprite diedFace;
    
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private GameObject shield;
    
    [SerializeField] private EntityEffect enemyEffect;
    
    private void Awake()
    {
        enemy = gameObject.GetComponent<Enemy>();

        enemy.onHealthChanged += (health, oldHealth) => UpdateHealthBar(health, oldHealth);
        enemy.onShieldChanged += (shield, oldShield) => UpdateShield(shield, oldShield);
        
        ObserverManager.Register(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Register(ENEMY_GET_HIT, (Action<Enemy>)GetHit);
    }
    
    

    private void GainShield(Enemy target)
    {
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor(new Color(0,0.2f,0.8f,1f));
        StartCoroutine(ShieldAnimation());
    }

    private void GetHit(Enemy target)
    {
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor((new Color(1, 0, 0, 1f)));
        StartCoroutine(HitAnimation());
    }

    IEnumerator HitAnimation()
    {
        hitParticleEffect.Play();
        EnemyFace.sprite = hitFace;
        yield return new WaitForSeconds(0.1f);
        EnemyFace.sprite = normalFace;
    }

    IEnumerator ShieldAnimation()
    {
        shield.SetActive(true);
        shield.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        shield.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        shield.transform.DOScale(2.8f, 0.5f);
        shield.GetComponent<SpriteRenderer>().DOFade(0.6f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        shield.SetActive(false);
    }

    

    private void UpdateHealthBar(int health, int oldHealth)
    {
        healthBar.fillAmount = (float)health / enemy.maxHealth;
    }

    private void UpdateShield(int shield, int oldShield)
    {
        shieldBar.fillAmount = (float)shield/oldShield;
    }
    
}
