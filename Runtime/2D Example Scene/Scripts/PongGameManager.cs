using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PongGameManager : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform ballSpawnPoint;
    
    [Header("Boundaries")]
    [SerializeField] private float leftBound = -10f;
    [SerializeField] private float rightBound = 10f;
    [SerializeField] private float topBound = 6f;
    [SerializeField] private float bottomBound = -6f;
    
    private int player1Score = 0;
    private int player2Score = 0;
    private int player3Score = 0;
    private int player4Score = 0;
    
    private GameObject currentBall;
    
    void Start()
    {
        SpawnBall();
    }
    
    void Update()
    {
        if (currentBall == null)
        {
            SpawnBall();
            return;
        }
        
        Vector3 ballPos = currentBall.transform.position;
        
        // check left/right boundaries (P1/P2)
        if (ballPos.x < leftBound)
        {
            Player2Scores();  // ball went left, right scores
        }
        else if (ballPos.x > rightBound)
        {
            Player1Scores();  // ball went right, left scores
        }
        
        // check top/bottom boundaries (P3/P4)
        if (ballPos.y > topBound)
        {
            Player4Scores();  // ball went top, bottom scores
        }
        else if (ballPos.y < bottomBound)
        {
            Player3Scores();  // ball went bottom, top scores
        }
    }
    
    void SpawnBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("PongGameManager: Ball prefab not assigned!");
            return;
        }
        
        Vector3 spawnPosition = ballSpawnPoint != null ? ballSpawnPoint.position : Vector3.zero;
        currentBall = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
        currentBall.name = "PongBall";
    }
    
    void Player1Scores()
    {
        player1Score++;
        UpdateScore();
        DestroyBallAndSpawnNew();
    }
    
    void Player2Scores()
    {
        player2Score++;
        UpdateScore();
        DestroyBallAndSpawnNew();
    }
    
    void Player3Scores()
    {
        player3Score++;
        UpdateScore();
        DestroyBallAndSpawnNew();
    }
    
    void Player4Scores()
    {
        player4Score++;
        UpdateScore();
        DestroyBallAndSpawnNew();
    }
    
    void DestroyBallAndSpawnNew()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }
        
        // spawn new ball after a short delay
        Invoke(nameof(SpawnBall), 1f);
    }
    
    void UpdateScore()
    {
        Debug.Log($"Scores - Player 1: {player1Score}, Player 2: {player2Score}, Player 3: {player3Score}, Player 4: {player4Score}");
    }
}