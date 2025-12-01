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
    [SerializeField] private int playerNumber = 1;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    public bool keyboardInput = false;
    
    private InputActionAsset instancedActions;
    private InputAction headPositionAction;
    private CapturyInput myDevice;  // direct reference to this player's device

    private bool isHorizontalPaddle = false; // true for P3/P4, false for P1/P2

    public int PlayerNumber => playerNumber;
    
    void Awake()
    {
        Debug.Log($"[PongPlayer {playerNumber}] ===== AWAKE CALLED =====");
        isHorizontalPaddle = (playerNumber == 3 || playerNumber == 4);
        Debug.Log($"[PongPlayer {playerNumber}] Is horizontal paddle: {isHorizontalPaddle}");

        SetupInput();
    }
    
    private void SetupInput()
    {
        Debug.Log($"[PongPlayer {playerNumber}] ===== SETUP INPUT CALLED =====");
        
        if (inputActions == null)
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] No InputActionAsset assigned, motion tracking disabled");
            return;
        }

        Debug.Log($"[PongPlayer {playerNumber}] InputActionAsset assigned: {inputActions.name}");

        // find this player's specific device
        FindMyDevice();
        
        if (myDevice == null && !keyboardInput)
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] Could not find CapturyInput device for Player{playerNumber}. Will retry each frame.");
            return;
        }

        // create an instance of the InputActionAsset for this player
        instancedActions = Instantiate(inputActions);
        Debug.Log($"[PongPlayer {playerNumber}] Created instanced InputActionAsset");
        
        // get action map from instanced asset
        var actionMap = instancedActions.FindActionMap(actionMapName);
        if (actionMap == null)
        {
            Debug.LogError($"[PongPlayer {playerNumber}] Action map '{actionMapName}' not found!");
            Debug.Log($"[PongPlayer {playerNumber}] Available action maps:");
            foreach (var map in instancedActions.actionMaps)
            {
                Debug.Log($"  - {map.name}");
            }
            return;
        }
        
        Debug.Log($"[PongPlayer {playerNumber}] Found action map: {actionMapName}");
        
        // get head position action
        headPositionAction = actionMap.FindAction("headPosition");
        if (headPositionAction == null)
        {
            Debug.LogError($"[PongPlayer {playerNumber}] 'headPosition' action not found in map '{actionMapName}'!");
            Debug.Log($"[PongPlayer {playerNumber}] Available actions in '{actionMapName}':");
            foreach (var action in actionMap.actions)
            {
                Debug.Log($"  - {action.name}");
            }
            return;
        }

        Debug.Log($"[PongPlayer {playerNumber}] Found headPosition action");
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
                
                // check if this device has the usage we're looking for
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
                    Debug.Log($"[PongPlayer {playerNumber}]   ✓✓✓ THIS IS MY DEVICE! ✓✓✓");
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
        Debug.Log($"[PongPlayer {playerNumber}] OnEnable called");
        if (headPositionAction != null)
        {
            headPositionAction.Enable();
            Debug.Log($"[PongPlayer {playerNumber}] headPositionAction enabled");
        }
        else
        {
            Debug.LogWarning($"[PongPlayer {playerNumber}] headPositionAction is NULL in OnEnable!");
        }
    }
    
    void OnDisable()
    {
        Debug.Log($"[PongPlayer {playerNumber}] OnDisable called");
        headPositionAction?.Disable();
    }
    
    void OnDestroy()
    {
        Debug.Log($"[PongPlayer {playerNumber}] OnDestroy called");
        if (instancedActions != null)
        {
            instancedActions.Disable();
            Destroy(instancedActions);
        }
    }
    
    void Update()
    {
        // if we haven't found device yet, try again
        if (myDevice == null && !keyboardInput)
        {
            if (Time.frameCount % 60 == 0)  // every 60 frames
            {
                FindMyDevice();
            }
        }
        
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

            // directly from the device control instead of using InputAction
            Vector3 headPos = myDevice.headPosition.ReadValue();
            
            // map head Z position directly to paddle position
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

        // apply position based on paddle orientation
        Vector3 oldPosition = transform.position;
        if (isHorizontalPaddle)
        {
            // T/B move horizontally
            transform.position = new Vector3(clampedPosition, transform.position.y, transform.position.z);
        }
        else
        {
            // L/R move vertically
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
        
        // re-find device with new player number
        myDevice = null;
        FindMyDevice();
    }   
}