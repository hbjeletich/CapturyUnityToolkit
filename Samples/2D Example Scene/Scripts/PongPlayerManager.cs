using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages pong player spawning and positioning.
/// 
/// In tracking mode:
///   - CapturyPlayerInputBridge calls PlayerInputManager.JoinPlayer() when skeletons are ready
///   - PlayerInputManager spawns the prefab with a paired PlayerInput
///   - This script listens to PlayerInputManager.onPlayerJoined to configure positioning/bounds
///
/// In keyboard debug mode:
///   - Press Enter to add a debug player (spawned manually without PlayerInput)
/// </summary>
public class PongPlayerManager : MonoBehaviour
{
    [Header("Player Setup")]
    [SerializeField] private GameObject playerPrefab; // only used for keyboard debug mode
    [SerializeField] private int maxPlayers = 4;
    
    [Header("Spawn Positions")]
    [SerializeField] private Transform leftSpawn;    // Player 1
    [SerializeField] private Transform rightSpawn;   // Player 2
    [SerializeField] private Transform topSpawn;     // Player 3
    [SerializeField] private Transform bottomSpawn;  // Player 4
    
    [Header("Paddle Constraints")]
    [SerializeField] private Vector2 verticalPaddleBounds = new Vector2(-4f, 4f);
    [SerializeField] private Vector2 horizontalPaddleBounds = new Vector2(-8f, 8f);
    
    [Header("Debug")]
    [SerializeField] private bool keyboardDebugMode = false;
    [SerializeField] private bool showDebugLogs = true;
    
    private Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();

    private bool allowNewPlayers = true;

    public static PongPlayerManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        verticalPaddleBounds = new Vector2(
            PongGameManager.Instance.GetBoundaryMins().y + 1f,
            PongGameManager.Instance.GetBoundaryMaxs().y - 1f
        );

        horizontalPaddleBounds = new Vector2(
            PongGameManager.Instance.GetBoundaryMins().x + 1f,
            PongGameManager.Instance.GetBoundaryMaxs().x - 1f
        );

        // in tracking mode, listen for players joined through PlayerInputManager
        if (!keyboardDebugMode)
        {
            var pim = PlayerInputManager.instance;
            if (pim != null)
            {
                pim.onPlayerJoined += OnPlayerJoined;
                pim.onPlayerLeft += OnPlayerLeft;

                if (showDebugLogs)
                    Debug.Log("PongPlayerManager: Listening to PlayerInputManager events");
            }
            else
            {
                Debug.LogWarning("PongPlayerManager: No PlayerInputManager found! " +
                               "Add PlayerInputManager + CapturyPlayerInputBridge to the scene.");
            }
        }
    }

    void OnDestroy()
    {
        if (!keyboardDebugMode && PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
            PlayerInputManager.instance.onPlayerLeft -= OnPlayerLeft;
        }
    }
    
    void Update()
    {
        // keyboard debug mode: press enter to add player
        if (keyboardDebugMode && Input.GetKeyDown(KeyCode.Return))
        {
            AddDebugPlayer();
        }
    }

    /// <summary>
    /// Called by PlayerInputManager when a player is joined (via CapturyPlayerInputBridge).
    /// The PlayerInput component is already on the spawned prefab, paired with the correct device.
    /// </summary>
    private void OnPlayerJoined(PlayerInput playerInput)
    {
        if (!allowNewPlayers)
        {
            if (showDebugLogs)
                Debug.Log("PongPlayerManager: New player blocked — game already started.");
            Destroy(playerInput.gameObject);
            return;
        }

        // playerIndex is zero-based, our player numbers are one-based
        int playerNumber = playerInput.playerIndex + 1;

        if (playerNumber > maxPlayers)
        {
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Player {playerNumber} exceeds max ({maxPlayers}), rejecting.");
            Destroy(playerInput.gameObject);
            return;
        }

        GameObject playerObject = playerInput.gameObject;
        playerObject.name = $"Player{playerNumber}";

        // position the player at the correct spawn point
        Transform spawnTransform = GetSpawnTransform(playerNumber);
        if (spawnTransform != null)
        {
            playerObject.transform.position = spawnTransform.position;
            playerObject.transform.rotation = spawnTransform.rotation;
        }

        // configure the PongPlayer component
        PongPlayer pongPlayer = playerObject.GetComponent<PongPlayer>();
        if (pongPlayer != null)
        {
            pongPlayer.SetPlayerNumber(playerNumber);
            pongPlayer.keyboardInput = false;
            SetPlayerBounds(pongPlayer, playerNumber);

            if (playerNumber > 2)
                playerObject.transform.Rotate(0f, 0f, 90f);

            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Player {playerNumber} joined at {playerObject.transform.position}");
        }

        spawnedPlayers[playerNumber] = playerObject;
        PongGameManager.Instance.ActivateNextBoundary(playerNumber);
    }

    private void OnPlayerLeft(PlayerInput playerInput)
    {
        // find which player number this was
        int playerNumber = playerInput.playerIndex + 1;
        
        if (spawnedPlayers.ContainsKey(playerNumber))
        {
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Player {playerNumber} left");

            spawnedPlayers.Remove(playerNumber);
            PongGameManager.Instance.DeactivateBoundary(playerNumber);
        }
    }
    
    private void AddDebugPlayer()
    {
        int nextPlayerNumber = spawnedPlayers.Count + 1;
        
        if (nextPlayerNumber > maxPlayers)
        {
            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Max players ({maxPlayers}) reached!");
            return;
        }
        
        SpawnDebugPlayer(nextPlayerNumber);
    }
    
    private void SpawnDebugPlayer(int playerNumber)
    {
        if (!allowNewPlayers) return;

        if (playerPrefab == null)
        {
            Debug.LogError("PongPlayerManager: Player prefab not assigned!");
            return;
        }
        
        Transform spawnTransform = GetSpawnTransform(playerNumber);
        if (spawnTransform == null)
        {
            Debug.LogError($"PongPlayerManager: No spawn position for player {playerNumber}!");
            return;
        }
        
        GameObject playerObject = Instantiate(playerPrefab, spawnTransform.position, spawnTransform.rotation);
        playerObject.name = $"Player{playerNumber}";
        
        PongPlayer pongPlayer = playerObject.GetComponent<PongPlayer>();
        if (pongPlayer != null)
        {            
            pongPlayer.SetPlayerNumber(playerNumber);
            pongPlayer.keyboardInput = true;
            SetPlayerBounds(pongPlayer, playerNumber);
            
            if (playerNumber > 2)
                playerObject.transform.Rotate(0f, 0f, 90f);

            if (showDebugLogs)
                Debug.Log($"PongPlayerManager: Spawned debug Player {playerNumber} at {spawnTransform.position}");
        }
        else
        {
            Debug.LogError("PongPlayerManager: Prefab missing PongPlayer component!");
            Destroy(playerObject);
            return;
        }
        
        spawnedPlayers.Add(playerNumber, playerObject);
        PongGameManager.Instance.ActivateNextBoundary(playerNumber);
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
        if (playerNumber == 1 || playerNumber == 2)
        {
            player.GetType().GetField("minY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, verticalPaddleBounds.x);
            
            player.GetType().GetField("maxY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, verticalPaddleBounds.y);
        }
        else
        {
            player.GetType().GetField("minY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, horizontalPaddleBounds.x);
            
            player.GetType().GetField("maxY", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(player, horizontalPaddleBounds.y);
        }
    }

    public bool ReadyToStartGame()
    {
        foreach (int playerNumber in spawnedPlayers.Keys)
        {
            GameObject playerObject = spawnedPlayers[playerNumber];
            PongPlayer pongPlayer = playerObject.GetComponent<PongPlayer>();
            if (pongPlayer != null && !pongPlayer.AreBothHandsRaised())
                return false;
        }

        if (spawnedPlayers.Count > 0) allowNewPlayers = false;
        return spawnedPlayers.Count > 0;
    }
    
    public int GetPlayerCount() => spawnedPlayers.Count;
    
    public GameObject GetPlayerObject(int playerNumber)
    {
        spawnedPlayers.TryGetValue(playerNumber, out GameObject player);
        return player;
    }
}