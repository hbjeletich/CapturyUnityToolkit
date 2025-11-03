using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// Interface for motion tracking managers.
/// Allows modules to work with both single-player and multiplayer managers.
public interface IMotionTrackingManager
{
    MotionTrackingConfiguration Config { get; }
    Transform GetJointByName(string jointName);
}
