using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;
using System.Linq;

public class BalanceTrackingModule : MotionTrackingModule
{
    #region Variables

    // body segment masses (from research - fractions of total body mass)
    private const float TRUNK_MASS = 0.497f;
    private const float FOREARM_MASS = 0.016f;
    private const float LOWER_LEG_MASS = 0.0465f;

    // tracking transforms
    private Transform trackedTrunk = null;
    private Transform trackedLeftForeArm = null;
    private Transform trackedRightForeArm = null;
    private Transform trackedLeftLeg = null;
    private Transform trackedRightLeg = null;

    // TOE BASE TRACKING FOR BASE OF SUPPORT
    private Transform trackedLeftToeBase = null;
    private Transform trackedRightToeBase = null;

    // neutral calibration
    private Vector3 neutralCoM = Vector3.zero;
    private float neutralGroundHeight = 0.0f;

    // current CoM tracking
    private Vector3 currentCoM = Vector3.zero;
    private Vector3 previousCoM = Vector3.zero;
    private float comVelocity = 0f;

    // BASE OF SUPPORT TRACKING
    private Vector2 baseOfSupportCenter = Vector2.zero;
    private float baseOfSupportWidth = 0f;
    private float comToBaseOfSupportDistance = 0f;

    // balance state
    private bool isBalanced = true;
    private float swayMagnitude = 0f;

    // data buffering
    private Queue<Vector3> comHistory;
    private Queue<float> comTimestamps;

    public override bool IsEnabled => manager?.Config?.enableBalanceModule ?? false;
    public override float Sensitivity => manager?.Config?.balanceSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.balanceDebugMode ?? false;

    // balance-specific config
    public bool IsCoMTracked => manager?.Config?.isCoMTracked ?? true;
    public bool IsSwayTracked => manager?.Config?.isSwayTracked ?? true;
    public bool IsStabilityTracked => manager?.Config?.isStabilityTracked ?? true;
    public float SwayThreshold => manager?.Config?.swayThreshold ?? 0.1f;
    public float StabilityThreshold => manager?.Config?.stabilityThreshold ?? 0.05f;
    public int CoMHistoryFrames => manager?.Config?.comHistoryFrames ?? 180;

    #endregion
    #region Initialize, Calibrate, Joints

    public override void Initialize(MotionTrackingManager manager)
    {
        base.Initialize(manager);

        comHistory = new Queue<Vector3>();
        comTimestamps = new Queue<float>();

        Debug.Log($"BalanceTrackingModule: Initialized - CoM: {IsCoMTracked}, Sway: {IsSwayTracked}, Stability: {IsStabilityTracked}");
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("BalanceTrackingModule: Calibrate() called");

        Transform trunk = GetTrunkJoint(joints);
        Transform leftForeArm = GetLeftForeArmJoint(joints);
        Transform rightForeArm = GetRightForeArmJoint(joints);
        Transform leftLeg = GetLeftLegJoint(joints);
        Transform rightLeg = GetRightLegJoint(joints);
        Transform leftToeBase = GetLeftToeBaseJoint(joints);
        Transform rightToeBase = GetRightToeBaseJoint(joints);

        if (trunk != null && leftForeArm != null && rightForeArm != null &&
            leftLeg != null && rightLeg != null && leftToeBase != null && rightToeBase != null)
        {
            trackedTrunk = trunk;
            trackedLeftForeArm = leftForeArm;
            trackedRightForeArm = rightForeArm;
            trackedLeftLeg = leftLeg;
            trackedRightLeg = rightLeg;
            trackedLeftToeBase = leftToeBase;
            trackedRightToeBase = rightToeBase;

            // calculate initial CoM
            neutralCoM = CalculateCenterOfMass();
            neutralGroundHeight = Mathf.Min(leftToeBase.position.y, rightToeBase.position.y);

            // initialize tracking
            currentCoM = neutralCoM;
            previousCoM = neutralCoM;

            // calculate initial base of support
            UpdateBaseOfSupport();

            comHistory.Clear();
            comTimestamps.Clear();

            isCalibrated = true;

            Debug.Log("BalanceTrackingModule: Successfully calibrated! " +
                     $"Neutral CoM: {neutralCoM:F3}, Ground Height: {neutralGroundHeight:F3}, " +
                     $"Base of Support Width: {baseOfSupportWidth:F3}");
        }
        else
        {
            Debug.LogError($"BalanceTrackingModule: Failed to calibrate - missing joints! " +
                          $"Trunk: {trunk != null}, LeftForeArm: {leftForeArm != null}, " +
                          $"RightForeArm: {rightForeArm != null}, LeftLeg: {leftLeg != null}, " +
                          $"RightLeg: {rightLeg != null}, LeftToeBase: {leftToeBase != null}, RightToeBase: {rightToeBase != null}");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        return GetTrunkJoint(joints) != null &&
               GetLeftForeArmJoint(joints) != null &&
               GetRightForeArmJoint(joints) != null &&
               GetLeftLegJoint(joints) != null &&
               GetRightLegJoint(joints) != null &&
               GetLeftToeBaseJoint(joints) != null &&
               GetRightToeBaseJoint(joints) != null;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.trunkJointName ?? "Spine1",
            manager?.Config?.leftForeArmJointName ?? "LeftForeArm",
            manager?.Config?.rightForeArmJointName ?? "RightForeArm",
            manager?.Config?.leftLegJointName ?? "LeftLeg",
            manager?.Config?.rightLegJointName ?? "RightLeg",
            manager?.Config?.leftToeBaseJointName ?? "LeftToeBase",
            manager?.Config?.rightToeBaseJointName ?? "RightToeBase"
        };
    }

    private Transform GetTrunkJoint(Transform[] joints)
    {
        string trunkName = manager?.Config?.trunkJointName ?? "Spine1";
        Transform trunk = manager?.GetJointByName(trunkName);

        if (DebugMode)
        {
            if (trunk == null)
                Debug.LogWarning($"BalanceTrackingModule: Could not find trunk joint '{trunkName}'");
            else
                Debug.Log($"BalanceTrackingModule: Found trunk joint '{trunkName}' at {trunk.position}");
        }

        return trunk;
    }

    private Transform GetLeftForeArmJoint(Transform[] joints)
    {
        string leftForeArmName = manager?.Config?.leftForeArmJointName ?? "LeftForeArm";
        return manager?.GetJointByName(leftForeArmName);
    }

    private Transform GetRightForeArmJoint(Transform[] joints)
    {
        string rightForeArmName = manager?.Config?.rightForeArmJointName ?? "RightForeArm";
        return manager?.GetJointByName(rightForeArmName);
    }

    private Transform GetLeftLegJoint(Transform[] joints)
    {
        string leftLegName = manager?.Config?.leftLegJointName ?? "LeftLeg";
        return manager?.GetJointByName(leftLegName);
    }

    private Transform GetRightLegJoint(Transform[] joints)
    {
        string rightLegName = manager?.Config?.rightLegJointName ?? "RightLeg";
        return manager?.GetJointByName(rightLegName);
    }

    private Transform GetLeftToeBaseJoint(Transform[] joints)
    {
        string leftToeBaseName = manager?.Config?.leftToeBaseJointName ?? "LeftToeBase";
        return manager?.GetJointByName(leftToeBaseName);
    }

    private Transform GetRightToeBaseJoint(Transform[] joints)
    {
        string rightToeBaseName = manager?.Config?.rightToeBaseJointName ?? "RightToeBase";
        return manager?.GetJointByName(rightToeBaseName);
    }

    #endregion
    #region Update Functions

    public override void UpdateTracking(ref CapturyInputState state, Transform[] joints)
    {
        if (!IsEnabled || !IsCalibrated)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
            {
                if (!IsEnabled) Debug.Log("BalanceTrackingModule: Module disabled");
                if (!IsCalibrated) Debug.Log("BalanceTrackingModule: Module not calibrated");
            }
            return;
        }

        if (trackedTrunk == null || trackedLeftForeArm == null || trackedRightForeArm == null ||
            trackedLeftLeg == null || trackedRightLeg == null ||
            trackedLeftToeBase == null || trackedRightToeBase == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log("BalanceTrackingModule: Missing tracked joints");
            return;
        }

        // calculate current center of mass
        previousCoM = currentCoM;
        currentCoM = CalculateCenterOfMass();

        // update base of support from current toe positions
        UpdateBaseOfSupport();

        // update data buffers
        UpdateDataBuffers();

        // track CoM position
        if (IsCoMTracked)
            UpdateCoMPosition(ref state);

        // track sway (now relative to base of support)
        if (IsSwayTracked)
            UpdateSwayRelativeToFeet(ref state);

        // track stability (improved with base of support)
        if (IsStabilityTracked)
            UpdateStabilityWithBaseOfSupport(ref state);
    }

    private Vector3 CalculateCenterOfMass()
    {
        // weighted average of body segment positions
        float totalMass = TRUNK_MASS + (2 * FOREARM_MASS) + (2 * LOWER_LEG_MASS);

        Vector3 weightedSum = Vector3.zero;
        weightedSum += trackedTrunk.position * TRUNK_MASS;
        weightedSum += trackedLeftForeArm.position * FOREARM_MASS;
        weightedSum += trackedRightForeArm.position * FOREARM_MASS;
        weightedSum += trackedLeftLeg.position * LOWER_LEG_MASS;
        weightedSum += trackedRightLeg.position * LOWER_LEG_MASS;

        return weightedSum / totalMass;
    }

    private void UpdateBaseOfSupport()
    {
        // get toe base positions projected onto ground plane (XZ)
        Vector2 leftToePos2D = new Vector2(trackedLeftToeBase.position.x, trackedLeftToeBase.position.z);
        Vector2 rightToePos2D = new Vector2(trackedRightToeBase.position.x, trackedRightToeBase.position.z);

        // center of base of support is midpoint between toes
        baseOfSupportCenter = (leftToePos2D + rightToePos2D) / 2f;

        // width of base of support
        baseOfSupportWidth = Vector2.Distance(leftToePos2D, rightToePos2D);

        // calculate CoM projection onto ground plane
        Vector2 comProjection = new Vector2(currentCoM.x, currentCoM.z);

        // distance from CoM to center of base of support
        comToBaseOfSupportDistance = Vector2.Distance(comProjection, baseOfSupportCenter);
    }

    private void UpdateDataBuffers()
    {
        comHistory.Enqueue(currentCoM);
        comTimestamps.Enqueue(Time.time);

        while (comHistory.Count > CoMHistoryFrames)
        {
            comHistory.Dequeue();
            comTimestamps.Dequeue();
        }
    }

    private void UpdateCoMPosition(ref CapturyInputState state)
    {
        // instead of comparing to neutral, compare to base of support center
        Vector2 comProjection = new Vector2(currentCoM.x, currentCoM.z);
        Vector2 comRelativeToBase = comProjection - baseOfSupportCenter;

        // convert back to 3D (keep Y component relative to neutral)
        Vector3 comRelativeToSupport = new Vector3(
            comRelativeToBase.x,
            currentCoM.y - neutralCoM.y,
            comRelativeToBase.y
        );

        state.centerOfMassPosition = comRelativeToSupport * Sensitivity;

        if (DebugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"BalanceTrackingModule: CoM offset from base center: {comRelativeToBase.magnitude:F3}m");
        }
    }

    private void UpdateSwayRelativeToFeet(ref CapturyInputState state)
    {
        // calculate sway relative to base of support center instead of neutral position
        Vector2 comProjection = new Vector2(currentCoM.x, currentCoM.z);
        Vector2 swayVector = comProjection - baseOfSupportCenter;

        // separate lateral (X) and anterior-posterior (Z) sway
        float lateralSway = swayVector.x;
        float apSway = swayVector.y;

        state.lateralSway = lateralSway * Sensitivity;
        state.anteriorPosteriorSway = apSway * Sensitivity;

        // calculate total sway magnitude
        swayMagnitude = swayVector.magnitude;
        state.swayMagnitude = swayMagnitude;

        // normalize sway by base of support width for better threshold
        float normalizedSway = baseOfSupportWidth > 0 ? swayMagnitude / (baseOfSupportWidth * 0.5f) : 0f;

        // detect excessive sway (CoM getting close to edge of base of support)
        bool swayingNow = normalizedSway > 0.6f; // 60% of half base width
        state.isSwaying = swayingNow ? 1.0f : 0.0f;

        if (DebugMode && (swayingNow || Time.frameCount % 120 == 0))
        {
            Debug.Log($"BalanceTrackingModule: Lateral: {lateralSway:F3}, AP: {apSway:F3}, " +
                     $"Total: {swayMagnitude:F3}, Normalized: {normalizedSway:F2}, " +
                     $"BaseWidth: {baseOfSupportWidth:F3}");
        }
    }

    private void UpdateStabilityWithBaseOfSupport(ref CapturyInputState state)
    {
        if (comHistory.Count < 2) return;

        // calculate CoM velocity
        Vector3 comDisplacement = currentCoM - previousCoM;
        float deltaTime = Time.deltaTime;
        comVelocity = comDisplacement.magnitude / deltaTime;

        state.comVelocity = comVelocity;

        // improved balance determination:
        // 1. Low velocity (stable movement)
        // 2. CoM projection within base of support
        bool hasLowVelocity = comVelocity < StabilityThreshold;

        // CoM should be within reasonable distance from base center
        // If feet are wide apart, allow more deviation
        float maxAllowedDistance = baseOfSupportWidth * 0.4f; // 40% of base width
        bool withinBaseOfSupport = comToBaseOfSupportDistance < maxAllowedDistance;

        bool wasBalanced = isBalanced;
        isBalanced = hasLowVelocity && withinBaseOfSupport;

        state.isBalanced = isBalanced ? 1.0f : 0.0f;
        state.balanceLost = (!isBalanced && wasBalanced) ? 1.0f : 0.0f;
        state.balanceRegained = (isBalanced && !wasBalanced) ? 1.0f : 0.0f;

        if (DebugMode && (!isBalanced || Time.frameCount % 120 == 0))
        {
            Debug.Log($"BalanceTrackingModule: Velocity: {comVelocity:F3} m/s, " +
                     $"CoM Distance from Base: {comToBaseOfSupportDistance:F3}m, " +
                     $"Max Allowed: {maxAllowedDistance:F3}m, " +
                     $"Balanced: {isBalanced} (velocity OK: {hasLowVelocity}, within base: {withinBaseOfSupport})");
        }
    }

    #endregion
    #region Utility Methods

    public void RecalibrateBalanceModule()
    {
        if (trackedTrunk != null && trackedLeftForeArm != null && trackedRightForeArm != null &&
            trackedLeftLeg != null && trackedRightLeg != null &&
            trackedLeftToeBase != null && trackedRightToeBase != null)
        {
            Transform[] joints = { trackedTrunk, trackedLeftForeArm, trackedRightForeArm,
                                  trackedLeftLeg, trackedRightLeg, trackedLeftToeBase, trackedRightToeBase };
            Calibrate(joints);
        }
    }

    public Vector3 GetCurrentCoM() => currentCoM;
    public Vector3 GetCoMRelativeToNeutral() => currentCoM - neutralCoM;
    public Vector2 GetCoMRelativeToBaseOfSupport()
    {
        Vector2 comProjection = new Vector2(currentCoM.x, currentCoM.z);
        return comProjection - baseOfSupportCenter;
    }
    public float GetSwayMagnitude() => swayMagnitude;
    public float GetCoMVelocity() => comVelocity;
    public bool GetIsBalanced() => isBalanced;
    public float GetBaseOfSupportWidth() => baseOfSupportWidth;
    public Vector2 GetBaseOfSupportCenter() => baseOfSupportCenter;
    public float GetDistanceFromBaseCenter() => comToBaseOfSupportDistance;

    public Vector2 GetSwayComponents()
    {
        Vector2 comProjection = new Vector2(currentCoM.x, currentCoM.z);
        Vector2 sway = comProjection - baseOfSupportCenter;
        return sway; // lateral (x), anterior-posterior (z)
    }

    #endregion
}