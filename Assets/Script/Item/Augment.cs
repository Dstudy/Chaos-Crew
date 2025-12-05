using UnityEngine;

public class Augment: BaseItem
{
    public AugmentType augmentType;
    public int bonusValue;
    
    public Augment(int id, string name, AugmentType augmentType, int bonusValue) : base(id, name, ItemType.Augment, Item.Support)
    {
        this.augmentType = augmentType;
        this.bonusValue = bonusValue;
    }

    public override string GetStats()
    {
        return $"{augmentType}: {bonusValue}";
    }

    public override void UseOn(MonoBehaviour target)
    {
        Debug.Log("This is augment it cannot use on anything.");
    }
}
