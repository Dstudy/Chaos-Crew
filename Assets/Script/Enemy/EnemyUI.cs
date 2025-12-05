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
    [SerializeField] private Sprite stunFace;
    [SerializeField] private Sprite normalFace;
    [SerializeField] private Sprite happyFace;
    [SerializeField] private Sprite shieldFace;
    [SerializeField] private Sprite diedFace;

    private Sprite normalSpriteState;
    
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject starGroup;
    
    [SerializeField] private EntityEffect enemyEffect;

    [SerializeField] private GameObject hitAnimation;
    
    private void Awake()
    {
        enemy = gameObject.GetComponent<Enemy>();

        enemy.onHealthChanged += (health, oldHealth) => UpdateHealthBar(health, oldHealth);
        enemy.onShieldChanged += (shield, oldShield) => UpdateShield(shield, oldShield);

        normalSpriteState = normalFace;
    }

    private void OnEnable()
    {
        ObserverManager.Register(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Register(ENEMY_GET_HIT, (Action<Enemy, BaseItem>)GetHit);
        ObserverManager.Register(ENEMY_GET_STUNNED, (Action)GetStunned);
        ObserverManager.Register(ENEMY_OUT_STUN, (Action)OutStunned);
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Unregister(ENEMY_GET_HIT, (Action<Enemy, BaseItem>)GetHit);
        ObserverManager.Unregister(ENEMY_GET_STUNNED, (Action)GetStunned);
        ObserverManager.Unregister(ENEMY_OUT_STUN, (Action)OutStunned);
    }

    private void GetStunned()
    {
        starGroup.SetActive(true);
        normalSpriteState = stunFace;
        EnemyFace.sprite = normalSpriteState;
    }

    private void OutStunned()
    {
        starGroup.SetActive(false);
        normalSpriteState = normalFace;
        EnemyFace.sprite = normalSpriteState;
    }

    private void GainShield(Enemy target)
    {
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor(new Color(0,0.2f,0.8f,1f));
        StartCoroutine(ShieldAnimation());
    }

    private void GetHit(Enemy target, BaseItem item)
    {
        StartCoroutine(HitAnimation(target, item));
    }
    

    IEnumerator HitAnimation(Enemy target, BaseItem item)
    {
        hitAnimation.GetComponent<Slash>().SetColor(target.element, item);
        hitAnimation.SetActive(true);
        hitAnimation.GetComponent<Animator>().SetTrigger("Slash");
        yield return new WaitForSeconds(0.2f);
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor((new Color(1, 0, 0, 1f)));
        hitParticleEffect.Play();
        EnemyFace.sprite = hitFace;
        yield return new WaitForSeconds(0.5f);
        EnemyFace.sprite = normalSpriteState;
        hitAnimation.SetActive(false);
    }

    IEnumerator ShieldAnimation()
    {
        shield.SetActive(true);
        shield.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        shield.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        shield.transform.DOScale(5.3f, 0.5f);
        shield.GetComponent<SpriteRenderer>().DOFade(0.6f, 0.5f);

        EnemyFace.sprite = shieldFace;
        
        yield return new WaitForSeconds(0.5f);
        shield.SetActive(false);

        EnemyFace.sprite = normalSpriteState;
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
