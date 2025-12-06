using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

public class PlayerUI : MonoBehaviour
{
    private Player player;

    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;
    
    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private ParticleSystem healParticleEffect;
    [SerializeField] private GameObject shield;
    
    [SerializeField] private EntityEffect playerEffect;
    
    [SerializeField] private AudioSource audioSource;
    
    [SerializeField] private Animator enemyHitAnimator;
    private void Awake()
    {
        if(audioSource == null)
            audioSource = GetComponent<AudioSource>();
        player = gameObject.GetComponent<Player>();

        player.onHealthChanged += (health, oldHealth) => UpdateHealthBar(health);
        player.onShieldChanged += (shield, maxShield) => UpdateShield(shield);

        healthText.text = $"{player.maxHealth}/{player.maxHealth}";
        shieldText.text = $"0/{player.maxShield}";
    }

    private void OnEnable()
    {
        ObserverManager.Register(PLAYER_DIED, (Action<Player>)OnPlayerDie);
        ObserverManager.Register(PLAYER_HEAL, (Action)OnPLayerHeal);
        ObserverManager.Register(PLAYER_SHIELD, (Action)OnPlayerShield);
        ObserverManager.Register(PLAYER_GET_HIT, (Action<Player>)OnPlayerGetHit);
        ObserverManager.Register(ENEMY_GET_STUNNED, (Action)StunEnemy);
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(PLAYER_DIED, (Action<Player>)OnPlayerDie);
        ObserverManager.Unregister(PLAYER_HEAL, (Action)OnPLayerHeal);
        ObserverManager.Unregister(PLAYER_SHIELD, (Action)OnPlayerShield);
        ObserverManager.Unregister(PLAYER_GET_HIT, (Action<Player>)OnPlayerGetHit);
        ObserverManager.Unregister(ENEMY_GET_STUNNED, (Action)StunEnemy);
    }

    private void OnPlayerGetHit(Player target)
    {
        Debug.Log("Run anim hit");
        enemyHitAnimator.gameObject.SetActive(true);
        enemyHitAnimator.SetTrigger("Hit");
        AudioManager.Instance.PlayEnemyHitSound(audioSource);
        if(target == player && player.isLocalPlayer)
            playerEffect.SetColor((new Color(1, 0, 0, 1f)));
        StartCoroutine(HitAnimation());
    }

    private void OnPlayerDie(Player _)
    {
        StartCoroutine(DieAnimation());
    }
    
    private void OnPLayerHeal()
    {
        StartCoroutine(HealAnimation());
    }

    private void OnPlayerShield()
    {
        StartCoroutine(ShieldAnimation());
    }

    IEnumerator HitAnimation()
    {
        hitParticleEffect.Play();
        yield return new WaitForSeconds(0.1f);
        enemyHitAnimator.gameObject.SetActive(false);
    }
    
    IEnumerator DieAnimation()
    {
        playerEffect.PayMau();
        yield return new WaitForSeconds(3f);
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);   
        }
    }

    IEnumerator HealAnimation()
    {
        healParticleEffect.Play();
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator ShieldAnimation()
    {
        AudioManager.Instance.PlayShieldUpSound(audioSource);
        shield.SetActive(true);
        shield.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        shield.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        shield.transform.DOScale(4f, 0.5f);
        shield.GetComponent<SpriteRenderer>().DOFade(0.6f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        shield.SetActive(false);
    }

    private void StunEnemy()
    {
        gameObject.GetComponent<PlayerCamera>().ScreenShake();
    }

    private void UpdateHealthBar(int health)
    {
        healthText.text = $"{health}/{player.maxHealth}";
        healthBar.fillAmount = ((float)health / player.maxHealth)/2f;
    }

    private void UpdateShield(int shield)
    {
        shieldText.text = $"{shield}/{player.maxShield}";
        shieldBar.fillAmount = ((float)shield/player.maxShield)/2f;
    }
}
