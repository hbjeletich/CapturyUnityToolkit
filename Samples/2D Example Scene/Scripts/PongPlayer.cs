using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Pong paddle controller that works with Unity's PlayerInput component.
/// 
/// When using PlayerInputManager + CapturyPlayerInputBridge:
///   - PlayerInput is automatically added and paired with the correct CapturyInput device
///   - This script reads input through PlayerInput's actions (no manual device hunting)
///   - The InputActionAsset is instanced automatically by PlayerInput
///
/// When using keyboard debug mode:
///   - Set keyboardInput = true, no PlayerInput component needed
///   - Uses legacy Input axes for movement
/// </summary>
public class PongPlayer : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private string headPositionActionName = "headPosition";
    [SerializeField] private string leftHandPositionActionName = "lefthandPosition";
    [SerializeField] private string rightHandPositionActionName = "rightHandPosition";
    [SerializeField] private int playerNumber = 1;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    [SerializeField] private float armRaiseThreshold = 0.3f;
    
    [Header("Visual Settings")]
    [SerializeField] private float colorTransitionSpeed = 5f;
    [SerializeField] private float countdownFadeDuration = 3.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    public bool keyboardInput = false;
    
    // PlayerInput-driven actions (resolved from the PlayerInput component)
    private PlayerInput playerInput;
    private InputAction headPositionAction;
    private InputAction leftHandRaiseAction;
    private InputAction rightHandRaiseAction;

    private bool isHorizontalPaddle = false;

    private bool isleftHandRaised = false;
    private bool isRightHandRaised = false;
    private bool debugReady = false;

    private SpriteRenderer spriteRenderer;
    private Color defaultColor = Color.white;
    private Color targetColor;
    private bool isReady = false;

    public int PlayerNumber => playerNumber;
    
    void Awake()
    {
        isHorizontalPaddle = (playerNumber == 3 || playerNumber == 4);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
            targetColor = defaultColor;
        }

        SetupInput();
    }
    
    private void SetupInput()
    {
        if (keyboardInput) return;

        // get the PlayerInput component — added automatically by PlayerInputManager
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] No PlayerInput component found. " +
                           "If using CapturyPlayerInputBridge, make sure the player prefab has a PlayerInput component.");
            return;
        }

        // resolve actions from PlayerInput's instanced action asset
        // PlayerInput automatically instances the InputActionAsset and pairs it with the correct device
        headPositionAction = playerInput.actions.FindAction(headPositionActionName);
        leftHandRaiseAction = playerInput.actions.FindAction(leftHandPositionActionName);
        rightHandRaiseAction = playerInput.actions.FindAction(rightHandPositionActionName);

        if (headPositionAction == null)
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find action '{headPositionActionName}'");
        if (leftHandRaiseAction == null)
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find action '{leftHandPositionActionName}'");
        if (rightHandRaiseAction == null)
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find action '{rightHandPositionActionName}'");

        if (showDebugLogs)
        {
            var devices = playerInput.devices;
            string deviceNames = devices.Count > 0 ? string.Join(", ", devices) : "none";
            Debug.Log($"[PongPlayer {playerNumber}] PlayerInput setup complete — " +
                     $"Scheme: {playerInput.currentControlScheme}, Devices: {deviceNames}");
        }
    }
    
    void Update()
    {
        HandleHandRaiseInput();
        HandleDebugReadyInput();
        UpdateReadyState();
        UpdatePaddleColor();
        HandleMovement();
    }

    private void HandleHandRaiseInput()
    {
        if (keyboardInput) return;

        float leftHandValue = leftHandRaiseAction?.ReadValue<Vector3>().y ?? 0f;
        float rightHandValue = rightHandRaiseAction?.ReadValue<Vector3>().y ?? 0f;

        if (leftHandValue > armRaiseThreshold)
        {
            if (!isleftHandRaised)
            {
                isleftHandRaised = true;
                if (showDebugLogs) Debug.Log($"[PongPlayer {playerNumber}] Left hand raised (value: {leftHandValue})");
            }
        }
        else
        {
            if (isleftHandRaised)
            {
                isleftHandRaised = false;
                if (showDebugLogs) Debug.Log($"[PongPlayer {playerNumber}] Left hand lowered (value: {leftHandValue})");
            }
        }

        if (rightHandValue > armRaiseThreshold)
        {
            if (!isRightHandRaised)
            {
                isRightHandRaised = true;
                if (showDebugLogs) Debug.Log($"[PongPlayer {playerNumber}] Right hand raised (value: {rightHandValue})");
            }
        }
        else
        {
            if (isRightHandRaised)
            {
                isRightHandRaised = false;
                if (showDebugLogs) Debug.Log($"[PongPlayer {playerNumber}] Right hand lowered (value: {rightHandValue})");
            }
        }
    }

    private void HandleDebugReadyInput()
    {
        if (!keyboardInput) return;

        if (playerNumber == 1 && Input.GetKeyDown(KeyCode.Alpha1))
        {
            debugReady = !debugReady;
            Debug.Log($"[PongPlayer {playerNumber}] Debug ready toggled: {debugReady}");
        }
        else if (playerNumber == 2 && Input.GetKeyDown(KeyCode.Alpha2))
        {
            debugReady = !debugReady;
            Debug.Log($"[PongPlayer {playerNumber}] Debug ready toggled: {debugReady}");
        }
    }

    private void UpdateReadyState()
    {
        bool wasReady = isReady;
        isReady = AreBothHandsRaised();

        if (isReady != wasReady)
        {
            if (isReady)
            {
                if (PongUIManager.Instance != null)
                {
                    targetColor = PongUIManager.Instance.GetPlayerColor(playerNumber);
                }
                else
                {
                    targetColor = playerNumber == 1 
                        ? new Color(1f, 0f, 1f)
                        : new Color(0f, 1f, 1f);
                }
                Debug.Log($"[PongPlayer {playerNumber}] Now READY - changing to player color: {targetColor}");
            }
            else
            {
                targetColor = defaultColor;
                Debug.Log($"[PongPlayer {playerNumber}] No longer ready - returning to white");
            }
        }
    }

    private void UpdatePaddleColor()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * colorTransitionSpeed);
    }

    private void HandleMovement()
    {
        float newPosition;
        
        if (keyboardInput)
        {
            if (isHorizontalPaddle)
            {
                float horizontalInput = Input.GetAxis("Horizontal");
                newPosition = transform.position.x + (horizontalInput * moveSpeed * Time.deltaTime);
            }
            else
            {
                float verticalInput = Input.GetAxis("Vertical");
                newPosition = transform.position.y + (verticalInput * moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (headPositionAction == null) return;

            Vector3 headPos = headPositionAction.ReadValue<Vector3>();
            newPosition = headPos.z;
            
            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[PongPlayer {playerNumber}] TRACKING MODE — Head pos: {headPos}, Z: {headPos.z}");
            }
        }

        float clampedPosition = Mathf.Clamp(newPosition, minY, maxY);

        Vector3 oldPosition = transform.position;
        if (isHorizontalPaddle)
        {
            transform.position = new Vector3(clampedPosition, transform.position.y, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, clampedPosition, transform.position.z);
        }
    }

    public void SetPlayerNumber(int number)
    {
        playerNumber = number;
        isHorizontalPaddle = (playerNumber == 3 || playerNumber == 4);
    }   

    public bool AreBothHandsRaised()
    {
        if (keyboardInput) return debugReady;
        return isleftHandRaised && isRightHandRaised;
    }

    public void ResetToDefaultColor()
    {
        targetColor = defaultColor;
    }

    public void FadeToDefaultColor(float duration)
    {
        StartCoroutine(SlowFadeToDefault(duration));
    }

    private IEnumerator SlowFadeToDefault(float duration)
    {
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            spriteRenderer.color = Color.Lerp(startColor, defaultColor, easedT);
            yield return null;
        }

        spriteRenderer.color = defaultColor;
        targetColor = defaultColor;
    }
}