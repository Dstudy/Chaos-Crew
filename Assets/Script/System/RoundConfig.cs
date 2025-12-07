using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[CreateAssetMenu(fileName = "New Round Config", menuName = "Rounds/Round Config")]
public class RoundConfig : ScriptableObject
{
    [SerializeField] private List<RoundDefinition> rounds = new List<RoundDefinition>();

    public int RoundCount => rounds?.Count ?? 0;

    public RoundDefinition GetRound(int index)
    {
        if (index < 0 || index >= RoundCount) return null;
        return rounds[index];
    }

    public IReadOnlyList<RoundDefinition> Rounds => rounds;
}

[Serializable]
public class RoundDefinition
{
    public string roundName;
    public List<WaveSpawn> waves = new List<WaveSpawn>();
    public List<WaveSpawn> rewardWaves = new List<WaveSpawn>();
    public float waveStartDelay = 2f;
    public bool autoStartWaves = true;

    [Header("Player Settings")]
    public RoundPlayerSettings playerSettings = new RoundPlayerSettings();

    [Header("Map Settings")]
    public Sprite backgroundMap;
    public BackgroundEffectSettings backgroundEffect = new BackgroundEffectSettings();

    [Header("Enemy Settings")]
    public RoundEnemySettings enemySettings = new RoundEnemySettings();
}

[Serializable]
public class BackgroundEffectSettings
{
    public BackgroundEffectType effectType = BackgroundEffectType.None;
    [Range(0.1f, 10f)] public float duration = 2f;
    [Range(0f, 1f)] public float fadeAlpha = 0.8f;
    [Range(0f, 1f)] public float amplitude = 0.05f;
    public Vector2 panOffset = new Vector2(0.5f, 0f);
    public Ease ease = Ease.InOutSine;
}

public enum BackgroundEffectType
{
    None,
    FadePulse,
    ScalePulse,
    FloatY,
    Pan
}

[Serializable]
public class RoundPlayerSettings
{
    public int maxHealth = 100;
    public int maxShield = 0;
}

[Serializable]
public class RoundEnemySettings
{
    public int maxHealth = 100;
    public int maxShield = 0;
    [Header("Moves")]
    public float punchChargeTime = 5f;
    public int punchValue = 1;
    public float shieldChargeTime = 5f;
    public int shieldValue = 1;
}
