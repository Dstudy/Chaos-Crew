using UnityEngine;

[CreateAssetMenu(fileName = "New Attack Item", menuName = "Items/Attack Item Data")]
public class AttackItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public int damage;
    public Element element;
    public GameObject prefab;

    public AttackItem CreateAttackItem()
    {
        var item = new AttackItem(id, itemName, damage, element, Item.Attack);
        item.prefab = prefab;
        return item;
    }
}

