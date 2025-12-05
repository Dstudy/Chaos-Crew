using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

public class PlayerUI : MonoBehaviour
{
    private Player player;

    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    
    [SerializeField] private ParticleSystem hitParticleEffect;
    [SerializeField] private ParticleSystem healParticleEffect;
    [SerializeField] private GameObject shield;
    
    [SerializeField] private EntityEffect playerEffect;
    
    private void Awake()
    {
        player = gameObject.GetComponent<Player>();

        player.onHealthChanged += (health, oldHealth) => UpdateHealthBar(health);
        player.onShieldChanged += (shield, maxShield) => UpdateShield(shield);
    }

    private void OnEnable()
    {
        ObserverManager.Register(PLAYER_HEAL, (Action)OnPLayerHeal);
        ObserverManager.Register(PLAYER_SHIELD, (Action)OnPlayerShield);
        ObserverManager.Register(PLAYER_GET_HIT, (Action<Player>)OnPlayerGetHit);
    }

    private void OnDisable()
    {
        ObserverManager.Unregister(PLAYER_HEAL, (Action)OnPLayerHeal);
        ObserverManager.Unregister(PLAYER_SHIELD, (Action)OnPlayerShield);
        ObserverManager.Unregister(PLAYER_GET_HIT, (Action<Player>)OnPlayerGetHit);
    }

    private void OnPlayerGetHit(Player target)
    {
        if(target == player && player.isLocalPlayer)
            playerEffect.SetColor((new Color(1, 0, 0, 1f)));
        StartCoroutine(HitAnimation());
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
    }

    IEnumerator HealAnimation()
    {
        healParticleEffect.Play();
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator ShieldAnimation()
    {
        shield.SetActive(true);
        shield.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        shield.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        shield.transform.DOScale(4f, 0.5f);
        shield.GetComponent<SpriteRenderer>().DOFade(0.6f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        shield.SetActive(false);
    }

    private void UpdateHealthBar(int health)
    {
        healthBar.fillAmount = ((float)health / player.maxHealth)/2f;
    }

    private void UpdateShield(int shield)
    {
        shieldBar.fillAmount = ((float)shield/player.maxShield)/2f;
    }
}
