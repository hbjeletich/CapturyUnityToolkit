using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongPlayerManager : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxPlayers = 4;
    
    [Header("Spawn Positions")]
    [SerializeField] private Transform leftSpawn;    // Player 1
    [SerializeField] private Transform rightSpawn;   // Player 2
    [SerializeField] private Transform topSpawn;     // Player 3
    [SerializeField] private Transform bottomSpawn;  // Player 4
    
    [Header("Paddle Constraints")]
    [SerializeField] private Vector2 verticalPaddleBounds = new Vector2(-4f, 4f);  // Y bounds for left/right
    [SerializeField] private Vector2 horizontalPaddleBounds = new Vector2(-8f, 8f); // X bounds for top/bottom
    
    [Header("Debug")]
    [SerializeField] private bool keyboardDebugMode = false;
    [SerializeField] private bool showDebugLogs = true;
    
    private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();
    private MultiplayerMotionTrackingManager motionManager;
    
    void Start()
    {
        // find the motion tracking manager
        motionManager = MultiplayerMotionTrackingManager.Instance;
        
        if (motionManager != null)
        {
            // subscribe to skeleton events
            if (showDebugLogs)
                Debug.Log("PongPlayerManager: Connected to MultiplayerMotionTrackingManager");
        }
        else if (!keyboardDebugMode)
        {
            Debug.LogWarning("PongPlayerManager: MultiplayerMotionTrackingManager not found! Players won't spawn automatically.");
        }
    }
    
    void Update()
    {
        // check for new skeletons in motion tracking mode
        if (motionManager != null && !keyboardDebugMode)
        {
            CheckForNewPlayers();
        }
        
        // keyboard debug mode: press enter to add player
        if (keyboardDebugMode && Input.GetKeyDown(KeyCode.Return))
        {
            AddDebugPlayer();
        }
    }
    
    private void CheckForNewPlayers()
    {
        List<int> allPlayerNumbers = motionManager.GetAllPlayerNumbers();
        
        foreach (int playerNumber in allPlayerNumbers)
        {
            // if we haven't spawned this player yet, spawn them
            if (!spawnedPlayers.ContainsKey(playerNumber) && playerNumber <= maxPlayers)
            {
                SpawnPlayer(playerNumber, false);
            }
        }
        
        // clean up players that are no longer tracked
        List<int> playersToRemove = new List<int>();
        foreach (int playerNumber in spawnedPlayers.Keys)
        {
            if (!allPlayerNumbers.Contains(playerNumber))
            {
                playersToRemove.Add(playerNumber);
            }
        }
        
        foreach (int playerNumber in playersToRemove)
        {
            RemovePlayer(playerNumber);
        }
    }
    
    private void AddDebugPlayer()
    {
        // find next available player slot
        int nextPlayerNumber = spawnedPlayers.Count + 1;
        
        if (nextPlayerNumber > maxPlayers)
        {
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Max players ({maxPlayers}) reached!");
            return;
        }
        
        SpawnPlayer(nextPlayerNumber, true);
    }
    
    private void SpawnPlayer(int playerNumber, bool isKeyboardMode)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PongPlayerManager: Player prefab not assigned!");
            return;
        }
        
        // get spawn position and rotation based on player number
        Transform spawnTransform = GetSpawnTransform(playerNumber);
        if (spawnTransform == null)
        {
            Debug.LogError($"PongPlayerManager: No spawn position configured for player {playerNumber}!");
            return;
        }
        
        // instantiate the player
        GameObject playerObject = Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);
        playerObject.name = $"Player{playerNumber}";
        
        // configure the PongPlayer component
        PongPlayer pongPlayer = playerObject.GetComponent<PongPlayer>();
        if (pongPlayer != null)
        {            
            pongPlayer.SetPlayerNumber(playerNumber);
            
            pongPlayer.keyboardInput = isKeyboardMode;
            
            SetPlayerBounds(pongPlayer, playerNumber);
            
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Spawned Player {playerNumber} at {spawnTransform.position} (Keyboard: {isKeyboardMode})");

            if (playerNumber > 2)
                playerObject.transform.Rotate(0f, 0f, 90f);
        }
        else
        {
            Debug.LogError("PongPlayerManager: Player prefab doesn't have PongPlayer component!");
            Destroy(playerObject);
            return;
        }
        
        spawnedPlayers.Add(playerNumber, playerObject);
    }
    
    private void RemovePlayer(int playerNumber)
    {
        if (spawnedPlayers.TryGetValue(playerNumber, out GameObject playerObject))
        {
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Removing Player {playerNumber}");
            
            Destroy(playerObject);
            spawnedPlayers.Remove(playerNumber);
        }
    }
    
    private Transform GetSpawnTransform(int playerNumber)
    {
        switch (playerNumber)
        {
            case 1: return leftSpawn;
            case 2: return rightSpawn;
            case 3: return topSpawn;
            case 4: return bottomSpawn;
            default: return null;
        }
    }
    
    private void SetPlayerBounds(PongPlayer player, int playerNumber)
    {
        // players 1 & 2 move vertically (left/right)
        // players 3 & 4 move horizontally (top/bottom)
        
        if (playerNumber == 1 || playerNumber == 2)
        {
            // vertical
            player.GetType().GetField("minY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, verticalPaddleBounds.x);
            
            player.GetType().GetField("maxY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, verticalPaddleBounds.y);
        }
        else
        {
            // horizontal paddles
            player.GetType().GetField("minY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, horizontalPaddleBounds.x);
            
            player.GetType().GetField("maxY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, horizontalPaddleBounds.y);
        }
    }
    
    public int GetPlayerCount()
    {
        return spawnedPlayers.Count;
    }
    
    public GameObject GetPlayerObject(int playerNumber)
    {
        spawnedPlayers.TryGetValue(playerNumber, out GameObject player);
        return player;
    }
}