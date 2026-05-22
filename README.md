# Captury Motion Tracking Toolkit

Unity package for motion capture tracking using Captury and Unity's Input System. Provides modular tracking for torso, feet, arms, head, and balance with walk detection and gait analysis.

---

## Features

- **Modular Design** — Enable/disable tracking modules independently
- **Input System Integration** — Access tracking data through Unity's Input System
- **Torso Tracking** — Weight shift detection, bent over detection
- **Foot Tracking** — Foot raise, hip abduction, position tracking
- **Walk Detection** — Speed, cadence, walk state (idle/walking/stopping)
- **Gait Analysis** — Step timing, asymmetry, consistency metrics
- **Arm Tracking** — Hand position and raise detection
- **Head Tracking** — Position, rotation, nod/shake gesture detection
- **Balance Tracking** — Center of mass, lateral sway, anterior/posterior sway
- **Per-Module Configuration** — Each module has its own ScriptableObject-based config with a custom inspector
- **Calibration Snapshots** — Save/restore calibration states; revert to a previous calibration at any time
- **Multiplayer Support** — Multiple Captury skeletons tracked simultaneously with instanced input devices
- **Sample Scenes** — Includes a 2-player Pong game and a runtime UI debug panel

---

## Installation

### Prerequisites

This package includes the **Captury Unity Plugin** (MIT License) in `/Runtime/ThirdParty/Captury/` and requires the **Unity Input System**.

You can find Captury's Unity tutorial on Youtube [here.](https://www.youtube.com/watch?v=06uo_TKcK9I)

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

### 1. Scene Setup — Singleplayer

Add these components to a GameObject in your scene:

| Component | Source | Purpose |
| --- | --- | --- |
| `CapturyNetworkPlugin` | Captury Plugin | Connects to CapturyLive |
| `CapturyInputManager` | This Package | Registers the `CapturyInput` device with the Input System |
| `MotionTrackingManager` | This Package | Manages tracking modules, calibration, and input state |

### Scene Setup — Multiplayer

| Component | Source | Purpose |
| --- | --- | --- |
| `CapturyNetworkPlugin` | Captury Plugin | Connects to CapturyLive |
| `MultiplayerTrackingManager` | This Package | Manages per-skeleton input devices and tracking |

### 2. Configure Captury Connection

On the `CapturyNetworkPlugin` component:

- Set **Host** to the IP address where CapturyLive is running
- Set **Port** to `2101` (default)
- Assign your **Streamed Skeleton** and **Streamed Avatar**

### 3. Create a Configuration Asset

1. Right-click in the Project window
2. Select `Create → Motion Tracking → Configuration`
3. Add the modules you need (Torso, Foot, Arms, Head, Balance) — each module has its own configuration section with independent settings
4. Drag your configuration asset to the **Config** field on `MotionTrackingManager` or `MultiplayerTrackingManager`

---

## Accessing Tracking Data — Singleplayer

The simplest way to access tracking data in singleplayer is with Unity's built-in `PlayerInput` component. This handles device pairing and action callbacks automatically.

### Using PlayerInput (Recommended)

1. Add a `PlayerInput` component to your GameObject
2. Assign the included `CapturyInputActions` asset to the **Actions** field
3. Set **Behavior** to your preferred mode (e.g., `Invoke Unity Events` or `Send Messages`)
4. Wire up callbacks in the Inspector or in code

For example, with `Send Messages`, Unity will call methods on your MonoBehaviour matching the action names:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackingExample : MonoBehaviour
{
    // called automatically by PlayerInput when the FootRaised action fires
    void OnFootRaised(InputValue value)
    {
        Debug.Log("A foot was raised!");
    }

    // called automatically for the WeightShiftX axis value
    void OnWeightShiftX(InputValue value)
    {
        float shift = value.Get<float>();
        Debug.Log($"Weight shift: {shift}");
    }

    // called automatically when IsWalking is pressed
    void OnIsWalking(InputValue value)
    {
        Debug.Log("Player is walking!");
    }
}
```

With `Invoke Unity Events`, you can wire actions to methods directly in the Inspector — no code needed for the bindings themselves.

### Reading the Device Directly

You can also read from the `CapturyInput` device directly if you prefer:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class DirectInputExample : MonoBehaviour
{
    void Update()
    {
        var captury = InputSystem.GetDevice<CapturyInput>();
        if (captury == null) return;

        if (captury.isWalking.isPressed)
        {
            float speed = captury.walkSpeed.ReadValue();
            Debug.Log($"Walking at {speed} m/s");
        }

        if (captury.weightShiftLeft.isPressed)
            Debug.Log("Weight shifted left");

        Vector3 leftFoot = captury.leftFootPosition.ReadValue();
        Debug.Log($"Left foot: {leftFoot}");
    }
}
```

### Reading the Input Actions Map

The toolkit also comes with an input actions map already hooked up to the input device. 

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionMapExample : MonoBehaviour
{
    // assign the input action asset in the inspector
    public InputActionAsset capturyInputActions;

    // variables for the input actions themselves
    private InputAction footRaiseAction;
    private InputAction weightShiftXAction;
    private InputAction walkingAction;

    void Awake()
    {
        // each module has its own map within the input action asset
        var footMap = capturyInputActions.FindActionMap("Foot");
        var torsoMap = capturyInputActions.FindActionMap("Torso");

        // reference the actions through their input map
        footRaiseAction = footMap.FindAction("FootRaised");
        weightShiftXAction = torsoMap.FindAction("WeightShiftX");
        walkingAction = footMap.FindAction("IsWalking");

        // for boolean-based actions, you can add a listener
        footRaiseAction.performed += OnFootRaise;
        walkingAction.performed += OnWalking;
    }

    void OnEnable()
    {
        // ensure each action is enabled
        footRaiseAction.Enable();
        weightShiftXAction.Enable();
        walkingAction.Enable();
    }

    void OnDisable()
    {
        // ensure each action is disabled
        footRaiseAction.Disable();
        weightShiftXAction.Disable();
        walkingAction.Disable();
    }

    void Update()
    {
        // read the float-based action
        float weightShift = weightShiftXAction.ReadValue<float>();
        Debug.Log($"Weight Shift X: {weightShift}");
    }

    private void OnFootRaise(InputAction.CallbackContext ctx)
    {
        Debug.Log("Foot was raised!");
    }

    private void OnWalking(InputAction.CallbackContext ctx)
    {
        Debug.Log("Walking state changed!");
    }
}
```

---

## Accessing Tracking Data — Multiplayer

For multiplayer, each player script finds its specific `CapturyInput` device by its usage tag (`Player1`, `Player2`, etc.) and reads from it directly:

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
                foreach (var usage in device.usages)
                {
                    if (usage == $"Player{playerNumber}")
                    {
                        myDevice = capturyDevice;
                        Debug.Log($"Player {playerNumber}: Found device");
                        return;
                    }
                }
            }
        }

        Debug.LogWarning($"Player {playerNumber}: Device not found yet");
    }

    void Update()
    {
        if (myDevice == null)
        {
            // retry periodically — skeleton may not have spawned yet
            if (Time.frameCount % 60 == 0) FindMyDevice();
            return;
        }

        bool isWalking = myDevice.isWalking.isPressed;
        float walkSpeed = myDevice.walkSpeed.ReadValue();

        if (myDevice.weightShiftLeft.wasPressedThisFrame)
            Debug.Log($"Player {playerNumber}: Weight shifted left");

        if (isWalking && Time.frameCount % 60 == 0)
            Debug.Log($"Player {playerNumber}: Walking at {walkSpeed} m/s");
    }
}
```

**How it works:** The `MultiplayerTrackingManager` creates a separate `CapturyInput` device for each detected skeleton and tags it with a `Player{N}` usage. Each player script searches for the device matching its player number and reads from it directly.

**Alternative — InputAction Binding Overrides:** If you prefer `InputActions`, you can instance the asset and apply binding overrides per player:

```csharp
// in Awake():
instancedActions = Instantiate(inputActions);
var footMap = instancedActions.FindActionMap("Foot");
isWalkingAction = footMap.FindAction("IsWalking");
isWalkingAction.ApplyBindingOverride($"<CapturyInput>/{{Player{playerNumber}}}/isWalking");
```

For most cases, direct device access is simpler and avoids binding resolution issues.

---

## Configuration

### Module Configuration System

Each tracking module has its own `ModuleConfiguration` ScriptableObject. The main `MotionTrackingConfiguration` asset holds a list of these, and a custom inspector lets you add, remove, and configure modules individually.

To create a custom module, extend `ModuleConfiguration` (for settings) and `MotionTrackingModule` (for tracking logic), then add your config to the configuration asset's module list.

### MotionTrackingManager

- **Configuration** — Assign a `MotionTrackingConfiguration` asset; swap configurations at runtime with `SwapConfiguration()`
- **Calibration** — Automatic on skeleton detection, or call `Recalibrate()` manually; revert to a previous calibration with `LoadPreviousCalibration()`
- **Module Access** — `GetModule<T>()` for any module type, or use built-in getters like `GetTorsoModule()`

### MultiplayerTrackingManager

- **Max Players** — Set the maximum number of tracked skeletons
- **Automatic Calibration** — Toggle auto-calibration on skeleton detection
- **Calibration Delay Per Skeleton** — Seconds between calibrating each new skeleton

### Torso Module

- **Weight Shift Threshold** — Distance to trigger weight shift detection
- **Neutral Zone Width** — Dead zone to prevent flutter
- **Bent Over Angle** — Forward bend angle threshold
- **Whole Body Movement Threshold** — Ratio to ignore coordinated movement

### Foot Module

- **Foot Raise Threshold** — Height difference to detect a foot raise
- **Hip Abduction Distance** — Spread distance for abduction detection
- **Min Lift Height** — Minimum height for valid movements
- **Position Tracking** — Relative or absolute positioning
- **Walk Speed Threshold / Min Walk Duration / Walk Stop Threshold** — Walk detection tuning
- **Gait Analysis** — Step time range, minimum cycles, consistency calculation

### Arms Module

- **Hand Raise Threshold** — Height above shoulder to trigger
- **Min Height Gain** — Minimum lift from neutral position

### Head Module

- **Nod/Shake Thresholds** — Rotation angles for gesture detection
- **Gesture Speed** — Time window for gesture completion
- **Gesture Timeout** — Maximum active duration

### Balance Module

- **Sway and Stability Thresholds** — Max stability in m/s
- **Center of Mass Frame History** — Frames of CoM history to retain

---

## Available Input Controls

All controls are available on the `CapturyInput` device and in the `CapturyInputActions` asset, organized by action map.

| Action Map | Control | Type | Description |
| --- | --- | --- | --- |
| **Torso** | `IsBentOver` | Button | Player is bent forward |
| | `IsUpright` | Button | Player is standing upright |
| | `WeightShiftLeft` | Button | Weight shifted left |
| | `WeightShiftRight` | Button | Weight shifted right |
| | `WeightShiftX` | Axis | Continuous weight shift value |
| | `PelvisPosition` | Vector3 | Pelvis world position |
| **Foot** | `FootRaised` | Button | A foot was raised |
| | `FootLowered` | Button | A foot was lowered |
| | `LeftFootPosition` | Vector3 | Left foot position |
| | `RightFootPosition` | Vector3 | Right foot position |
| | `LeftHipAbducted` | Button | Left hip abduction detected |
| | `RightHipAbducted` | Button | Right hip abduction detected |
| | `IsWalking` | Button | Player is walking |
| | `WalkStarted` | Button | Walk just began |
| | `WalkStopped` | Button | Walk just ended |
| | `WalkSpeed` | Axis | Current walk speed (m/s) |
| | `Cadence` | Axis | Steps per minute |
| | `LeftStep` / `RightStep` | Button | Individual step events |
| | `LeftStepTime` / `RightStepTime` | Axis | Time per step |
| | `StepTimeAsymmetry` | Axis | Left/right step time difference |
| | `GaitConsistency` | Axis | Step timing consistency |
| **Arms** | `LeftHandPosition` | Vector3 | Left hand position |
| | `RightHandPosition` | Vector3 | Right hand position |
| | `LeftHandRaised` | Button | Left hand raised above shoulder |
| | `RightHandRaised` | Button | Right hand raised above shoulder |
| **Head** | `HeadPosition` | Vector3 | Head position |
| | `HeadRotation` | Vector3 | Head rotation (euler) |
| | `HeadNodding` | Button | Nod gesture detected |
| | `HeadShaking` | Button | Shake gesture detected |
| **Balance** | `CenterOfMassPosition` | Vector3 | Center of mass position |
| | `LateralSway` | Axis | Side-to-side sway |
| | `AnteriorPosteriorSway` | Axis | Front-to-back sway |
| | `SwayMagnitude` | Axis | Overall sway magnitude |
| | `IsSwaying` | Button | Sway exceeds threshold |
| | `CoMVelocity` | Axis | Center of mass velocity |
| | `IsBalanced` | Button | Player is balanced |
| | `BalanceLost` | Button | Balance was just lost |
| | `BalanceRegained` | Button | Balance was just regained |

---

## Joint Name Configuration

Configure joint names in your configuration asset to match your skeleton. By default, joint names match the model that ships with the Captury plugin. Each joint can only be accessed by one module at a time.

| Module | Joint | Default Name |
| --- | --- | --- |
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

## Samples

Import samples from the Package Manager (`Window → Package Manager → Captury Motion Tracking Toolkit → Samples`).

### 2D Example Scene (Pong)

A 2-player Pong game controlled entirely through motion tracking. Demonstrates multiplayer device pairing, walk-based paddle movement, and visual effects (anaglyph 3D text, ball trails, paddle/border hit effects, shader graph glow).

### UI Scene

A runtime debug UI that displays real-time tracking data for every active module. Useful for verifying your setup, tuning configuration values, and debugging skeleton data.

---

## Requirements

- Unity 2020.3 or later
- Unity Input System 1.4.0 or later
- Captury Unity Plugin (included)
- TextMeshPro (for sample scenes)
- URP (for 2D Example Scene shader effects)

---

## Third-Party Licenses

This package includes the **Captury Unity Plugin**:

- Copyright © 2017 thecaptury
- Licensed under MIT License
- See `Runtime/ThirdParty/Captury/LICENSE.txt`

---

## Version History

### 1.3.0 (Latest)

- **Separated per-module configurations** — Each tracking module now has its own `ModuleConfiguration` ScriptableObject instead of a single monolithic config
- **Extended base `MotionTrackingModule` class** — Common calibration, joint resolution, and configuration access handled by the base class; custom modules only need to implement `GetModuleConfig()`, `CaptureCalibration()`, and `UpdateTracking()`
- **Calibration snapshots** — Save and restore calibration states; revert to a previous calibration with `LoadPreviousCalibration()`
- **Custom inspector** for `MotionTrackingConfiguration` — Add/remove/configure modules directly in the editor
- **Samples reorganized** — Pong and UI Scene moved to `Samples/` for proper UPM sample import
- **New UI Scene sample** — Runtime debug panel showing live tracking data for all modules
- **Pong example improvements** — Anaglyph 3D text, ball trails, paddle/border hit effects, shader graph glow, improved game management
- **MIT License** added to the project root
- **Code cleanup** across all modules

### 1.2.0

- Added multiplayer support (multiple skeletons tracked by the same system)
- `MultiplayerMotionTrackingManager` for per-skeleton device creation
- 2D Pong example game

### 1.1.0 & 1.1.1

- Added balance tracking module (center of mass, base of support, sway detection)
- Fixed foot tracking relative position bug

### 1.0.0

- Initial release
- Torso, foot, arm, and head tracking modules
- Walk detection and gait analysis
- Input System integration
