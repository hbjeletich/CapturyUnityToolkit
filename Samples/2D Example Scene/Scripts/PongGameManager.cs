using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PongGameManager : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform ballSpawnPoint;
    [SerializeField] private float ballRespawnDelay = 1f;
    [SerializeField] private float ballSpeed = 5f;
    
    [Header("Boundaries")]
    [SerializeField] private GameBounds leftBound;
    [SerializeField] private GameBounds rightBound;
    [SerializeField] private GameBounds topBound;
    [SerializeField] private GameBounds bottomBound;
    
    private int player1Score = 0;
    private int player2Score = 0;

    private bool isGameActive = false;
    private bool countdownStarted = false;
    
    private GameObject currentBall;

    public static PongGameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        DeactivateAllBoundaries();
    }
    
    void Update()
    {
        if (!isGameActive)
        {
            if (!countdownStarted && PongPlayerManager.Instance.ReadyToStartGame())
            {
                countdownStarted = true;
                StartCoroutine(StartGameSequence());
            }
            return;
        }

        if (currentBall == null)
        {
            return;
        }
        
        Vector3 ballPos = currentBall.transform.position;
        
        // Check left/right boundaries (P1/P2)
        if (ballPos.x < leftBound.xMin)
        {
            Player2Scores();
        }
        else if (ballPos.x > rightBound.xMax)
        {
            Player1Scores();
        }
        
        // Check top/bottom boundaries (ball just bounces)
        // Top and bottom are now just bounce walls for 2-player mode
    }

    private IEnumerator StartGameSequence()
    {
        
        // Start countdown via UI manager
        if (PongUIManager.Instance != null)
        {
            PongUIManager.Instance.StartCountdown();
        }
        
        // Wait for countdown (3 + 2 + 1 + GO + delay)
        yield return new WaitForSeconds(4.5f);
        
        isGameActive = true;
        SpawnBall();
        ResetAllPaddleColors();
    }

    private void ResetAllPaddleColors()
    {
        // Fade paddles back to white
        float fadeDuration = 2f;
        
        for (int i = 1; i <= 2; i++)
        {
            GameObject playerObj = PongPlayerManager.Instance.GetPlayerObject(i);
            if (playerObj != null)
            {
                PongPlayer player = playerObj.GetComponent<PongPlayer>();
                player?.FadeToDefaultColor(fadeDuration);
            }
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
        PongBall ballScript = currentBall.GetComponent<PongBall>();
        if (ballScript != null) ballScript.Speed = ballSpeed;
        currentBall.name = "PongBall";
    }
    
    void Player1Scores()
    {
        player1Score++;
        
        if (PongUIManager.Instance != null)
        {
            PongUIManager.Instance.Player1Scored();
        }
        
        DestroyBallAndSpawnNew();
    }
    
    void Player2Scores()
    {
        player2Score++;
        
        if (PongUIManager.Instance != null)
        {
            PongUIManager.Instance.Player2Scored();
        }
        
        DestroyBallAndSpawnNew();
    }
    
    void DestroyBallAndSpawnNew()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }
        
        Invoke(nameof(SpawnBall), ballRespawnDelay);
    }

    public void DeactivateAllBoundaries()
    {
        leftBound.ActivateCollider(false);
        rightBound.ActivateCollider(false);
        topBound.ActivateCollider(false);
        bottomBound.ActivateCollider(false);
    }

    public void ActivateNextBoundary(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                leftBound.ActivateCollider(true);
                break;
            case 2:
                rightBound.ActivateCollider(true);
                break;
            case 3:
                topBound.ActivateCollider(true);
                break;
            case 4:
                bottomBound.ActivateCollider(true);
                break;
            default:
                Debug.LogWarning("PongGameManager: Invalid player number for boundary activation.");
                break;
        }
    }

    public void DeactivateBoundary(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                leftBound.ActivateCollider(false);
                break;
            case 2:
                rightBound.ActivateCollider(false);
                break;
            case 3:
                topBound.ActivateCollider(false);
                break;
            case 4:
                bottomBound.ActivateCollider(false);
                break;
            default:
                Debug.LogWarning("PongGameManager: Invalid player number for boundary deactivation.");
                break;
        }
    }

    public Vector2 GetBoundaryMins()
    {
        return new Vector2(leftBound.xMin, bottomBound.yMin);
    }

    public Vector2 GetBoundaryMaxs()
    {
        return new Vector2(rightBound.xMax, topBound.yMax);
    }
}
