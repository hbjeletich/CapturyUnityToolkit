using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionConfig", menuName = "Motion Tracking/Configuration")]
public class MotionTrackingConfiguration : ScriptableObject
{
    [Header("Configuration Info")]
    public string configurationName = "Default";
    [TextArea(2, 4)]
    public string description = "Default motion tracking setup for basic foot and torso tracking with optional walk analysis.";

    [Header("System Settings")]
    public float calibrationDelay = 2.0f;
    [Tooltip("Number of frames to average during calibration")]
    public int calibrationFrames = 30;

    [Header("Torso Module")]
    public bool enableTorsoModule = true;
    [Range(0.1f, 3.0f)]
    public float torsoSensitivity = 1.0f;
    public bool torsoDebugMode = false;

    [Space(5)]
    public bool isShiftTracked = true;
    [Tooltip("How far center of mass must shift to trigger weight shift")]
    public float weightShiftThreshold = 0.15f;
    [Tooltip("Width of neutral zone to prevent fluttering")]
    public float neutralZoneWidth = 0.05f;
    [Tooltip("Ratio threshold to ignore whole-body movement vs torso-only movement")]
    public float wholeBodyMovementThreshold = 0.8f;

    [Space(5)]
    public bool isBendTracked = true;
    [Tooltip("Degrees of forward bend to trigger bent over detection")]
    public float bentOverAngleThreshold = 30f;

    [Header("Foot/Leg Module")]
    public bool enableFootModule = true;
    [Range(0.1f, 3.0f)]
    public float footSensitivity = 1.0f;
    public bool footDebugMode = false;

    [Space(5)]
    public bool isFootRaiseTracked = true;
    [Tooltip("Minimum height difference between feet to trigger foot raise")]
    public float footRaiseThreshold = 0.1f;

    [Space(5)]
    public bool isHipAbductionTracked = true;
    [Tooltip("Additional distance feet must spread beyond normal stance")]
    public float minAbductionDistance = 0.2f;
    [Tooltip("Minimum foot lift height required for abduction detection")]
    public float minLiftHeight = 0.05f;

    [Space(5)]
    public bool isFootPositionTracked = true;
    [Tooltip("Use relative positions from calibration vs absolute world positions")]
    public bool useRelativeFootPosition = true;

    [Header("Walk Detection")]
    [Space(5)]
    public bool enableWalkTracking = false;
    [Tooltip("Minimum movement speed (m/s) to consider walking")]
    public float walkSpeedThreshold = 0.3f;
    [Tooltip("How long movement must continue to confirm walking")]
    public float minimumWalkDuration = 2.0f;
    [Tooltip("Speed below which walking stops")]
    public float walkStopThreshold = 0.1f;

    [Header("Gait Analysis")]
    [Space(5)]
    public bool enableGaitAnalysis = false;
    [Tooltip("Need this many complete cycles before analysis is reliable")]
    public int minimumCyclesForAnalysis = 3;
    [Tooltip("Filter out unrealistic step times - maximum")]
    public float maxReasonableStepTime = 2.0f;
    [Tooltip("Filter out unrealistic step times - minimum")]
    public float minReasonableStepTime = 0.3f;
    [Tooltip("Frames of position history to keep (300 = ~5 seconds at 60fps)")]
    public int positionHistoryFrames = 300;
    [Tooltip("Number of recent foot events to remember")]
    public int eventHistoryCount = 20;
    
    [Header("Arms/Hands Module")]
    public bool enableArmsModule = false;
    [Range(0.1f, 3.0f)]
    public float armsSensitivity = 1.0f;
    public bool armsDebugMode = false;
    [Space(5)]
    public bool isHandPositionTracked = true;
    [Tooltip("Use relative positions from calibration vs absolute world positions")]
    public bool useRelativeHandPosition = true;
    [Space(5)]
    public bool isHandRaiseTracked = true;
    [Tooltip("Height above shoulder required to trigger hand raised")]
    public float handRaiseThreshold = 0.3f;
    [Tooltip("Minimum height gain from neutral position")]
    public float handRaiseMinHeight = 0.1f;

    [Header("Head Module")]
    public bool enableHeadModule = false;
    [Range(0.1f, 3.0f)]
    public float headSensitivity = 1.0f;
    public bool headDebugMode = false;
    [Space(5)]
    public bool isHeadPositionTracked = true;
    [Tooltip("Use relative positions from calibration vs absolute world positions")]
    public bool useRelativeHeadPosition = true;
    [Space(5)]
    public bool isHeadRotationTracked = true;
    [Space(5)]
    public bool isHeadDirectionEnabled = true;
    [Tooltip("Degrees of upward tilt required to trigger head up detection")]
    public float headUpThreshold = 15f;
    [Tooltip("Degrees of downward tilt required to trigger head down detection")]
    public float headDownThreshold = 15f;
    [Tooltip("Degrees of leftward rotation required to trigger head left detection")]
    public float headLeftThreshold = 20f;
    [Tooltip("Degrees of rightward rotation required to trigger head right detection")]
    public float headRightThreshold = 20f;

    [Header("Balance Module")]
    public bool enableBalanceModule = false;
    [Range(0.1f, 3.0f)]
    public float balanceSensitivity = 1.0f;
    public bool balanceDebugMode = false;
    [Space(5)]
    public bool isCoMTracked = true;
    [Tooltip("Track center of mass position")]
    public bool isSwayTracked = true;
    [Tooltip("Detect and measure body sway during balance")]
    public bool isStabilityTracked = true;
    [Tooltip("Track balance stability and detect balance loss/regain")]
    [Space(5)]
    [Tooltip("Sway magnitude threshold (meters) to trigger swaying detection")]
    public float swayThreshold = 0.1f;
    [Tooltip("CoM velocity threshold (m/s) - below this is considered stable")]
    public float stabilityThreshold = 0.05f;
    [Tooltip("Frames of CoM history to keep (180 = ~3 seconds at 60fps)")]
    public int comHistoryFrames = 180;

    [Header("Joint Names")]
    [Tooltip("Name of the pelvis/hips joint in your skeleton")]
    public string pelvisJointName = "Hips";
    [Tooltip("Name of the spine joint used for torso tracking")]
    public string spineJointName = "Spine4";
    [Tooltip("Name of the head joint")]
    public string headJointName = "Head";
    [Tooltip("Name of the neck joint")]
    public string neckJointName = "Neck";
    [Tooltip("Joint names for arm tracking")]
    public string leftShoulderJointName = "LeftShoulder";
    public string rightShoulderJointName = "RightShoulder";
    [Tooltip("Joint names for foot tracking")]
    public string leftFootJointName = "LeftFoot";
    public string rightFootJointName = "RightFoot";
    // Add this in the Joint Names section:
    [Tooltip("Spine joint for walk speed tracking (FootModule uses this, separate from TorsoModule)")]
    public string walkTrackingSpineJointName = "Spine";
    [Tooltip("Joint names for hand tracking")]
    public string leftHandJointName = "LeftHand";
    public string rightHandJointName = "RightHand";
    [Tooltip("Joint names for balance tracking (center of mass calculation)")]
    public string trunkJointName = "Spine1";
    public string leftForeArmJointName = "LeftForeArm";
    public string rightForeArmJointName = "RightForeArm";
    public string leftLegJointName = "LeftLeg";
    public string rightLegJointName = "RightLeg";

    private void OnValidate()
    {
        //  walk stop threshold is lower than walk start threshold
        if (walkStopThreshold >= walkSpeedThreshold)
        {
            walkStopThreshold = walkSpeedThreshold * 0.5f;
        }
    }
}