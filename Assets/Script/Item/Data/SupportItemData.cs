using UnityEngine;

[CreateAssetMenu(fileName = "New Support Item", menuName = "Items/Support Item Data")]
public class SupportItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public int value;
    public SupportEffect effect;
    public GameObject prefab;

    public SupportItem CreateSupportItem()
    {
        var item = new SupportItem(id, itemName, value, effect);
        item.prefab = prefab;
        return item;
    }
}

