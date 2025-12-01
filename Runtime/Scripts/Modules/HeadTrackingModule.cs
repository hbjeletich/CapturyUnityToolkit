using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class HeadTrackingModule : MotionTrackingModule
{
    // internal states
    private bool isHeadUp = false;
    private bool isHeadDown = false;
    private bool isHeadLeft = false;
    private bool isHeadRight = false;

    // tracking transforms
    private Vector3 neutralHeadPosition = Vector3.zero;
    private Vector3 neutralNeckPosition = Vector3.zero;
    private Vector3 neutralHeadRotation = Vector3.zero;
    private Vector3 neutralNeckRotation = Vector3.zero;
    private Vector3 neutralHeadToNeckOffset = Vector3.zero;
    private Vector3 neutralHeadToNeckRotationOffset = Vector3.zero;

    private Transform trackedHead = null;
    private Transform trackedNeck = null;

    // get from config
    public override bool IsEnabled => manager?.Config?.enableHeadModule ?? false;
    public override float Sensitivity => manager?.Config?.headSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.headDebugMode ?? false;

    public bool IsHeadPositionTracked => manager?.Config?.isHeadPositionTracked ?? true;
    public bool IsHeadRotationTracked => manager?.Config?.isHeadRotationTracked ?? true;
    public bool IsDirectionDetectionEnabled => manager?.Config?.isHeadDirectionEnabled ?? true;
    public bool UseRelativeHeadPosition => manager?.Config?.useRelativeHeadPosition ?? true;

    // direction thresholds (in degrees for rotation)
    public float HeadUpThreshold => manager?.Config?.headUpThreshold ?? 15f;
    public float HeadDownThreshold => manager?.Config?.headDownThreshold ?? 15f;
    public float HeadLeftThreshold => manager?.Config?.headLeftThreshold ?? 20f;
    public float HeadRightThreshold => manager?.Config?.headRightThreshold ?? 20f;

    #region Initialize, Calibrate, Joints

    public override void Initialize(IMotionTrackingManager manager)
    {
        base.Initialize(manager);
        Debug.Log($"HeadTrackingModule: Initialized with manager. Config present: {manager?.Config != null}");
        if (manager?.Config != null)
        {
            Debug.Log($"HeadTrackingModule: Settings - Enabled: {IsEnabled}, Debug: {DebugMode}, " +
                     $"DirectionDetection: {IsDirectionDetectionEnabled}");
        }
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("HeadTrackingModule: Calibrate() called");
        Transform head = GetHeadJoint(joints);
        Transform neck = GetNeckJoint(joints);

        if (head != null && neck != null)
        {
            trackedHead = head;
            trackedNeck = neck;

            neutralHeadPosition = head.position;
            neutralNeckPosition = neck.position;
            neutralHeadRotation = head.eulerAngles;
            neutralNeckRotation = neck.eulerAngles;

            // calculate and store offsets
            neutralHeadToNeckOffset = neutralHeadPosition - neutralNeckPosition;
            neutralHeadToNeckRotationOffset = NormalizeEulerAngles(neutralHeadRotation - neutralNeckRotation);

            isCalibrated = true;

            Debug.Log("HeadTrackingModule: Successfully calibrated! " +
                     $"Neutral Head: {neutralHeadPosition:F3}, Neutral Neck: {neutralNeckPosition:F3}, " +
                     $"Neutral Head Rotation: {neutralHeadRotation:F3}, Neutral Neck Rotation: {neutralNeckRotation:F3}, " +
                     $"Neutral Rotation Offset: {neutralHeadToNeckRotationOffset:F3}");
        }
        else
        {
            Debug.LogError($"HeadTrackingModule: Failed to calibrate - missing joints! " +
                          $"Head: {head != null}, Neck: {neck != null}");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        bool hasJoints = GetHeadJoint(joints) != null && GetNeckJoint(joints) != null;
        if (DebugMode) Debug.Log($"HeadTrackingModule: HasRequiredJoints = {hasJoints}");
        return hasJoints;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.headJointName ?? "Head",
            manager?.Config?.neckJointName ?? "Neck"
        };
    }

    private Transform GetHeadJoint(Transform[] joints)
    {
        string headName = manager?.Config?.headJointName ?? "Head";
        Transform head = manager?.GetJointByName(headName);

        if (DebugMode)
        {
            if (head == null)
                Debug.LogWarning($"HeadTrackingModule: Could not find head joint '{headName}'");
            else
                Debug.Log($"HeadTrackingModule: Found head joint '{headName}' at position {head.position}");
        }

        return head;
    }

    private Transform GetNeckJoint(Transform[] joints)
    {
        string neckName = manager?.Config?.neckJointName ?? "Neck";
        Transform neck = manager?.GetJointByName(neckName);

        if (DebugMode)
        {
            if (neck == null)
                Debug.LogWarning($"HeadTrackingModule: Could not find neck joint '{neckName}'");
            else
                Debug.Log($"HeadTrackingModule: Found neck joint '{neckName}' at position {neck.position}");
        }

        return neck;
    }

    #endregion
    #region Update Functions

    public override void UpdateTracking(ref CapturyInputState state, Transform[] joints)
    {
        if (!IsEnabled || !IsCalibrated)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
            {
                if (!IsEnabled) Debug.Log("HeadTrackingModule: Module disabled");
                if (!IsCalibrated) Debug.Log("HeadTrackingModule: Module not calibrated");
            }
            return;
        }

        if (trackedHead == null || trackedNeck == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log($"HeadTrackingModule: Missing tracked joints - Head: {trackedHead != null}, Neck: {trackedNeck != null}");
            return;
        }

        Vector3 currentHeadPosition = trackedHead.position;
        Vector3 currentNeckPosition = trackedNeck.position;
        Vector3 currentHeadRotation = trackedHead.eulerAngles;
        Vector3 currentNeckRotation = trackedNeck.eulerAngles;

        // calculate current head-to-neck offset
        Vector3 currentHeadToNeckOffset = currentHeadPosition - currentNeckPosition;
        Vector3 relativePositionMovement = currentHeadToNeckOffset - neutralHeadToNeckOffset;

        // calculate current head-to-neck rotation offset
        Vector3 currentHeadToNeckRotationOffset = NormalizeEulerAngles(currentHeadRotation - currentNeckRotation);
        Vector3 relativeRotationMovement = NormalizeEulerAngles(currentHeadToNeckRotationOffset - neutralHeadToNeckRotationOffset);

        // update head position
        if (IsHeadPositionTracked)
            UpdateHeadPosition(ref state, relativePositionMovement);

        // update head rotation
        if (IsHeadRotationTracked)
            UpdateHeadRotation(ref state, relativeRotationMovement);

        // update directional detection based on relative rotation
        if (IsDirectionDetectionEnabled)
            UpdateHeadDirection(ref state, relativeRotationMovement);
    }

    private void UpdateHeadPosition(ref CapturyInputState state, Vector3 relativeMovement)
    {
        if (UseRelativeHeadPosition)
        {
            state.headPosition = relativeMovement * Sensitivity;
        }
        else
        {
            state.headPosition = trackedHead.position * Sensitivity;
        }
    }

    private void UpdateHeadRotation(ref CapturyInputState state, Vector3 relativeRotation)
    {
        // rotation relative to neck
        state.headRotation = relativeRotation * Sensitivity;
    }

    private void UpdateHeadDirection(ref CapturyInputState state, Vector3 relativeRotation)
    {
        // Roll (Z rotation) - negative is looking up, positive is looking down
        float pitchAngle = relativeRotation.z;

        // Yaw (Y rotation) - negative is looking left, positive is looking right
        float yawAngle = relativeRotation.y;

        // HEAD UP detection (negative)
        bool headUpNow = pitchAngle < -HeadUpThreshold;
        if (headUpNow != isHeadUp)
        {
            isHeadUp = headUpNow;
            if (DebugMode)
                Debug.Log($"HeadTrackingModule: HEAD {(isHeadUp ? "UP" : "NEUTRAL (vertical)")} - Pitch: {pitchAngle:F1}°");
        }
        state.headUp = isHeadUp ? 1.0f : 0.0f;

        // HEAD DOWN detection (positive)
        bool headDownNow = pitchAngle > HeadDownThreshold;
        if (headDownNow != isHeadDown)
        {
            isHeadDown = headDownNow;
            if (DebugMode)
                Debug.Log($"HeadTrackingModule: HEAD {(isHeadDown ? "DOWN" : "NEUTRAL (vertical)")} - Pitch: {pitchAngle:F1}°");
        }
        state.headDown = isHeadDown ? 1.0f : 0.0f;

        // HEAD LEFT detection (negative)
        bool headLeftNow = yawAngle < -HeadLeftThreshold;
        if (headLeftNow != isHeadLeft)
        {
            isHeadLeft = headLeftNow;
            if (DebugMode)
                Debug.Log($"HeadTrackingModule: HEAD {(isHeadLeft ? "LEFT" : "NEUTRAL (horizontal)")} - Yaw: {yawAngle:F1}°");
        }
        state.headLeft = isHeadLeft ? 1.0f : 0.0f;

        // HEAD RIGHT detection (positive)
        bool headRightNow = yawAngle > HeadRightThreshold;
        if (headRightNow != isHeadRight)
        {
            isHeadRight = headRightNow;
            if (DebugMode)
                Debug.Log($"HeadTrackingModule: HEAD {(isHeadRight ? "RIGHT" : "NEUTRAL (horizontal)")} - Yaw: {yawAngle:F1}°");
        }
        state.headRight = isHeadRight ? 1.0f : 0.0f;

        // log all values periodically for debugging
        if (DebugMode && Time.frameCount % 120 == 0)
        {
            Debug.Log($"HeadTrackingModule: Pitch={pitchAngle:F1}°, Yaw={yawAngle:F1}° | " +
                     $"Up={isHeadUp}, Down={isHeadDown}, Left={isHeadLeft}, Right={isHeadRight}");
        }
    }

    #endregion
    #region Helper Functions

    private Vector3 NormalizeEulerAngles(Vector3 angles)
    {
        angles.x = NormalizeAngle(angles.x);
        angles.y = NormalizeAngle(angles.y);
        angles.z = NormalizeAngle(angles.z);
        return angles;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    #endregion
    #region Utility Methods

    public void RecalibrateHeadModule()
    {
        if (trackedHead != null && trackedNeck != null)
        {
            Transform[] joints = { trackedHead, trackedNeck };
            Calibrate(joints);
        }
    }

    public bool GetIsHeadUp() => isHeadUp;
    public bool GetIsHeadDown() => isHeadDown;
    public bool GetIsHeadLeft() => isHeadLeft;
    public bool GetIsHeadRight() => isHeadRight;

    public Vector3 GetCurrentHeadPosition() => trackedHead?.position ?? Vector3.zero;
    public Vector3 GetCurrentHeadRotation() => trackedHead?.eulerAngles ?? Vector3.zero;
    public Vector3 GetRelativeHeadRotation()
    {
        if (trackedHead == null || trackedNeck == null) return Vector3.zero;
        Vector3 currentHeadToNeckRotationOffset = NormalizeEulerAngles(trackedHead.eulerAngles - trackedNeck.eulerAngles);
        return NormalizeEulerAngles(currentHeadToNeckRotationOffset - neutralHeadToNeckRotationOffset);
    }

    public Vector3 GetHeadRelativeToNeck()
    {
        if (trackedHead == null || trackedNeck == null) return Vector3.zero;
        Vector3 currentOffset = trackedHead.position - trackedNeck.position;
        return currentOffset - neutralHeadToNeckOffset;
    }

    #endregion
}