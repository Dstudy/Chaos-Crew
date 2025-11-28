using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Spawn", menuName = "Items/Wave Spawn")]
public class WaveSpawn : ScriptableObject
{
    public int waveNumber;
    public SpawnType spawnType;
    
    [Header("Item Data")]
    public List<AttackItemData> attackItemData;
    public List<SupportItemData> supportItemData;
    public List<AugmentData> augmentData;
    public List<HammerData> hammerData;
    
    [Header("Spawn Configuration")]
    public float spawnDelay = 0.5f;
    public int waveCount = 1;
    public int itemsPerPlayer = 100;
    public Vector3 spawnOffset = Vector3.zero;
}
