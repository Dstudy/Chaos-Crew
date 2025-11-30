using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CONST;

public class PlayerMap : MonoBehaviour
{
    public int mapPos;
    public GameObject topLeft;
    public GameObject bottomRight;

    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    
    public List<Transform> spawnItemPoints = new List<Transform>();
    
    public Vector3 playerPos;
    
    
    
    public void EnableCollider(bool enable, GameObject player)
    {
        topLeft.SetActive(enable);
        bottomRight.SetActive(enable);
        SetPointForScreen();
        
        playerPos = Camera.main.ScreenToWorldPoint(new Vector3(playerPos.x + Screen.width/2, 0, 1));
        player.transform.position = playerPos;
        
        player.GetComponent<Player>().enemy.gameObject.transform.position = new Vector3(playerPos.x, -playerPos.y, playerPos.z);
        
        ObserverManager.InvokeEvent(MAP_ENABLED, this, enable, player);
    }

    private void SetPointForScreen()
    {
        Vector3 point = new Vector3();
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(0 + playerPos.x, Screen.height, Camera.main.nearClipPlane));
        topLeft.transform.position = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width + playerPos.x, 0, Camera.main.nearClipPlane));
        bottomRight.transform.position = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(0 + playerPos.x, Screen.height/2, Camera.main.nearClipPlane));
        leftSpawnPoint.transform.position = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width + playerPos.x, Screen.height/2, Camera.main.nearClipPlane));
        rightSpawnPoint.transform.position = point;
    }
}
