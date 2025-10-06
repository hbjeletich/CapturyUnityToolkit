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
    private Transform trackedTrunk = null;  // Spine1
    private Transform trackedLeftForeArm = null;
    private Transform trackedRightForeArm = null;
    private Transform trackedLeftLeg = null;  // lower leg/calf
    private Transform trackedRightLeg = null;

    // neutral calibration
    private Vector3 neutralCoM = Vector3.zero;
    private float neutralGroundHeight = 0.0f;

    // current CoM tracking
    private Vector3 currentCoM = Vector3.zero;
    private Vector3 previousCoM = Vector3.zero;
    private float comVelocity = 0f;

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

        if (trunk != null && leftForeArm != null && rightForeArm != null && 
            leftLeg != null && rightLeg != null)
        {
            trackedTrunk = trunk;
            trackedLeftForeArm = leftForeArm;
            trackedRightForeArm = rightForeArm;
            trackedLeftLeg = leftLeg;
            trackedRightLeg = rightLeg;

            // calculate initial CoM
            neutralCoM = CalculateCenterOfMass();
            neutralGroundHeight = Mathf.Min(leftLeg.position.y, rightLeg.position.y);

            // initialize tracking
            currentCoM = neutralCoM;
            previousCoM = neutralCoM;

            comHistory.Clear();
            comTimestamps.Clear();

            isCalibrated = true;

            Debug.Log("BalanceTrackingModule: Successfully calibrated! " +
                     $"Neutral CoM: {neutralCoM:F3}, Ground Height: {neutralGroundHeight:F3}");
        }
        else
        {
            Debug.LogError($"BalanceTrackingModule: Failed to calibrate - missing joints! " +
                          $"Trunk: {trunk != null}, LeftForeArm: {leftForeArm != null}, " +
                          $"RightForeArm: {rightForeArm != null}, LeftLeg: {leftLeg != null}, RightLeg: {rightLeg != null}");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        return GetTrunkJoint(joints) != null && 
               GetLeftForeArmJoint(joints) != null && 
               GetRightForeArmJoint(joints) != null &&
               GetLeftLegJoint(joints) != null && 
               GetRightLegJoint(joints) != null;
    }

    public override string[] GetRequiredJointNames()
    {
        return new string[] {
            manager?.Config?.trunkJointName ?? "Spine1",
            manager?.Config?.leftForeArmJointName ?? "LeftForeArm",
            manager?.Config?.rightForeArmJointName ?? "RightForeArm",
            manager?.Config?.leftLegJointName ?? "LeftLeg",
            manager?.Config?.rightLegJointName ?? "RightLeg"
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
            trackedLeftLeg == null || trackedRightLeg == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log("BalanceTrackingModule: Missing tracked joints");
            return;
        }

        // calculate current center of mass
        previousCoM = currentCoM;
        currentCoM = CalculateCenterOfMass();

        // update data buffers
        UpdateDataBuffers();

        // track CoM position
        if (IsCoMTracked)
            UpdateCoMPosition(ref state);

        // track sway
        if (IsSwayTracked)
            UpdateSway(ref state);

        // track stability
        if (IsStabilityTracked)
            UpdateStability(ref state);
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
        Vector3 comRelativeToNeutral = currentCoM - neutralCoM;
        state.centerOfMassPosition = comRelativeToNeutral * Sensitivity;

        if (DebugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"BalanceTrackingModule: CoM Offset: {comRelativeToNeutral:F3}");
        }
    }

    private void UpdateSway(ref CapturyInputState state)
    {
        Vector3 swayVector = currentCoM - neutralCoM;
        
        // separate lateral (X) and anterior-posterior (Z) sway
        float lateralSway = swayVector.x;
        float apSway = swayVector.z;

        state.lateralSway = lateralSway * Sensitivity;
        state.anteriorPosteriorSway = apSway * Sensitivity;

        // calculate total sway magnitude
        swayMagnitude = new Vector2(lateralSway, apSway).magnitude;
        state.swayMagnitude = swayMagnitude;

        // detect excessive sway
        bool swayingNow = swayMagnitude > SwayThreshold;
        state.isSwaying = swayingNow ? 1.0f : 0.0f;

        if (DebugMode && swayingNow && Time.frameCount % 30 == 0)
        {
            Debug.Log($"BalanceTrackingModule: SWAYING - Lateral: {lateralSway:F3}, AP: {apSway:F3}, Magnitude: {swayMagnitude:F3}");
        }
    }

    private void UpdateStability(ref CapturyInputState state)
    {
        if (comHistory.Count < 2) return;

        // calculate CoM velocity
        Vector3 comDisplacement = currentCoM - previousCoM;
        float deltaTime = Time.deltaTime;
        comVelocity = comDisplacement.magnitude / deltaTime;

        state.comVelocity = comVelocity;

        // stability is inversely related to velocity
        // low velocity = high stability
        bool isStable = comVelocity < StabilityThreshold;
        bool wasBalanced = isBalanced;
        isBalanced = isStable;

        state.isBalanced = isBalanced ? 1.0f : 0.0f;
        state.balanceLost = (!isBalanced && wasBalanced) ? 1.0f : 0.0f;
        state.balanceRegained = (isBalanced && !wasBalanced) ? 1.0f : 0.0f;

        if (DebugMode && (!isBalanced || Time.frameCount % 120 == 0))
        {
            Debug.Log($"BalanceTrackingModule: Velocity: {comVelocity:F3} m/s, " +
                     $"Balanced: {isBalanced}, Sway: {swayMagnitude:F3}");
        }
    }

    #endregion
    #region Utility Methods

    public void RecalibrateBalanceModule()
    {
        if (trackedTrunk != null && trackedLeftForeArm != null && trackedRightForeArm != null &&
            trackedLeftLeg != null && trackedRightLeg != null)
        {
            Transform[] joints = { trackedTrunk, trackedLeftForeArm, trackedRightForeArm, 
                                  trackedLeftLeg, trackedRightLeg };
            Calibrate(joints);
        }
    }

    public Vector3 GetCurrentCoM() => currentCoM;
    public Vector3 GetCoMRelativeToNeutral() => currentCoM - neutralCoM;
    public float GetSwayMagnitude() => swayMagnitude;
    public float GetCoMVelocity() => comVelocity;
    public bool GetIsBalanced() => isBalanced;

    public Vector2 GetSwayComponents()
    {
        Vector3 sway = currentCoM - neutralCoM;
        return new Vector2(sway.x, sway.z); // lateral, anterior-posterior
    }

    #endregion
}