using UnityEngine;
using UnityEngine.UI;

public class SupportItem: BaseItem
{
    public int value;
    public SupportEffect effect;
    
    private Image image;
    private Transform parentAfterDrag;
    
    public SupportItem(int id, string name, int value, SupportEffect effect) : base(id, name, ItemType.Support, Item.Support)
    {
        this.value = value;
        this.effect = effect;
    }

    public override string GetStats()
    {
        throw new System.NotImplementedException();
    }

    public override void UseOn(MonoBehaviour target)
    {
        if (target is Player player)
        {
            Debug.Log(player.name);
            player.ApplyEffect(effect, value);
        }
        
    }
    
    
}
