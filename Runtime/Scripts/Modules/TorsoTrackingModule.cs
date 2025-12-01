using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TorsoTrackingModule : MotionTrackingModule
{
    // internal states
    private bool isShiftingLeft = false;
    private bool isShiftingRight = false;
    private bool isBentOver = false;

    // tracking both pelvis and spine
    private Vector3 neutralPelvisPosition = Vector3.zero;
    private Vector3 neutralPelvisRotation = Vector3.zero;
    private Vector3 neutralSpinePosition = Vector3.zero;
    private Vector3 neutralSpineToePelvisOffset = Vector3.zero; // the neutral offset between spine and pelvis

    private Transform trackedPelvis = null;
    private Transform trackedSpine = null;

    // get from config!
    public override bool IsEnabled => manager?.Config?.enableTorsoModule ?? false;
    public override float Sensitivity => manager?.Config?.torsoSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.torsoDebugMode ?? false;

    public bool IsShiftTracked => manager?.Config?.isShiftTracked ?? false;
    //public bool IsBalanceTracked => manager?.Config?.isBalanceTracked ?? false;
    public bool IsBendTracked => manager?.Config?.isBendTracked ?? false;

    public float WeightShiftThreshold => manager?.Config?.weightShiftThreshold ?? 0.15f;
    public float NeutralZoneWidth => manager?.Config?.neutralZoneWidth ?? 0.05f;
    //public float BalanceThreshold => manager?.Config?.balanceThreshold ?? 0.1f;
    public float BentOverAngleThreshold => manager?.Config?.bentOverAngleThreshold ?? 30f;
    public float WholeBodyMovementThreshold => manager?.Config?.wholeBodyMovementThreshold ?? 0.8f;

    #region Initialize, Calibrate, Joints
    public override void Initialize(IMotionTrackingManager manager)
    {
        base.Initialize(manager);
        Debug.Log($"TorsoTrackingModule: Initialized with manager. Config present: {manager?.Config != null}");
        if (manager?.Config != null)
        {
            Debug.Log($"TorsoTrackingModule: Settings - Enabled: {IsEnabled}, Debug: {DebugMode}, " +
                     $"ShiftTracked: {IsShiftTracked}, BendTracked: {IsBendTracked}");
        }
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("TorsoTrackingModule: Calibrate() called");
        Transform pelvis = GetPelvisJoint(joints);
        Transform spine = GetSpineJoint(joints);

        if (pelvis != null && spine != null)
        {
            trackedPelvis = pelvis;
            trackedSpine = spine;

            neutralPelvisPosition = pelvis.position;
            neutralPelvisRotation = pelvis.eulerAngles;
            neutralSpinePosition = spine.position;
            neutralSpineToePelvisOffset = neutralSpinePosition - neutralPelvisPosition;

            isCalibrated = true;

            Debug.Log("TorsoTrackingModule: Successfully calibrated! " +
                     $"Neutral Pelvis: {neutralPelvisPosition:F3}, Neutral Spine: {neutralSpinePosition:F3}, " +
                     $"Neutral Offset: {neutralSpineToePelvisOffset:F3}");
        }
        else
        {
            Debug.LogError($"TorsoTrackingModule: Failed to calibrate - missing joints! " +
                          $"Pelvis: {pelvis != null}, Spine: {spine != null}");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        bool hasJoints = GetPelvisJoint(joints) != null && GetSpineJoint(joints) != null;
        if (DebugMode) Debug.Log($"TorsoTrackingModule: HasRequiredJoints = {hasJoints}");
        return hasJoints;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.pelvisJointName ?? "Hips",
            manager?.Config?.spineJointName ?? "Spine4"
        };
    }

    private Transform GetPelvisJoint(Transform[] joints)
    {
        string pelvisName = manager?.Config?.pelvisJointName ?? "Hips";
        Transform pelvis = manager?.GetJointByName(pelvisName);

        if (DebugMode)
        {
            if (pelvis == null)
            {
                Debug.LogWarning($"TorsoTrackingModule: Could not find pelvis joint '{pelvisName}'");
            }
            else
            {
                Debug.Log($"TorsoTrackingModule: Successfully found pelvis joint '{pelvisName}' at position {pelvis.position}");
            }
        }

        return pelvis;
    }

    private Transform GetSpineJoint(Transform[] joints)
    {
        string spineName = manager?.Config?.spineJointName ?? "Spine4";
        Transform spine = manager?.GetJointByName(spineName);

        if (DebugMode)
        {
            if (spine == null)
            {
                Debug.LogWarning($"TorsoTrackingModule: Could not find spine joint '{spineName}'");
            }
            else
            {
                Debug.Log($"TorsoTrackingModule: Successfully found spine joint '{spineName}' at position {spine.position}");
            }
        }

        return spine;
    }

    #endregion
    #region Update Functions

    public override void UpdateTracking(ref CapturyInputState state, Transform[] joints)
    {
        if (!IsEnabled || !IsCalibrated)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
            {
                if (!IsEnabled) Debug.Log("TorsoTrackingModule: Module disabled");
                if (!IsCalibrated) Debug.Log("TorsoTrackingModule: Module not calibrated");
            }
            return;
        }

        if (trackedPelvis == null || trackedSpine == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log($"TorsoTrackingModule: Missing tracked joints - Pelvis: {trackedPelvis != null}, Spine: {trackedSpine != null}");
            return;
        }

        // calculate relative positions
        Vector3 currentPelvisPosition = trackedPelvis.position;
        Vector3 currentSpinePosition = trackedSpine.position;

        Vector3 pelvisMovement = currentPelvisPosition - neutralPelvisPosition;
        Vector3 spineMovement = currentSpinePosition - neutralSpinePosition;

        // calculate the current spine-to-pelvis offset and compare to neutral
        Vector3 currentSpineToPelvisOffset = currentSpinePosition - currentPelvisPosition;
        Vector3 relativeMovement = currentSpineToPelvisOffset - neutralSpineToePelvisOffset;

        // for backwards compatibility, still set pelvisPosition to absolute movement
        state.pelvisPosition = pelvisMovement;

        // rotation calculations (using pelvis rotation as before)
        Vector3 currentRotation = trackedPelvis.eulerAngles;
        Vector3 relativeRotation = NormalizeEulerAngles(currentRotation - neutralPelvisRotation);

        // update tracking 
        if (IsShiftTracked)
            UpdateWeightShiftRelative(ref state, pelvisMovement, spineMovement, relativeMovement);
/*        if (IsBalanceTracked)
            UpdateBalance(ref state, relativeRotation, currentRotation);*/
        if (IsBendTracked)
            UpdateBentOver(ref state, relativeRotation);
    }

    private void UpdateWeightShiftRelative(ref CapturyInputState state, Vector3 pelvisMovement, Vector3 spineMovement, Vector3 relativeMovement)
    {
        // check if this is whole-body movement (both joints moving together)
        float pelvisXMovement = pelvisMovement.x;
        float spineXMovement = spineMovement.x;

        bool isWholeBodyMovement = false;
        if (Mathf.Abs(pelvisXMovement) > 0.01f && Mathf.Abs(spineXMovement) > 0.01f)
        {
            float movementRatio = Mathf.Abs(spineXMovement / pelvisXMovement);
            isWholeBodyMovement = movementRatio > WholeBodyMovementThreshold;
        }

        float shiftAmount = relativeMovement.x * Sensitivity;

        if (isWholeBodyMovement)
        {
            shiftAmount = 0f;
            if (DebugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"TorsoTrackingModule: Whole-body movement detected - ignoring weight shift. Pelvis X: {pelvisXMovement:F3}, Spine X: {spineXMovement:F3}, Ratio: {Mathf.Abs(spineXMovement / pelvisXMovement):F3}");
            }
        }

        state.weightShiftX = Mathf.Clamp(shiftAmount / WeightShiftThreshold, -1f, 1f);

        bool isInNeutralZone = Mathf.Abs(shiftAmount) < NeutralZoneWidth;

        if (shiftAmount < -NeutralZoneWidth && !isShiftingLeft)
        {
            isShiftingLeft = true;
            isShiftingRight = false;
            state.weightShiftLeft = 1.0f;
            state.weightShiftRight = 0.0f;

            if (DebugMode)
                Debug.Log($"TorsoTrackingModule: WEIGHT SHIFT LEFT detected! " +
                         $"Relative X: {relativeMovement.x:F3}, Adjusted: {shiftAmount:F3}, " +
                         $"WholeBody: {isWholeBodyMovement}");
        }
        else if (shiftAmount > NeutralZoneWidth && !isShiftingRight)
        {
            isShiftingRight = true;
            isShiftingLeft = false;
            state.weightShiftRight = 1.0f;
            state.weightShiftLeft = 0.0f;

            if (DebugMode)
                Debug.Log($"TorsoTrackingModule: WEIGHT SHIFT RIGHT detected! " +
                         $"Relative X: {relativeMovement.x:F3}, Adjusted: {shiftAmount:F3}, " +
                         $"WholeBody: {isWholeBodyMovement}");
        }
        else if (isInNeutralZone && (isShiftingLeft || isShiftingRight))
        {
            isShiftingLeft = false;
            isShiftingRight = false;
            state.weightShiftLeft = 0.0f;
            state.weightShiftRight = 0.0f;

            if (DebugMode)
                Debug.Log($"TorsoTrackingModule: Weight returned to NEUTRAL. " +
                         $"Relative X: {relativeMovement.x:F3}, Adjusted: {shiftAmount:F3}");
        }
        else
        {
            state.weightShiftLeft = isShiftingLeft ? 1.0f : 0.0f;
            state.weightShiftRight = isShiftingRight ? 1.0f : 0.0f;
        }
    }

    private void UpdateBentOver(ref CapturyInputState state, Vector3 relativeRotation)
    {
        float xRotationDiff = Mathf.Abs(relativeRotation.x);
        bool currentlyBentOver = xRotationDiff > BentOverAngleThreshold;

        state.isBentOver = currentlyBentOver ? 1.0f : 0.0f;
        state.isUpright = currentlyBentOver ? 0.0f : 1.0f;

        if (DebugMode && (xRotationDiff > BentOverAngleThreshold * 0.7f || Time.frameCount % 120 == 0))
        {
            Debug.Log($"BentOver: XRotDiff={xRotationDiff:F1}, Threshold={BentOverAngleThreshold:F1}, BentOver={currentlyBentOver}");
        }

        if (currentlyBentOver != isBentOver)
        {
            isBentOver = currentlyBentOver;
            if (DebugMode)
                Debug.Log($"TorsoTrackingModule: Posture changed to {(isBentOver ? "BENT OVER" : "UPRIGHT")} " +
                         $"(X rotation diff: {xRotationDiff:F1} degrees)");
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
}