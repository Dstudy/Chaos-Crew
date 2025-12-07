using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Script.Enemy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

public class EnemyUI : MonoBehaviour
{
    private Enemy enemy;

    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;
    
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
    
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator enemyAnim;
    
    private void Awake()
    {
        if(audioSource == null)
            audioSource = gameObject.GetComponent<AudioSource>();
        enemy = gameObject.GetComponent<Enemy>();

        enemy.onHealthChanged += (health, oldHealth) => UpdateHealthBar(health, oldHealth);
        enemy.onShieldChanged += (shield, oldShield) => UpdateShield(shield, oldShield);

        normalSpriteState = normalFace;
        
        healthText.text = $"{enemy.maxHealth}/{enemy.maxHealth}";
        shieldText.text = $"0/{enemy.maxShield}";
    }

    private void OnEnable()
    {
        ObserverManager.Register(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)AttackPlayer);
        ObserverManager.Register(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Register(ENEMY_GET_HIT, (Action<Enemy, BaseItem, bool>)GetHit);
        ObserverManager.Register(ENEMY_GET_STUNNED, (Action)GetStunned);
        ObserverManager.Register(ENEMY_OUT_STUN, (Action)OutStunned);
        ObserverManager.Register(ENEMY_DEFEATED, (Action<Enemy>)EnemyDefeated);
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(ENEMY_CAST_NORMAL_ATTACK, (Action<int, int>)AttackPlayer);
        ObserverManager.Unregister(ENEMY_CAST_SHIELD, (Action<Enemy>)GainShield);
        ObserverManager.Unregister(ENEMY_GET_HIT, (Action<Enemy, BaseItem, bool>)GetHit);
        ObserverManager.Unregister(ENEMY_GET_STUNNED, (Action)GetStunned);
        ObserverManager.Unregister(ENEMY_OUT_STUN, (Action)OutStunned);
        ObserverManager.Unregister(ENEMY_DEFEATED, (Action<Enemy>)EnemyDefeated);
    }

    private void AttackPlayer(int dmg, int pos)
    {
        enemyAnim.SetTrigger("Hit");
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

    private void GetHit(Enemy target, BaseItem item, bool hitShield)
    {
        StartCoroutine(HitAnimation(target, item, hitShield));
    }
    

    IEnumerator HitAnimation(Enemy target, BaseItem item, bool hitShield)
    {
        switch (item.itemType)
        {
            case Item.Attack:
                AudioManager.Instance.PlaySwordSound(audioSource);
                break;
            case Item.Staff:
                AudioManager.Instance.PlayStaffSound(audioSource);
                break;
            case Item.Hammer:
                AudioManager.Instance.PlayHammerSound(audioSource);
                break;
        }
        
        hitAnimation.GetComponent<Slash>().SetColor(target.element, item);
        hitAnimation.SetActive(true);
        hitAnimation.GetComponent<Animator>().SetTrigger("Slash");
        
        yield return new WaitForSeconds(0.2f);
        
        if(target == enemy && enemy.isLocalEnemy)
            enemyEffect.SetColor((new Color(1, 0, 0, 1f)));
        hitParticleEffect.Play();
        if(!hitShield)
            AudioManager.Instance.PlayEnemyHitSound(audioSource);
        else
        {
            AudioManager.Instance.PlayShieldBlockSound(audioSource);
        }
        EnemyFace.sprite = hitFace;
        
        yield return new WaitForSeconds(0.5f);
        
        EnemyFace.sprite = normalSpriteState;
        hitAnimation.SetActive(false);
    }

    IEnumerator ShieldAnimation()
    {
        AudioManager.Instance.PlayShieldUpSound(audioSource);
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

    private void EnemyDefeated(Enemy _)
    {
        StartCoroutine(PayMauEnemy());
    }

    IEnumerator PayMauEnemy()
    {
        EnemyFace.sprite = diedFace;
        enemyEffect.PayMau();
        enemyEffect.PayMauFaceEnemy();
        yield return new WaitForSeconds(3f);
        Instantiate(SpawnSystem.singleton.meow, transform.position, transform.rotation);
        enemy.DisableEnemy();
    }

    private void UpdateHealthBar(int health, int oldHealth)
    {
        healthText.text = $"{health}/{enemy.maxHealth}";
        healthBar.fillAmount = (float)health / enemy.maxHealth;
    }

    private void UpdateShield(int shield, int oldShield)
    {
        shieldText.text = $"{shield}/{enemy.maxShield}";
        shieldBar.fillAmount = (float)shield/enemy.maxShield;
    }
    
}
