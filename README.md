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

Add these components to a GameObject in your scene:

| Component | Source | Purpose |
|-----------|--------|---------|
| `CapturyNetworkPlugin` | Captury Plugin | Connects to CapturyLive |
| `CapturyInputManager` | This Package | Registers input device |
| `MotionTrackingManager` | This Package | Main tracking manager |

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

Drag your configuration asset to the **Config** field on `MotionTrackingManager`.

### 5. Access Tracking Data

There are two ways to access tracking data. You can access it directly using the input device, or by using the provided Input Action Asset. 

#### Calling Directly

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackingExample : MonoBehaviour
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

    private void OnDisable()
    {
        weightShiftXAction.Disable();
        footRaiseAction.Disable();
        footLowerAction.Disable();
    }

    private void OnFootRaise(InputAction.CallbackContext ctx)
    {
        // this is where you put logic for what happens when you raise your foot
        Debug.Log("Foot raise detected!");
    }

    private void OnFootLower(InputAction.CallbackContext ctx)
    {
        // this is where you put logic for what happens when you raise your foot
        Debug.Log("Foot lower detected!");
    }

    void Update()
    {
        // read the value of the action and save it to a float
        float weightShift = weightShiftXAction.ReadValue<float>();

        // now you can do whatever you want with that number!
        Debug.Log($"Weight Shift X Value: {weightShift}");
    }
}
```
---

## Configuration Options

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

Configure joint names in your configuration asset to match your skeleton. You can upload your own skeleton, but by default it assumes the names of the model that comes with the Captury plugin. 

| Module | Joint | Default Name |
|--------|-------|--------------|
| Torso | Pelvis | `Hips` |
| Torso | Top of Spine | `Spine4` |
| Head | Head | `Head` |
| Arms | Left Shoulder | `LeftShoulder` |
| Arms | Right Shoulder | `RightShoulder` |
| Arms | Left Hand | `LeftHand` |
| Arms | Right Hand | `RightHand` |
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

### 1.1.0
- Added balance tracking
- Fixed a bug with whole body movement
