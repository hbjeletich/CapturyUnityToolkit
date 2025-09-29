# Captury Motion Tracking Toolkit

Unity package for motion capture tracking using Captury and Unity's Input System. Provides modular tracking for torso, feet, arms, and head with walk detection and gait analysis.

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
    "com.yourname.motiontracking": "https://github.com/hbjeletich/CapturyUnityToolkit.git"
  }
}
```

---

## 🚀 Quick Start

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

### 4. Assign Configuration

Drag your configuration asset to the **Config** field on `MotionTrackingManager`.

### 5. Access Tracking Data

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class TrackingExample : MonoBehaviour
{
    void Update()
    {
        var captury = InputSystem.GetDevice<CapturyInput>();
        
        if (captury != null)
        {
            // Check if walking
            if (captury.isWalking.isPressed)
            {
                float speed = captury.walkSpeed.ReadValue();
                Debug.Log($"Walking at {speed} m/s");
            }
            
            // Check weight shift
            if (captury.weightShiftLeft.isPressed)
            {
                Debug.Log("Weight shifted left");
            }
            
            // Get foot position
            Vector3 leftFoot = captury.leftFootPosition.ReadValue();
        }
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