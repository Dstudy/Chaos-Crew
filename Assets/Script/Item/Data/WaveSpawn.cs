using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Spawn", menuName = "Items/Wave Spawn")]
public class WaveSpawn : ScriptableObject
{
    public int waveNumber { get; private set; }
    public SpawnType spawnType;
    
    [Header("Item Data")]
    public List<AttackItemData> attackItemData;
    public List<SupportItemData> supportItemData;
    public List<AugmentData> augmentData;
    public List<HammerData> hammerData;
    public List<StaffItemData> staffItemData;
    
    [Header("Spawn Configuration")]
    public float spawnDelay = 0.5f;
    public int waveCount = 1;
    public int itemsPerPlayer = 100;
    public Vector3 spawnOffset = Vector3.zero;

   public Item itemType;

    [Header("RandomAll Probabilities (weights)")]
    [Range(0f, 1f)] public float attackProbability = 0f;
    [Range(0f, 1f)] public float supportProbability = 0f;
    [Range(0f, 1f)] public float staffProbability = 0f;
    [Range(0f, 1f)] public float hammerProbability = 0f;

    public AttackItemData GetAttackItem(Element element)
    {
        foreach (var item in attackItemData)
        {
            if(item.element == element)
                return item;
        }
        return null;
    }
    
    public HammerData GetHammerItem(Element element)
    {
        foreach (var item in hammerData)
        {
            if(item.element == element)
                return item;
        }
        return null;
    }

    public StaffItemData GetStaffItem(Element element)
    {
        foreach (var item in staffItemData)
        {
            if(item.initialElement == element)
                return item;
        }

        return null;
    }
}
