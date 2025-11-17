using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public GameObject leftCollider;
    public GameObject rightCollider;

    public GameObject playSide;

    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    
    public List<Transform> spawnItemPoints = new List<Transform>();

    public void EnableCollider(bool enable)
    {
        leftCollider.SetActive(enable);
        rightCollider.SetActive(enable);
        playSide.SetActive(enable);
    }
}
