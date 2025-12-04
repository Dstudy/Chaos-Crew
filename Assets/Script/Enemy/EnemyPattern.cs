using System;
using System.Collections;
using System.Collections.Generic;
using Script.Enemy;
using UnityEngine;
using UnityEngine.UI;
using static CONST;

[System.Serializable]
public struct EnemyMove
{
    public string moveName;
    public EnemyActionType actionType;
    public float chargeTime;
    public Color indicatorColor;
    public int value;
}

public class EnemyPattern : MonoBehaviour
{
    [Header("Pattern Configuration")]
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemyMove[] moves; // List of moves to cycle through
    [SerializeField] private float timeBetweenMoves = 2.0f;

    [SerializeField] private GameObject worldSpaceCanvas;
    [SerializeField] private Image indicatorImage;
    [SerializeField] private Image attackIcon;
    [SerializeField] private Image defenseIcon;

    [SerializeField] private GameObject stunIcon;
    // [SerializeField] private TextMeshProUGUI countdownText;

    private bool isStuned;

    // private void Start()
    // {
    //     // if(enemy.isLocalEnemy)
    //         StartCoroutine(PatternRoutine());
    // }

    public void StartEnemyPattern()
    {
        StartCoroutine(PatternRoutine());
    }

    private IEnumerator PatternRoutine()
    {
        int moveIndex = 0;

        while (true) // Infinite loop for enemy behavior
        {
            // 1. Wait before starting next move
            yield return new WaitForSeconds(timeBetweenMoves);

            // 2. Get the current move
            EnemyMove currentMove = moves[moveIndex];

            // 3. TELEGRAPH PHASE (Countdown)
            yield return StartCoroutine(PerformTelegraph(currentMove));

            // 4. ACTION PHASE
            if (isStuned)
            {
                isStuned = false;
                stunIcon.SetActive(false);
                Debug.Log(gameObject.name + " da het bi stun");
                ObserverManager.InvokeEvent(ENEMY_OUT_STUN);
            }
            else
            {
                PerformAction(currentMove);
            }
            
            // 5. Cycle to next move
            moveIndex = (moveIndex + 1) % moves.Length; 
        }
    }
    
    

    public void ApplyStun()
    {
        if (isStuned)
        {
            Debug.Log("Stunned before");
            return;
        }
        Debug.Log("Stun Enemy " + gameObject.name);
        isStuned = true;
        
        attackIcon.enabled = false;
        defenseIcon.enabled = false;
        stunIcon.SetActive(true);
    }
    
    private IEnumerator PerformTelegraph(EnemyMove move)
    {
        // worldSpaceCanvas.SetActive(true);
        // indicatorImage.color = move.indicatorColor;
        indicatorImage.fillAmount = 0;
        if (move.actionType == EnemyActionType.Attack)
        {
            attackIcon.enabled = true;
            defenseIcon.enabled = false;
        }
        else
        {
            attackIcon.enabled = false;
            defenseIcon.enabled = true;
        }
        float timer = move.chargeTime;

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            // Update UI
            // countdownText.text = Mathf.Ceil(timer).ToString(); // 3, 2, 1...
            indicatorImage.fillAmount = (float)1 - (timer / move.chargeTime); // Fill circle

            // Make UI face the camera (Billboard effect)
            // if(Camera.main != null)
            //     worldSpaceCanvas.transform.rotation = Camera.main.transform.rotation;

            yield return null;
        }

        // worldSpaceCanvas.gameObject.SetActive(false);
    }
    
    private void PerformAction(EnemyMove move)
    {
        Debug.Log(move.moveName + " " + move.value);
        switch (move.actionType)
        {
            case EnemyActionType.Attack:
                enemy.DoAttack(move.value, enemy.Pos);
                break;
            case EnemyActionType.Shield:
                enemy.DoShield(move.value);
                break;
        }
    }
}
