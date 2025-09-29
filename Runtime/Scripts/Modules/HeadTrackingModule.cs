using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class HeadTrackingModule : MotionTrackingModule
{
    // internal states
    private bool isNodding = false;
    private bool isShakingNo = false;

    // tracking transforms
    private Vector3 neutralHeadPosition = Vector3.zero;
    private Vector3 neutralHeadRotation = Vector3.zero;
    private Transform trackedHead = null;

    // gesture detection
    private float lastNodTime = 0f;
    private float lastShakeTime = 0f;
    private float nodGestureStartTime = 0f;
    private float shakeGestureStartTime = 0f;

    // get from config
    public override bool IsEnabled => manager?.Config?.enableHeadModule ?? false;
    public override float Sensitivity => manager?.Config?.headSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.headDebugMode ?? false;

    public bool IsHeadPositionTracked => manager?.Config?.isHeadPositionTracked ?? true;
    public bool IsHeadRotationTracked => manager?.Config?.isHeadRotationTracked ?? true;
    public bool IsNodDetectionEnabled => manager?.Config?.isNodDetectionEnabled ?? true;
    public bool IsShakeDetectionEnabled => manager?.Config?.isShakeDetectionEnabled ?? true;
    public bool UseRelativeHeadPosition => manager?.Config?.useRelativeHeadPosition ?? true;

    // gesture thresholds
    public float NodThreshold => manager?.Config?.nodThreshold ?? 15f;
    public float ShakeThreshold => manager?.Config?.shakeThreshold ?? 20f;
    public float NodSpeed => manager?.Config?.nodSpeed ?? 0.5f;
    public float ShakeSpeed => manager?.Config?.shakeSpeed ?? 0.7f;
    public float GestureTimeout => manager?.Config?.gestureTimeout ?? 2.0f;
    public float NeutralReturnThreshold => manager?.Config?.neutralReturnThreshold ?? 5f;

    #region Initialize, Calibrate, Joints

    public override void Initialize(MotionTrackingManager manager)
    {
        base.Initialize(manager);
        Debug.Log($"HeadTrackingModule: Initialized with manager. Config present: {manager?.Config != null}");
        if (manager?.Config != null)
        {
            Debug.Log($"HeadTrackingModule: Settings - Enabled: {IsEnabled}, Debug: {DebugMode}, " +
                     $"NodDetection: {IsNodDetectionEnabled}, ShakeDetection: {IsShakeDetectionEnabled}");
        }
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("HeadTrackingModule: Calibrate() called");
        Transform head = GetHeadJoint(joints);

        if (head != null)
        {
            trackedHead = head;
            neutralHeadPosition = head.position;
            neutralHeadRotation = head.eulerAngles;

            isCalibrated = true;

            Debug.Log("HeadTrackingModule: Successfully calibrated! " +
                     $"Neutral Position: {neutralHeadPosition:F3}, Neutral Rotation: {neutralHeadRotation:F3}");
        }
        else
        {
            Debug.LogError($"HeadTrackingModule: Failed to calibrate - missing head joint!");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        bool hasJoints = GetHeadJoint(joints) != null;
        if (DebugMode) Debug.Log($"HeadTrackingModule: HasRequiredJoints = {hasJoints}");
        return hasJoints;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.headJointName ?? "Head"
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

        if (trackedHead == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log($"HeadTrackingModule: Missing tracked head joint");
            return;
        }

        Vector3 currentHeadPosition = trackedHead.position;
        Vector3 currentHeadRotation = trackedHead.eulerAngles;

        // update head position
        if (IsHeadPositionTracked)
            UpdateHeadPosition(ref state, currentHeadPosition);

        // update head rotation
        if (IsHeadRotationTracked)
            UpdateHeadRotation(ref state, currentHeadRotation);

        // update gesture detection
        if (IsNodDetectionEnabled)
            UpdateNodDetection(ref state, currentHeadRotation);

        if (IsShakeDetectionEnabled)
            UpdateShakeDetection(ref state, currentHeadRotation);
    }

    private void UpdateHeadPosition(ref CapturyInputState state, Vector3 currentPosition)
    {
        if (UseRelativeHeadPosition)
        {
            state.headPosition = (currentPosition - neutralHeadPosition) * Sensitivity;
        }
        else
        {
            state.headPosition = currentPosition * Sensitivity;
        }
    }

    private void UpdateHeadRotation(ref CapturyInputState state, Vector3 currentRotation)
    {
        Vector3 relativeRotation = NormalizeEulerAngles(currentRotation - neutralHeadRotation);
        state.headRotation = relativeRotation * Sensitivity;
    }

    private void UpdateNodDetection(ref CapturyInputState state, Vector3 currentRotation)
    {
        Vector3 relativeRotation = NormalizeEulerAngles(currentRotation - neutralHeadRotation);
        float pitchAngle = relativeRotation.x;

        float currentTime = Time.time;
        bool significantNodMovement = Mathf.Abs(pitchAngle) > NodThreshold;

        if (significantNodMovement)
        {
            if (!isNodding)
            {
                // start of nod gesture
                isNodding = true;
                nodGestureStartTime = currentTime;
                state.headNodding = 1.0f;

                if (DebugMode)
                    Debug.Log($"HeadTrackingModule: NOD STARTED! Pitch: {pitchAngle:F1}°");
            }
            else
            {
                // continue nodding
                state.headNodding = 1.0f;
            }

            lastNodTime = currentTime;
        }
        else if (isNodding)
        {
            // check if we should stop nodding (returned to neutral or timeout)
            bool returnedToNeutral = Mathf.Abs(pitchAngle) < NeutralReturnThreshold;
            bool timedOut = (currentTime - lastNodTime) > NodSpeed;

            if (returnedToNeutral || timedOut)
            {
                isNodding = false;
                state.headNodding = 0.0f;

                if (DebugMode)
                    Debug.Log($"HeadTrackingModule: NOD ENDED! Reason: {(returnedToNeutral ? "Returned to neutral" : "Timeout")}");
            }
            else
            {
                state.headNodding = 1.0f;
            }
        }
        else
        {
            state.headNodding = 0.0f;
        }

        // time out old gestures
        if (isNodding && (currentTime - nodGestureStartTime) > GestureTimeout)
        {
            isNodding = false;
            state.headNodding = 0.0f;
            if (DebugMode) Debug.Log("HeadTrackingModule: Nod gesture timed out");
        }
    }

    private void UpdateShakeDetection(ref CapturyInputState state, Vector3 currentRotation)
    {
        Vector3 relativeRotation = NormalizeEulerAngles(currentRotation - neutralHeadRotation);
        float yawAngle = relativeRotation.y;

        float currentTime = Time.time;
        bool significantShakeMovement = Mathf.Abs(yawAngle) > ShakeThreshold;

        if (significantShakeMovement)
        {
            if (!isShakingNo)
            {
                // start of shake gesture
                isShakingNo = true;
                shakeGestureStartTime = currentTime;
                state.headShaking = 1.0f;

                if (DebugMode)
                    Debug.Log($"HeadTrackingModule: SHAKE STARTED! Yaw: {yawAngle:F1}°");
            }
            else
            {
                // continue shaking
                state.headShaking = 1.0f;
            }

            lastShakeTime = currentTime;
        }
        else if (isShakingNo)
        {
            // check if we should stop shaking (returned to neutral or timeout)
            bool returnedToNeutral = Mathf.Abs(yawAngle) < NeutralReturnThreshold;
            bool timedOut = (currentTime - lastShakeTime) > ShakeSpeed;

            if (returnedToNeutral || timedOut)
            {
                isShakingNo = false;
                state.headShaking = 0.0f;

                if (DebugMode)
                    Debug.Log($"HeadTrackingModule: SHAKE ENDED! Reason: {(returnedToNeutral ? "Returned to neutral" : "Timeout")}");
            }
            else
            {
                state.headShaking = 1.0f;
            }
        }
        else
        {
            state.headShaking = 0.0f;
        }

        // time out old gestures
        if (isShakingNo && (currentTime - shakeGestureStartTime) > GestureTimeout)
        {
            isShakingNo = false;
            state.headShaking = 0.0f;
            if (DebugMode) Debug.Log("HeadTrackingModule: Shake gesture timed out");
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
        if (trackedHead != null)
        {
            Transform[] joints = { trackedHead };
            Calibrate(joints);
        }
    }

    public bool GetIsNodding() => isNodding;
    public bool GetIsShaking() => isShakingNo;

    public Vector3 GetCurrentHeadPosition() => trackedHead?.position ?? Vector3.zero;
    public Vector3 GetCurrentHeadRotation() => trackedHead?.eulerAngles ?? Vector3.zero;
    public Vector3 GetRelativeHeadRotation() =>
        trackedHead != null ? NormalizeEulerAngles(trackedHead.eulerAngles - neutralHeadRotation) : Vector3.zero;

    #endregion
}