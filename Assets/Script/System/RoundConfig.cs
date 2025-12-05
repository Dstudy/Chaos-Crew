using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("Enemy Settings")]
    public RoundEnemySettings enemySettings = new RoundEnemySettings();
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
}
