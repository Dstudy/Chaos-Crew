using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Global UI button effects (hover/click) applied across all scenes.
/// Configurable via Inspector; uses DOTween for animations.
/// </summary>
public class ButtonEffectController : MonoBehaviour
{
    public static ButtonEffectController Instance { get; private set; }

    [Header("Hover Effect")]
    [SerializeField] private ButtonEffectSettings hoverEffect = new ButtonEffectSettings
    {
        type = ButtonEffectType.Scale,
        duration = 0.12f,
        scaleMultiplier = 1.05f,
        ease = Ease.OutQuad
    };

    [Header("Click Effect")]
    [SerializeField] private ButtonEffectSettings clickEffect = new ButtonEffectSettings
    {
        type = ButtonEffectType.Punch,
        duration = 0.2f,
        punchStrength = new Vector3(0.12f, 0.12f, 0f),
        vibrato = 12,
        elasticity = 0.9f
    };

    [Header("Misc")]
    [SerializeField] private bool autoRegisterOnSceneLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (autoRegisterOnSceneLoad)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        RegisterAllButtons();
    }

    private void OnDisable()
    {
        if (autoRegisterOnSceneLoad)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterAllButtons();
    }

    /// <summary>
    /// Finds all buttons in the active scene (including inactive) and attaches hover/click effects.
    /// </summary>
    public void RegisterAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>(true);
        foreach (var button in allButtons)
        {
            if (button == null) continue;

            var hook = button.GetComponent<ButtonEffectHook>();
            if (hook == null)
            {
                hook = button.gameObject.AddComponent<ButtonEffectHook>();
            }

            hook.Configure(this, hoverEffect, clickEffect);
        }
    }

    internal void PlayHoverEffect(Transform target, Vector3 baseScale)
    {
        PlayEffect(target, baseScale, hoverEffect, isHover: true);
    }

    internal void PlayClickEffect(Transform target, Vector3 baseScale)
    {
        PlayEffect(target, baseScale, clickEffect, isHover: false);
    }

    private void PlayEffect(Transform target, Vector3 baseScale, ButtonEffectSettings settings, bool isHover)
    {
        if (target == null || settings == null || settings.type == ButtonEffectType.None) return;

        target.DOKill(true);

        switch (settings.type)
        {
            case ButtonEffectType.Scale:
                target.DOScale(baseScale * settings.scaleMultiplier, settings.duration)
                    .SetEase(settings.ease);
                break;
            case ButtonEffectType.Punch:
                target.localScale = baseScale;
                target.DOPunchScale(settings.punchStrength, settings.duration, settings.vibrato, settings.elasticity)
                    .SetEase(settings.ease);
                break;
            case ButtonEffectType.Wiggle:
                target.localScale = baseScale;
                target.DOShakeScale(settings.duration, settings.shakeStrength, settings.vibrato, settings.randomness)
                    .SetEase(settings.ease);
                break;
            default:
                if (isHover)
                {
                    target.DOScale(baseScale, settings.duration).SetEase(settings.ease);
                }
                break;
        }
    }

    [Serializable]
    private class ButtonEffectSettings
    {
        public ButtonEffectType type = ButtonEffectType.None;
        public float duration = 0.15f;
        public float scaleMultiplier = 1.05f;
        public Vector3 punchStrength = new Vector3(0.1f, 0.1f, 0f);
        public int vibrato = 10;
        public float elasticity = 1f;
        public Vector3 shakeStrength = new Vector3(0.1f, 0.1f, 0f);
        public float randomness = 90f;
        public Ease ease = Ease.OutQuad;
    }

    private enum ButtonEffectType
    {
        None,
        Scale,
        Punch,
        Wiggle
    }

    /// <summary>
    /// Per-button hook handling pointer events and invoking the controller effects.
    /// </summary>
    private class ButtonEffectHook : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private ButtonEffectController controller;
        private ButtonEffectSettings hoverSettings;
        private ButtonEffectSettings clickSettings;
        private Vector3 baseScale;

        public void Configure(ButtonEffectController source, ButtonEffectSettings hover, ButtonEffectSettings click)
        {
            controller = source;
            hoverSettings = hover;
            clickSettings = click;
            baseScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            controller?.PlayHoverEffect(transform, baseScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Smoothly return to base scale after hover leaves if using scale-type effect.
            if (controller == null || hoverSettings == null) return;
            if (hoverSettings.type == ButtonEffectType.Scale)
            {
                transform.DOKill(true);
                transform.DOScale(baseScale, hoverSettings.duration).SetEase(hoverSettings.ease);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            controller?.PlayClickEffect(transform, baseScale);
        }
    }
}
