# Captury Motion Tracking Toolkit

Unity package for motion capture tracking using Captury and Unity's Input System. Provides modular tracking for torso, feet, arms, and head with walk detection and gait analysis and balance tracking.

---

## Features

- **Modular Design** - Enable/disable tracking modules independently
- **Input System Integration** - Access tracking data through Unity's Input System
- **Torso Tracking** - Weight shift detection, bent over detection
- **Foot Tracking** - Foot raise, hip abduction, position tracking
- **Walk Detection** - Speed, cadence, walk state (idle/walking/stopping)
- **Gait Analysis** - Step timing, asymmetry, consistency metrics
- **Arm Tracking** - Hand position and raise detection
- **Head Tracking** - Position, rotation, nod/shake gesture detection
- **Balance Tracking** - Center of mass position and velocity, lateral sway, anterior/posterior sway
- **Configurable** - ScriptableObject-based configuration system
- **Multiplayer Support** - Supports multiple captury skeletons with instanced input action assets

---

## Installation

### Prerequisites

This package includes the **Captury Unity Plugin** (MIT License) in `/Runtime/ThirdParty/Captury/` and the **Unity Input System**. 

### Install via Package Manager

1. Open Unity Package Manager: `Window → Package Manager`
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/hbjeletich/CapturyUnityToolkit.git`

### Install via manifest.json

Add this line to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.hbjeletich.capturytoolkit": "https://github.com/hbjeletich/CapturyUnityToolkit.git"
  }
}
```

---

## Quick Start

### 1. Scene Setup

Use the BaseScene for fastest setup for singleplayer. 

For existing scenes or starting from scratch, add these components to a GameObject in your scene:

| Component | Source | Purpose |
|-----------|--------|---------|
| `CapturyNetworkPlugin` | Captury Plugin | Connects to CapturyLive |
| `CapturyInputManager` | This Package | Registers input device |
| `MotionTrackingManager` | This Package | Main tracking manager |

If you're doing multiplayer, the components are slightly different:

| Component | Source | Purpose |
|-----------|--------|---------|
| `CapturyNetworkPlugin` | Captury Plugin | Connects to CapturyLive |
| `MultiplayerTrackingManager` | This Package | Main tracking manager, handles input device registration |



### 2. Configure Captury Connection

On the `CapturyNetworkPlugin` component:
- Set **Host** to the IP address where CapturyLive is running
- Set **Port** to `2101` (default)
- Assign your **Streamed Skeleton** and **Streamed Avatar**

### 3. Create a Configuration Asset

1. Right-click in Project window
2. Select `Create → Motion Tracking → Configuration`
3. Enable the modules you need:
   - Torso Module
   - Foot Module
   - Arms Module
   - Head Module
   - Balance Module

### 4. Assign Configuration

Drag your configuration asset to the **Config** field on `MotionTrackingManager` or `MultiplayerTrackingManager`.

### 5. Access Tracking Data -- Singleplayer

To directly find input actions from the input device:

There are two ways to access tracking data. You can access it directly using the input device, or by using the provided Input Action Asset. 

#### Calling Directly

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class DirectInputTrackingExample : MonoBehaviour
{
    void Update()
    {
        // find captury input device
        var captury = InputSystem.GetDevice<CapturyInput>();
        
        if (captury != null)
        {
            // check if walking
            if (captury.isWalking.isPressed)
            {
                // read the value of our speed
                float speed = captury.walkSpeed.ReadValue();
                Debug.Log($"Walking at {speed} m/s");
            }
            
            // check weight shift
            if (captury.weightShiftLeft.isPressed)
            {
                Debug.Log("Weight shifted left");
            }
            
            // get foot position
            Vector3 leftFoot = captury.leftFootPosition.ReadValue();

            // do whatever you want with these numbers!
            // for now, print the x, y, and z separately
            Debug.Log($"Left foot X: {leftFoot.x}");
            Debug.Log($"Left foot Y: {leftFoot.y}");
            Debug.Log($"Left foot Z: {leftFoot.z}");
        }
    }
}
```
#### Using Input Action Asset

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackingExample : MonoBehaviour
{
    public InputActionAsset inputActions;

    private InputAction weightShiftXAction;
    private InputAction footRaisedAction;
    private InputAction footLoweredAction;

    void Awake()
    {
        // make sure this is AWAKE and not start!
        // find the maps within the action asset
        var torsoMap = inputActions.FindActionMap("Torso");
        var footMap = inputActions.FindActionMap("Foot");

        // find the specific actions
        weightShiftXAction = torsoMap.FindAction("WeightShiftX");
        footRaisedAction = footMap.FindAction("FootRaised");
        footLowerAction = actionMap.FindAction("FootLowered");

        // subscribe foot actions to methods
        footRaiseAction.performed += OnFootRaise;
        footLowerAction.performed += OnFootLower;
    }

    private void OnEnable()
    {
        weightShiftXAction.Enable();
        footRaiseAction.Enable();
        footLowerAction.Enable();
    }

You can also use an `InputActionAsset`, or the MotionTracking one created for you already:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionAssetTrackingExample : MonoBehaviour
{
    // assign Input Action Asset in the Inspector
    public InputActionAsset inputActions;

    private InputAction isWalkingAction;
    private InputAction walkSpeedAction;
    private InputAction weightShiftLeftAction;

    void Awake()
    {
        // must be in AWAKE!
        // assuming you're using the given MotionTracking asset, action maps are separated by module
        var footMap = inputActions.FindActionMap("Foot");
        var torsoMap = inputActions.FindActionMap("Torso");

        // find specific actions
        isWalkingAction = footMap.FindAction("IsWalking");
        walkSpeedAction = footMap.FindAction("WalkSpeed");
        weightShiftLeftAction = torsoMap.FindAction("WeightShiftLeft");
    }

    void OnEnable()
    {
        // enable the actions
        isWalkingAction.Enable();
        walkSpeedAction.Enable();
        weightShiftLeftAction.Enable();

        isWalkingAction.performed += OnWalk;
        weightShiftLeftAction.performed += OnWeightShiftLeft;
    }

    void OnEnable()
    {
        // disable the actions
        isWalkingAction.Disable();
        walkSpeedAction.Disable();
        weightShiftLeftAction.Disable();

        isWalkingAction.performed -= OnWalk;
        weightShiftLeftAction.performed -= OnWeightShiftLeft;
    }

    void OnWalk(InputAction.CallbackContext ctx)
    {
        float walkSpeed = walkSpeedAction.ReadValue<float>();
        Debug.Log($"Walking at {speed} m/s");
    }

    void OnWeightShift(InputAction.CallbackContext ctx)
    {
        Debug.Log("Weight shifted left");
    }
}
```

Though the setup for this is a little longer, it is a much better, more modular way to call the actions, especially if you have more complex control schemes. 

### 6. Access Tracking Data -- Multiplayer

For multiplayer scenarios, the recommended approach is **direct device access**. Each player script finds and stores a reference to its specific CapturyInput device based on the device's usage tag (e.g., `Player1`, `Player2`).

This approach is simpler and more reliable than binding overrides, as each player maintains a direct reference to their own device and reads from it directly.
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiplayerTrackingExample : MonoBehaviour
{
    public int playerNumber;
    private CapturyInput myDevice;
    
    void Start()
    {
        FindMyDevice();
    }
    
    private void FindMyDevice()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is CapturyInput capturyDevice)
            {
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
                    Debug.Log($"Player {playerNumber}: Found my device - {myDevice.name}");
                    return;
                }
            }
        }
        
        Debug.LogWarning($"Player {playerNumber}: Could not find device with usage 'Player{playerNumber}'");
    }
    
    void Update()
    {
        // if device not found yet, retry periodically
        if (myDevice == null)
        {
            if (Time.frameCount % 60 == 0)
            {
                FindMyDevice();
            }
            return;
        }
        
        // read values directly from the device
        bool isWalking = myDevice.isWalking.isPressed;
        float walkSpeed = myDevice.walkSpeed.ReadValue();
        
        // check for button state changes
        if (myDevice.weightShiftLeft.wasPressedThisFrame)
        {
            Debug.Log($"Player {playerNumber}: Weight shifted left");
        }
        
        if (myDevice.weightShiftRight.wasPressedThisFrame)
        {
            Debug.Log($"Player {playerNumber}: Weight shifted right");
        }
        
        // check continuous values
        if (isWalking && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Player {playerNumber}: Walking at {walkSpeed} m/s");
        }
    }
}
```

**How it works:**
1. Each player script searches for a CapturyInput device with the matching `Player{N}` usage
2. Once found, the script stores a direct reference to that specific device
3. All tracking data is read directly from the device using `.ReadValue()` or `.isPressed`
4. Button-like controls (e.g., `weightShiftLeft`) support state checks like `wasPressedThisFrame`
5. If the device isn't found immediately (e.g., skeleton spawns late), the script retries every 60 frames

**Alternative Approach - InputAction Binding Overrides:**

If you prefer to use InputActions, you can instance your InputActionAsset and apply binding overrides. However, this approach can be more complex:
```csharp
// in Awake():
instancedActions = Instantiate(inputActions);
var footMap = instancedActions.FindActionMap("Foot");
isWalkingAction = footMap.FindAction("IsWalking");

// override binding to specific player's device
// format: <DeviceType>/{Usage}/controlPath
isWalkingAction.ApplyBindingOverride($"<CapturyInput>/{{Player{playerNumber}}}/isWalking");
```

For most use cases, **direct device access is recommended** as it's more straightforward and avoids potential binding resolution issues in multiplayer scenarios.

---

## Configuration Options

### Motion Tracking Manager
- **Modular System** - Use what you need, don't use what you don't
- **Configuration Scriptables** - Create your own configurations and swap between them in editor or during runtime
- **Calibration Setup** - Automatically or manually calibrate your modules, setting your own delays and calling the calibrate method when necessary

### Multiplayer Manager
- **Maximum Players** - Set a number of max players accepted
- **Calibration Preferences** - Decide if calibration will happen automatically, and the delay (in seconds) between calibrating skeletons

### Torso Module
- **Weight Shift Threshold** - Distance required to trigger weight shift detection
- **Neutral Zone Width** - Size of neutral zone to prevent flutter
- **Bent Over Angle** - Forward bend angle to trigger detection
- **Whole Body Movement Threshold** - Ratio to ignore coordinated movement

### Foot Module
- **Foot Raise Threshold** - Height difference to trigger foot raise
- **Hip Abduction Distance** - Additional spread distance for abduction
- **Min Lift Height** - Minimum height for valid movements
- **Position Tracking** - Relative or absolute positioning

### Walk Tracking
- **Walk Speed Threshold** - Minimum speed to start walk detection
- **Minimum Walk Duration** - Time required to confirm walking
- **Walk Stop Threshold** - Speed below which walking stops

### Gait Analysis
- **Minimum Cycles** - Required complete cycles for reliable analysis
- **Step Time Range** - Min/max reasonable step times (filters outliers)
- **Consistency Calculation** - Based on step time variance

### Arms/Hands Module
- **Hand Raise Threshold** - Height above shoulder to trigger
- **Min Height Gain** - Minimum lift from neutral position

### Head Module
- **Nod/Shake Thresholds** - Rotation angles for gesture detection
- **Gesture Speed** - Time window for gesture completion
- **Gesture Timeout** - Maximum active duration

### Balance Module
- **Sway and Stability Thresholds** - Max stability in m/s
- **Center of Mass Frame History** - Frames of CoM history to keep

---

## Joint Name Configuration

Configure joint names in your configuration asset to match your skeleton. You can upload your own skeleton, but by default it assumes the names of the model that comes with the Captury plugin. Note that **each joint can only be accessed by one module at a time**.

| Module | Joint | Default Name |
|--------|-------|--------------|
| Torso | Pelvis | `Hips` |
| Balance | Bottom of Spine | `Spine1` |
| Torso | Top of Spine | `Spine4` |
| Head | Head | `Head` |
| Arms | Left Shoulder | `LeftShoulder` |
| Arms | Right Shoulder | `RightShoulder` |
| Balance | Left Forearm | `LeftForearm` |
| Balance | Right Forearm | `RightForearm` |
| Arms | Left Hand | `LeftHand` |
| Arms | Right Hand | `RightHand` |
| Balance | Left Leg | `LeftLeg` |
| Balance | Right Leg | `RightLeg` |
| Feet | Left Foot | `LeftFoot` |
| Feet | Right Foot | `RightFoot` |
| Balance | Bottom of Spine | `Spine1` |
| Balance | Left Forearm | `LeftForeArm` |
| Balance | Right Forearm | `RightForeArm` |
| Balance | Left Leg | `LeftLeg` |
| Balance | Right Leg | `RightLeg` |
| Balance | Left Toe Base | `LeftToeBase` |
| Balance | Right Toe Base | `RightToeBase` |

---

## Requirements

- Unity 2020.3 or later
- Unity Input System 1.4.0 or later
- Captury Unity Plugin (included)

---

### Third-Party Licenses

This package includes the **Captury Unity Plugin**:
- Copyright © 2017 thecaptury
- Licensed under MIT License
- See `Runtime/ThirdParty/Captury/LICENSE.txt`

---

## Version History

### 1.0.0
- Initial release
- Torso, foot, arm, and head tracking modules
- Walk detection and gait analysis
- Input System integration

### 1.1.0 & 1.1.1
- Added balance tracking support
- Fixed foot tracking relative position bug

### 1.2.0
- Added Multiplayer support (multiple skeletons tracked by the same system)
