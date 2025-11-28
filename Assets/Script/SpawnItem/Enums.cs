using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Attack,
    Support,
    Augment
}

public enum Element
{
    None, 
    Fire,
    Water,
    Earth,
    Air,
    Chaos,
}

public enum AugmentType
{
    Add,
    Multiple,
}

public enum SupportEffect
{
    Heal,
    Shield
}

public enum SpawnType
{
    AllAttackItemsPerElement,
    AttackAndSupport,
    Augment,
}

public enum EnemyActionType
{
    Attack,
    Shield
}