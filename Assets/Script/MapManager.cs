using System;
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

    private void Awake()
    {
        EnableCollider(false);
    }

    public void EnableCollider(bool enable)
    {
        Debug.Log("Enable map " + gameObject.name);
        leftCollider.SetActive(enable);
        rightCollider.SetActive(enable);
        playSide.SetActive(enable);
    }
    
}
