using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class ArmTrackingModule : MotionTrackingModule
{
    // internal states
    private bool isLeftHandRaised = false;
    private bool isRightHandRaised = false;

    // tracking transforms
    private Vector3 neutralLeftHandPosition = Vector3.zero;
    private Vector3 neutralRightHandPosition = Vector3.zero;
    private Vector3 neutralLeftShoulderPosition = Vector3.zero;
    private Vector3 neutralRightShoulderPosition = Vector3.zero;

    // neutral offsets between hand and shoulder
    private Vector3 neutralLeftHandToShoulderOffset = Vector3.zero;
    private Vector3 neutralRightHandToShoulderOffset = Vector3.zero;

    private Transform trackedLeftHand = null;
    private Transform trackedRightHand = null;
    private Transform trackedLeftShoulder = null;
    private Transform trackedRightShoulder = null;

    public override bool IsEnabled => manager?.Config?.enableArmsModule ?? false;
    public override float Sensitivity => manager?.Config?.armsSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.armsDebugMode ?? false;

    public bool IsHandPositionTracked => manager?.Config?.isHandPositionTracked ?? true;
    public bool IsHandRaiseTracked => manager?.Config?.isHandRaiseTracked ?? true;
    public bool UseRelativeHandPosition => manager?.Config?.useRelativeHandPosition ?? true;
    public float HandRaiseThreshold => manager?.Config?.handRaiseThreshold ?? 0.3f;
    public float HandRaiseMinHeight => manager?.Config?.handRaiseMinHeight ?? 0.1f;

    #region Initialize, Calibrate, Joints

    public override void Initialize(IMotionTrackingManager manager)
    {
        base.Initialize(manager);
        Debug.Log($"ArmsTrackingModule: Initialized with manager. Config present: {manager?.Config != null}");
        if (manager?.Config != null)
        {
            Debug.Log($"ArmsTrackingModule: Settings - Enabled: {IsEnabled}, Debug: {DebugMode}, " +
                     $"HandPositionTracked: {IsHandPositionTracked}, HandRaiseTracked: {IsHandRaiseTracked}");
        }
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("ArmsTrackingModule: Calibrate() called");
        Transform leftHand = GetLeftHandJoint(joints);
        Transform rightHand = GetRightHandJoint(joints);
        Transform leftShoulder = GetLeftShoulderJoint(joints);
        Transform rightShoulder = GetRightShoulderJoint(joints);

        if (leftHand != null && rightHand != null && leftShoulder != null && rightShoulder != null)
        {
            trackedLeftHand = leftHand;
            trackedRightHand = rightHand;
            trackedLeftShoulder = leftShoulder;
            trackedRightShoulder = rightShoulder;

            neutralLeftHandPosition = leftHand.position;
            neutralRightHandPosition = rightHand.position;
            neutralLeftShoulderPosition = leftShoulder.position;
            neutralRightShoulderPosition = rightShoulder.position;

            neutralLeftHandToShoulderOffset = neutralLeftHandPosition - neutralLeftShoulderPosition;
            neutralRightHandToShoulderOffset = neutralRightHandPosition - neutralRightShoulderPosition;

            isCalibrated = true;

            Debug.Log("ArmsTrackingModule: Successfully calibrated! " +
                     $"Left Hand: {neutralLeftHandPosition:F3}, Right Hand: {neutralRightHandPosition:F3}, " +
                     $"Left Shoulder: {neutralLeftShoulderPosition:F3}, Right Shoulder: {neutralRightShoulderPosition:F3}, " +
                     $"Left Offset: {neutralLeftHandToShoulderOffset:F3}, Right Offset: {neutralRightHandToShoulderOffset:F3}");
        }
        else
        {
            Debug.LogError($"ArmsTrackingModule: Failed to calibrate - missing joints! " +
                          $"LeftHand: {leftHand != null}, RightHand: {rightHand != null}, " +
                          $"LeftShoulder: {leftShoulder != null}, RightShoulder: {rightShoulder != null}");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        bool hasJoints = GetLeftHandJoint(joints) != null && GetRightHandJoint(joints) != null &&
                        GetLeftShoulderJoint(joints) != null && GetRightShoulderJoint(joints) != null;
        if (DebugMode) Debug.Log($"ArmsTrackingModule: HasRequiredJoints = {hasJoints}");
        return hasJoints;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.leftHandJointName ?? "LeftHand",
            manager?.Config?.rightHandJointName ?? "RightHand",
            manager?.Config?.leftShoulderJointName ?? "LeftShoulder",
            manager?.Config?.rightShoulderJointName ?? "RightShoulder"
        };
    }

    private Transform GetLeftHandJoint(Transform[] joints)
    {
        string leftHandName = manager?.Config?.leftHandJointName ?? "LeftHand";
        Transform leftHand = manager?.GetJointByName(leftHandName);

        if (DebugMode)
        {
            if (leftHand == null)
                Debug.LogWarning($"ArmsTrackingModule: Could not find left hand joint '{leftHandName}'");
            else
                Debug.Log($"ArmsTrackingModule: Found left hand joint '{leftHandName}' at position {leftHand.position}");
        }

        return leftHand;
    }

    private Transform GetRightHandJoint(Transform[] joints)
    {
        string rightHandName = manager?.Config?.rightHandJointName ?? "RightHand";
        Transform rightHand = manager?.GetJointByName(rightHandName);

        if (DebugMode)
        {
            if (rightHand == null)
                Debug.LogWarning($"ArmsTrackingModule: Could not find right hand joint '{rightHandName}'");
            else
                Debug.Log($"ArmsTrackingModule: Found right hand joint '{rightHandName}' at position {rightHand.position}");
        }

        return rightHand;
    }

    private Transform GetLeftShoulderJoint(Transform[] joints)
    {
        string leftShoulderName = manager?.Config?.leftShoulderJointName ?? "LeftShoulder";
        Transform leftShoulder = manager?.GetJointByName(leftShoulderName);

        if (DebugMode)
        {
            if (leftShoulder == null)
                Debug.LogWarning($"ArmsTrackingModule: Could not find left shoulder joint '{leftShoulderName}'");
            else
                Debug.Log($"ArmsTrackingModule: Found left shoulder joint '{leftShoulderName}' at position {leftShoulder.position}");
        }

        return leftShoulder;
    }

    private Transform GetRightShoulderJoint(Transform[] joints)
    {
        string rightShoulderName = manager?.Config?.rightShoulderJointName ?? "RightShoulder";
        Transform rightShoulder = manager?.GetJointByName(rightShoulderName);

        if (DebugMode)
        {
            if (rightShoulder == null)
                Debug.LogWarning($"ArmsTrackingModule: Could not find right shoulder joint '{rightShoulderName}'");
            else
                Debug.Log($"ArmsTrackingModule: Found right shoulder joint '{rightShoulderName}' at position {rightShoulder.position}");
        }

        return rightShoulder;
    }

    #endregion
    #region Update Functions

    public override void UpdateTracking(ref CapturyInputState state, Transform[] joints)
    {
        if (!IsEnabled || !IsCalibrated)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
            {
                if (!IsEnabled) Debug.Log("ArmsTrackingModule: Module disabled");
                if (!IsCalibrated) Debug.Log("ArmsTrackingModule: Module not calibrated");
            }
            return;
        }

        if (trackedLeftHand == null || trackedRightHand == null || trackedLeftShoulder == null || trackedRightShoulder == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log($"ArmsTrackingModule: Missing tracked joints");
            return;
        }

        Vector3 currentLeftHandPosition = trackedLeftHand.position;
        Vector3 currentRightHandPosition = trackedRightHand.position;
        Vector3 currentLeftShoulderPosition = trackedLeftShoulder.position;
        Vector3 currentRightShoulderPosition = trackedRightShoulder.position;

        // current hand-to-shoulder offsets
        Vector3 currentLeftHandToShoulderOffset = currentLeftHandPosition - currentLeftShoulderPosition;
        Vector3 currentRightHandToShoulderOffset = currentRightHandPosition - currentRightShoulderPosition;

        // calculate relative movement (difference from neutral offset)
        Vector3 leftRelativeMovement = currentLeftHandToShoulderOffset - neutralLeftHandToShoulderOffset;
        Vector3 rightRelativeMovement = currentRightHandToShoulderOffset - neutralRightHandToShoulderOffset;

        // update hand positions
        if (IsHandPositionTracked)
            UpdateHandPositions(ref state, leftRelativeMovement, rightRelativeMovement);

        // update hand raise detection
        if (IsHandRaiseTracked)
            UpdateHandRaise(ref state, currentLeftHandPosition, currentRightHandPosition,
                          currentLeftShoulderPosition, currentRightShoulderPosition);
    }

    private void UpdateHandPositions(ref CapturyInputState state, Vector3 leftRelativeMovement, Vector3 rightRelativeMovement)
    {

        if (UseRelativeHandPosition)
        {
            state.leftHandPosition = leftRelativeMovement * Sensitivity;
            state.rightHandPosition = rightRelativeMovement * Sensitivity;
        }
        else
        {
            // if not using relative, absolute hand positions
            state.leftHandPosition = trackedLeftHand.position * Sensitivity;
            state.rightHandPosition = trackedRightHand.position * Sensitivity;
        }
    }

    private void UpdateHandRaise(ref CapturyInputState state, Vector3 leftHandPos, Vector3 rightHandPos,
                                Vector3 leftShoulderPos, Vector3 rightShoulderPos)
    {
        // calculate height of hands relative to shoulders
        float leftHandRelativeHeight = leftHandPos.y - leftShoulderPos.y;
        float rightHandRelativeHeight = rightHandPos.y - rightShoulderPos.y;

        // check absolute height gain from neutral position
        float leftHandHeightGain = leftHandPos.y - neutralLeftHandPosition.y;
        float rightHandHeightGain = rightHandPos.y - neutralRightHandPosition.y;

        // left hand raise detection
        bool leftHandRaisedNow = (leftHandRelativeHeight > HandRaiseThreshold) &&
                                 (leftHandHeightGain > HandRaiseMinHeight);

        if (leftHandRaisedNow && !isLeftHandRaised)
        {
            isLeftHandRaised = true;
            state.leftHandRaised = 1.0f;

            if (DebugMode)
                Debug.Log($"ArmsTrackingModule: LEFT HAND RAISED! " +
                         $"RelHeight: {leftHandRelativeHeight:F3}, HeightGain: {leftHandHeightGain:F3}");
        }
        else if (!leftHandRaisedNow && isLeftHandRaised)
        {
            isLeftHandRaised = false;
            state.leftHandRaised = 0.0f;

            if (DebugMode)
                Debug.Log($"ArmsTrackingModule: LEFT HAND LOWERED!");
        }
        else
        {
            state.leftHandRaised = isLeftHandRaised ? 1.0f : 0.0f;
        }

        // right hand raise detection
        bool rightHandRaisedNow = (rightHandRelativeHeight > HandRaiseThreshold) &&
                                  (rightHandHeightGain > HandRaiseMinHeight);

        if (rightHandRaisedNow && !isRightHandRaised)
        {
            isRightHandRaised = true;
            state.rightHandRaised = 1.0f;

            if (DebugMode)
                Debug.Log($"ArmsTrackingModule: RIGHT HAND RAISED! " +
                         $"RelHeight: {rightHandRelativeHeight:F3}, HeightGain: {rightHandHeightGain:F3}");
        }
        else if (!rightHandRaisedNow && isRightHandRaised)
        {
            isRightHandRaised = false;
            state.rightHandRaised = 0.0f;

            if (DebugMode)
                Debug.Log($"ArmsTrackingModule: RIGHT HAND LOWERED!");
        }
        else
        {
            state.rightHandRaised = isRightHandRaised ? 1.0f : 0.0f;
        }
    }

    #endregion
    #region Utility Methods

    public void RecalibrateArmsModule()
    {
        if (trackedLeftHand != null && trackedRightHand != null &&
            trackedLeftShoulder != null && trackedRightShoulder != null)
        {
            Transform[] joints = { trackedLeftHand, trackedRightHand, trackedLeftShoulder, trackedRightShoulder };
            Calibrate(joints);
        }
    }

    public bool GetLeftHandRaised() => isLeftHandRaised;
    public bool GetRightHandRaised() => isRightHandRaised;

    public Vector3 GetCurrentLeftHandPosition() => trackedLeftHand?.position ?? Vector3.zero;
    public Vector3 GetCurrentRightHandPosition() => trackedRightHand?.position ?? Vector3.zero;

    public Vector3 GetLeftHandRelativeToShoulder()
    {
        if (trackedLeftHand == null || trackedLeftShoulder == null) return Vector3.zero;
        Vector3 currentOffset = trackedLeftHand.position - trackedLeftShoulder.position;
        return currentOffset - neutralLeftHandToShoulderOffset;
    }

    public Vector3 GetRightHandRelativeToShoulder()
    {
        if (trackedRightHand == null || trackedRightShoulder == null) return Vector3.zero;
        Vector3 currentOffset = trackedRightHand.position - trackedRightShoulder.position;
        return currentOffset - neutralRightHandToShoulderOffset;
    }

    #endregion
}