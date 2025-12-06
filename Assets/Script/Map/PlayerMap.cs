using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using static CONST;

public class PlayerMap : NetworkBehaviour
{
    [SyncVar]
    public int mapPos;
    public GameObject topLeft;
    public GameObject bottomRight;
    public GameObject mapBackground;
    
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;
    
    //Để 4 điểm thôi
    public List<Vector3> spawnItemPoints = new List<Vector3>();
    
    public Vector3 playerPos;
    
    
    
    public void EnableCollider(bool enable, GameObject player)
    {
        topLeft.SetActive(enable);
        bottomRight.SetActive(enable);
        mapBackground.SetActive(enable);
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
        
        // Calculate spawn point positions
        point = Camera.main.ScreenToWorldPoint(new Vector3(0 + playerPos.x, Screen.height/2, Camera.main.nearClipPlane));
        leftSpawnPoint.transform.position = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width + playerPos.x, Screen.height/2, Camera.main.nearClipPlane));
        rightSpawnPoint.transform.position = point;
        
        // Send positions to server so it can broadcast to all clients
        // if (isClient) // Only the owner client sends
        // {
        Debug.Log("Local map pos: " + mapPos);
            
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/6 + playerPos.x, Screen.height/9, Camera.main.nearClipPlane));
        spawnItemPoints[0] = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/3 + playerPos.x, Screen.height/9, Camera.main.nearClipPlane));
        spawnItemPoints[1] = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3((Screen.width*2)/3 + playerPos.x, Screen.height/9, Camera.main.nearClipPlane));
        spawnItemPoints[2] = point;
        
        point = Camera.main.ScreenToWorldPoint(new Vector3((Screen.width*5)/6 + playerPos.x, Screen.height/9, Camera.main.nearClipPlane));
        spawnItemPoints[3] = point;
        
        CmdUpdateSpawnPoints(mapPos, leftSpawnPoint.position, rightSpawnPoint.position, spawnItemPoints);
    }
    
    [Command(requiresAuthority = false)]
    private void CmdUpdateSpawnPoints(int mapPosition, Vector3 leftPos, Vector3 rightPos, List<Vector3> spawnPoints)
    {
        // Server broadcasts to all clients
        RpcUpdateSpawnPoints(mapPosition, leftPos, rightPos, spawnPoints);
    }
    
    [ClientRpc]
    private void RpcUpdateSpawnPoints(int mapPosition, Vector3 leftPos, Vector3 rightPos, List<Vector3> spawnPoints)
    {
        Debug.Log("Map pos " + mapPosition);
        if (this.mapPos == mapPosition)
        {
            Debug.Log("Da tim thay map: " + mapPos);
            if (leftSpawnPoint != null)
            {
                leftSpawnPoint.position = leftPos;
            }
            
            if (rightSpawnPoint != null)
            {
                rightSpawnPoint.position = rightPos;
            }
            spawnItemPoints = new List<Vector3>(spawnPoints);
        }
    }
}