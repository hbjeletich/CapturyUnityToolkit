using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PongPlayer : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Head";
    [SerializeField] private string armMapName = "Arms";
    [SerializeField] private int playerNumber = 1;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    [SerializeField] private float armRaiseThreshold = 0.3f;
    
    [Header("Visual Settings")]
    [SerializeField] private float colorTransitionSpeed = 5f;
    [SerializeField] private float countdownFadeDuration = 3.5f; // Matches countdown length
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    public bool keyboardInput = false;
    
    private InputActionAsset instancedActions;
    private InputAction headPositionAction;
    private InputAction leftHandRaiseAction, rightHandRaiseAction;
    private CapturyInput myDevice;

    private bool isHorizontalPaddle = false;

    private bool isleftHandRaised = false;
    private bool isRightHandRaised = false;
    private bool debugReady = false; // For keyboard debug mode

    private SpriteRenderer spriteRenderer;
    private Color defaultColor = Color.white;
    private Color targetColor;
    private bool isReady = false;

    public int PlayerNumber => playerNumber;
    
    void Awake()
    {
        isHorizontalPaddle = (playerNumber == 3 || playerNumber == 4);
        Debug.Log($"[PongPlayer {playerNumber}] Is horizontal paddle: {isHorizontalPaddle}");

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
        if (inputActions == null)
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] No InputActionAsset assigned, motion tracking disabled");
            return;
        }

        FindMyDevice();
        
        if (myDevice == null && !keyboardInput)
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find CapturyInput device for Player{playerNumber}. Will retry each frame.");
            return;
        }

        instancedActions = Instantiate(inputActions);
        Debug.Log($"[PongPlayer {playerNumber}] Created instanced InputActionAsset");
        
        var actionMap = instancedActions.FindActionMap(actionMapName);
        if (actionMap == null)
        {
            Debug.LogError($"[PongPlayer {playerNumber}] Action map '{actionMapName}' not found!");
        }
        
        headPositionAction = actionMap.FindAction("headPosition");

        var armMap = instancedActions.FindActionMap(armMapName);
        leftHandRaiseAction = armMap.FindAction("lefthandPosition");
        rightHandRaiseAction = armMap.FindAction("rightHandPosition");
    }
    
    private void FindMyDevice()
    {
        Debug.Log($"[PongPlayer {playerNumber}] ===== SEARCHING FOR MY DEVICE =====");
        
        int capturyCount = 0;
        foreach (var device in InputSystem.devices)
        {
            if (device is CapturyInput capturyDevice)
            {
                capturyCount++;
                string usages = device.usages.Count > 0 ? string.Join(", ", device.usages) : "none";
                
                Debug.Log($"[PongPlayer {playerNumber}]   CapturyInput #{capturyCount}:");
                Debug.Log($"[PongPlayer {playerNumber}]     Name: {device.name}");
                Debug.Log($"[PongPlayer {playerNumber}]     Usages: {usages}");
                Debug.Log($"[PongPlayer {playerNumber}]     Path: {device.path}");
                
                bool isMyDevice = false;
                foreach (var usage in device.usages)
                {
                    if (usage == $"Player{playerNumber}")
                    {
                        isMyDevice = true;
                        break;
                    }
                }
                
                if (isMyDevice)
                {
                    myDevice = capturyDevice;
                }
            }
        }
        
        Debug.Log($"[PongPlayer {playerNumber}] Total CapturyInput devices: {capturyCount}");
        
        if (myDevice != null)
        {
            Debug.Log($"[PongPlayer {playerNumber}] Successfully found my device: {myDevice.name}");
        }
        else
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find device with usage 'Player{playerNumber}'");
        }
    }
    
    void OnEnable()
    {
        headPositionAction?.Enable();
        leftHandRaiseAction?.Enable();
        rightHandRaiseAction?.Enable();
    }
    
    void OnDisable()
    {
        headPositionAction?.Disable();
        leftHandRaiseAction?.Disable();
        rightHandRaiseAction?.Disable();
    }
    
    void OnDestroy()
    {
        if (instancedActions != null)
        {
            instancedActions.Disable();
            Destroy(instancedActions);
        }
    }
    
    void Update()
    {
        if (myDevice == null && !keyboardInput)
        {
            if (Time.frameCount % 60 == 0)
            {
                FindMyDevice();
            }
        }

        HandleHandRaiseInput();
        HandleDebugReadyInput();
        UpdateReadyState();
        UpdatePaddleColor();
        HandleMovement();
    }

    private void HandleHandRaiseInput()
    {
        float leftHandValue = leftHandRaiseAction?.ReadValue<Vector3>().y ?? 0f;
        float rightHandValue = rightHandRaiseAction?.ReadValue<Vector3>().y ?? 0f;

        if (leftHandValue > armRaiseThreshold)
        {
            if (!isleftHandRaised)
            {
                isleftHandRaised = true;
                Debug.Log($"[PongPlayer {playerNumber}] Left hand raised (value: {leftHandValue})");
            }
        }
        else
        {
            if (isleftHandRaised)
            {
                isleftHandRaised = false;
                Debug.Log($"[PongPlayer {playerNumber}] Left hand lowered (value: {leftHandValue})");
            }
        }

        if (rightHandValue > armRaiseThreshold)
        {
            if (!isRightHandRaised)
            {
                isRightHandRaised = true;
                Debug.Log($"[PongPlayer {playerNumber}] Right hand raised (value: {rightHandValue})");
            }
        }
        else
        {
            if (isRightHandRaised)
            {
                isRightHandRaised = false;
                Debug.Log($"[PongPlayer {playerNumber}] Right hand lowered (value: {rightHandValue})");
            }
        }
    }

    private void HandleDebugReadyInput()
    {
        if (!keyboardInput) return;

        // Press 1 to toggle P1 ready, 2 to toggle P2 ready
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
                // Set target color to player color
                if (PongUIManager.Instance != null)
                {
                    targetColor = PongUIManager.Instance.GetPlayerColor(playerNumber);
                }
                else
                {
                    // Fallback colors if UIManager not available
                    targetColor = playerNumber == 1 
                        ? new Color(1f, 0f, 1f)  // Magenta
                        : new Color(0f, 1f, 1f); // Cyan
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

        // Smoothly transition to target color
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
            
            if (showDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[PongPlayer {playerNumber}] KEYBOARD MODE - NewPos: {newPosition}");
            }
        }
        else
        {
            if (myDevice == null)
            {
                if (showDebugLogs && Time.frameCount % 300 == 0)
                {
                    Debug.LogWarning($"[PongPlayer {playerNumber}] myDevice is NULL in Update!");
                }
                return;
            }

            Vector3 headPos = myDevice.headPosition.ReadValue();
            newPosition = headPos.z;
            
            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[PongPlayer {playerNumber}] TRACKING MODE - Device: {myDevice.name}, Head pos: {headPos}, Z: {headPos.z}, NewPos: {newPosition}");
            }
        }

        float clampedPosition = Mathf.Clamp(newPosition, minY, maxY);
        
        if (showDebugLogs && Time.frameCount % 120 == 0 && clampedPosition != newPosition)
        {
            Debug.Log($"[PongPlayer {playerNumber}] Position clamped from {newPosition} to {clampedPosition} (bounds: {minY} to {maxY})");
        }

        Vector3 oldPosition = transform.position;
        if (isHorizontalPaddle)
        {
            transform.position = new Vector3(clampedPosition, transform.position.y, transform.position.z);
        }
        else
        {
            transform.position = new Vector3(transform.position.x, clampedPosition, transform.position.z);
        }
        
        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"[PongPlayer {playerNumber}] Position: {oldPosition} -> {transform.position}");
        }
    }

    public void SetPlayerNumber(int number)
    {
        Debug.Log($"[PongPlayer {playerNumber}] SetPlayerNumber called, changing from {playerNumber} to {number}");
        playerNumber = number;
        
        myDevice = null;
        FindMyDevice();
    }   

    public bool AreBothHandsRaised()
    {
        // In keyboard debug mode, use debug ready state
        if (keyboardInput)
        {
            return debugReady;
        }
        
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
            
            // Ease out for a nice feel
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            spriteRenderer.color = Color.Lerp(startColor, defaultColor, easedT);
            yield return null;
        }

        spriteRenderer.color = defaultColor;
        targetColor = defaultColor;
    }
}
