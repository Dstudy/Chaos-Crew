using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

[System.Serializable]
public abstract class BaseItem
{
    public int id;
    public string name;
    public ItemType type;
    public GameObject prefab;
    private Sprite sprite;

    public Sprite icon
    {
        get => sprite != null ? sprite : prefab.GetComponent<SpriteRenderer>().sprite;
    }

    public virtual void setIcon(Sprite sprite)
    {
        this.sprite = sprite;  
    }

    public PolygonCollider2D collider2D
    {
        get => prefab.GetComponent<PolygonCollider2D>();
    }

    public BaseItem(int id, string name, ItemType type)
    {
        this.id = id;
        this.name = name;
        this.type = type;
    }

    public abstract string GetStats();
    
    public abstract void UseOn(MonoBehaviour target);
}
