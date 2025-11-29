using UnityEngine;

[CreateAssetMenu(fileName = "New Hammer Item", menuName = "Items/Hammer Data")]
public class HammerData : ScriptableObject
{
    public int id;
    public string itemName;
    public int damage;
    public Element element;
    public GameObject prefab;

    public HammerItem CreateHammerItem()
    {
        var item = new HammerItem(id, itemName, damage, element);
        item.prefab = prefab;
        return item;
    }
    
}