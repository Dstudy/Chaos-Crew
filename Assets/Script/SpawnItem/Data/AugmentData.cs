using UnityEngine;

[CreateAssetMenu(fileName = "New Augment", menuName = "Items/Augment Data")]
public class AugmentData : ScriptableObject
{
    public int id;
    public string itemName;
    public AugmentType augmentType;
    public int bonusValue;
    public GameObject prefab;

    public Augment CreateAugment()
    {
        var item = new Augment(id, itemName, augmentType, bonusValue);
        item.prefab = prefab;
        return item;
    }
}

