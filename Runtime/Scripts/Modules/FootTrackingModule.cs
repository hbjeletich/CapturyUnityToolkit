using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using System.Collections.Generic;
using System.Linq;

public class FootTrackingModule : MotionTrackingModule
{
    #region Data Structures

    [System.Serializable]
    public struct FootEvent
    {
        public float timestamp;
        public bool isLeftFoot;
        public bool isFootDown;
        public Vector3 position;

        public FootEvent(float time, bool left, bool down, Vector3 pos)
        {
            timestamp = time;
            isLeftFoot = left;
            isFootDown = down;
            position = pos;
        }
    }

    public enum WalkState
    {
        Idle,
        InitiatingWalk,
        Walking,
        Stopping
    }

    #endregion
    #region Variables

    // foot tracking
    private bool isFootRaised = false;
    private bool isLeftHipAbducted = false;
    private bool isRightHipAbducted = false;
    private float defaultFootDistance = 0.0f;
    private float groundHeight = 0.0f;
    private Vector3 neutralLeftFootPosition = Vector3.zero;
    private Vector3 neutralRightFootPosition = Vector3.zero;
    private Transform trackedLeftFoot = null;
    private Transform trackedRightFoot = null;

    // walk tracking - using spine instead of pelvis to avoid conflict with TorsoModule
    private Transform trackedSpineForWalk = null;

    // walk tracking
    private WalkState currentWalkState = WalkState.Idle;
    private WalkState previousWalkState = WalkState.Idle;
    private float walkStateChangeTime = 0f;
    private float currentWalkSpeed = 0f;

    // data buffers
    private Queue<Vector3> spinePositionHistory;
    private Queue<float> timestampHistory;
    private Queue<FootEvent> footEventHistory;

    // foot contact detection
    private bool leftFootInContact = false;
    private bool rightFootInContact = false;

    // gait analysis
    private float lastLeftStepTime = 0f;
    private float lastRightStepTime = 0f;
    private float lastLeftContactTime = 0f;
    private float lastRightContactTime = 0f;
    private float currentCadence = 0f;
    private List<float> recentStepTimes; // for consistency calculation

    public override bool IsEnabled => manager?.Config?.enableFootModule ?? false;
    public override float Sensitivity => manager?.Config?.footSensitivity ?? 1.0f;
    public override bool DebugMode => manager?.Config?.footDebugMode ?? false;

    // basic foot tracking
    public bool IsFootRaiseTracked => manager?.Config?.isFootRaiseTracked ?? true;
    public bool IsHipAbductionTracked => manager?.Config?.isHipAbductionTracked ?? true;
    public bool IsFootPositionTracked => manager?.Config?.isFootPositionTracked ?? true;
    public bool UseRelativeFootPosition => manager?.Config?.useRelativeFootPosition ?? true;
    public float FootRaiseThreshold => manager?.Config?.footRaiseThreshold ?? 0.1f;
    public float MinAbductionDistance => manager?.Config?.minAbductionDistance ?? 0.2f;
    public float MinLiftHeight => manager?.Config?.minLiftHeight ?? 0.05f;

    // walk tracking
    public bool IsWalkTrackingEnabled => manager?.Config?.enableWalkTracking ?? false;
    public float WalkSpeedThreshold => manager?.Config?.walkSpeedThreshold ?? 0.3f;
    public float MinimumWalkDuration => manager?.Config?.minimumWalkDuration ?? 2.0f;
    public float WalkStopThreshold => manager?.Config?.walkStopThreshold ?? 0.1f;

    // gait analysis
    public bool IsGaitAnalysisEnabled => manager?.Config?.enableGaitAnalysis ?? false;
    public int MinimumCyclesForAnalysis => manager?.Config?.minimumCyclesForAnalysis ?? 3;
    public float MaxReasonableStepTime => manager?.Config?.maxReasonableStepTime ?? 2.0f;
    public float MinReasonableStepTime => manager?.Config?.minReasonableStepTime ?? 0.3f;
    
    // data buffering
    public int PositionHistoryFrames => manager?.Config?.positionHistoryFrames ?? 300;
    public int EventHistoryCount => manager?.Config?.eventHistoryCount ?? 20;

    #endregion
    #region Initialize, Calibrate, Joints

    public override void Initialize(MotionTrackingManager manager)
    {
        base.Initialize(manager);

        // initialize simplified data structures
        spinePositionHistory = new Queue<Vector3>();
        timestampHistory = new Queue<float>();
        footEventHistory = new Queue<FootEvent>();
        recentStepTimes = new List<float>();

        Debug.Log($"FootTrackingModule: Initialized with Walk: {IsWalkTrackingEnabled}, Gait: {IsGaitAnalysisEnabled}");
    }

    public override void Calibrate(Transform[] joints)
    {
        Debug.Log("FootTrackingModule: Calibrate() called");
        Transform leftFoot = GetLeftFootJoint(joints);
        Transform rightFoot = GetRightFootJoint(joints);

        if (leftFoot != null && rightFoot != null)
        {
            trackedLeftFoot = leftFoot;
            trackedRightFoot = rightFoot;

            Vector3 leftPos = leftFoot.position;
            Vector3 rightPos = rightFoot.position;

            neutralLeftFootPosition = leftPos;
            neutralRightFootPosition = rightPos;

            Vector2 leftPos2D = new Vector2(leftPos.x, leftPos.z);
            Vector2 rightPos2D = new Vector2(rightPos.x, rightPos.z);
            defaultFootDistance = Vector2.Distance(leftPos2D, rightPos2D);
            groundHeight = (leftPos.y + rightPos.y) / 2.0f;

            // get spine for walk tracking if enabled
            if (IsWalkTrackingEnabled || IsGaitAnalysisEnabled)
            {
                trackedSpineForWalk = GetSpineForWalkJoint(joints);
                if (trackedSpineForWalk == null)
                {
                    Debug.LogWarning("FootTrackingModule: Could not find Spine joint for walk tracking! Walk detection may not work correctly.");
                }
            }

            ClearHistoryBuffers();

            isCalibrated = true;
            Debug.Log("FootTrackingModule: Successfully calibrated with simplified walk/gait support!");
        }
        else
        {
            Debug.LogError($"FootTrackingModule: Failed to calibrate - missing joints!");
            isCalibrated = false;
        }
    }

    public override bool HasRequiredJoints(Transform[] joints)
    {
        bool hasFeet = GetLeftFootJoint(joints) != null && GetRightFootJoint(joints) != null;
        
        // if walk tracking is enabled, also need spine
        if (IsWalkTrackingEnabled || IsGaitAnalysisEnabled)
        {
            bool hasSpine = GetSpineForWalkJoint(joints) != null;
            return hasFeet && hasSpine;
        }
        
        return hasFeet;
    }

    public override string[] GetRequiredJointNames()
    {
        if (IsWalkTrackingEnabled || IsGaitAnalysisEnabled)
        {
            return new string[] {
                manager?.Config?.leftFootJointName ?? "LeftFoot",
                manager?.Config?.rightFootJointName ?? "RightFoot",
                manager?.Config?.walkTrackingSpineJointName ?? "Spine"
            };
        }
        
        return new string[] {
            manager?.Config?.leftFootJointName ?? "LeftFoot",
            manager?.Config?.rightFootJointName ?? "RightFoot"
        };
    }

    private Transform GetLeftFootJoint(Transform[] joints)
    {
        string leftFootName = manager?.Config?.leftFootJointName ?? "LeftFoot";
        return manager?.GetJointByName(leftFootName);
    }

    private Transform GetRightFootJoint(Transform[] joints)
    {
        string rightFootName = manager?.Config?.rightFootJointName ?? "RightFoot";
        return manager?.GetJointByName(rightFootName);
    }

    private Transform GetSpineForWalkJoint(Transform[] joints)
    {
        string spineName = manager?.Config?.walkTrackingSpineJointName ?? "Spine";
        Transform spine = manager?.GetJointByName(spineName);
        
        if (DebugMode)
        {
            if (spine == null)
                Debug.LogWarning($"FootTrackingModule: Could not find spine joint '{spineName}' for walk tracking");
            else
                Debug.Log($"FootTrackingModule: Found spine joint '{spineName}' for walk tracking at {spine.position}");
        }
        
        return spine;
    }

    #endregion
    #region Main Update Loop

    public override void UpdateTracking(ref CapturyInputState state, Transform[] joints)
    {
        if (!IsEnabled || !IsCalibrated || trackedLeftFoot == null || trackedRightFoot == null)
        {
            if (DebugMode && Time.frameCount % 300 == 0)
                Debug.Log("FootTrackingModule: Not updating - disabled or not calibrated");
            return;
        }

        Vector3 leftPos = trackedLeftFoot.position;
        Vector3 rightPos = trackedRightFoot.position;
        float leftHeight = leftPos.y - groundHeight;
        float rightHeight = rightPos.y - groundHeight;

        // update data buffers
        if (IsWalkTrackingEnabled || IsGaitAnalysisEnabled)
        {
            UpdateDataBuffers();
        }

        // layer 1: foot tracking (always runs)
        UpdateBasicFootTracking(ref state, leftPos, rightPos, leftHeight, rightHeight);

        // layer 2: walk detection (if enabled)
        if (IsWalkTrackingEnabled)
        {
            UpdateWalkDetection(ref state);
        }

        // layer 3: gait analysis (if enabled)
        if (IsGaitAnalysisEnabled)
        {
            UpdateGaitAnalysis(ref state, leftHeight, rightHeight);
        }

        // update walk state in output
        if (IsWalkTrackingEnabled)
        {
            UpdateWalkState(ref state);
        }
    }

    #endregion
    #region Layer 1: Basic Foot Tracking

    private void UpdateBasicFootTracking(ref CapturyInputState state, Vector3 leftPos, Vector3 rightPos, float leftHeight, float rightHeight)
    {
        if (IsFootRaiseTracked)
            UpdateFootRaise(ref state, leftHeight, rightHeight);

        if (IsHipAbductionTracked)
            UpdateHipAbduction(ref state, leftPos, rightPos, leftHeight, rightHeight);

        if (IsFootPositionTracked)
            UpdateFootPositions(ref state, leftPos, rightPos);
    }

    private void UpdateFootRaise(ref CapturyInputState state, float leftHeight, float rightHeight)
    {
        float footHeightDifference = Mathf.Abs(leftHeight - rightHeight);

        bool footRaisedNow = footHeightDifference > FootRaiseThreshold;

        if (footRaisedNow && !isFootRaised)
        {
            isFootRaised = true;
            state.footRaised = 1.0f;
            state.footLowered = 0.0f;

            if (DebugMode)
                Debug.Log($"FootTrackingModule: FOOT RAISED!");
        }
        else if (!footRaisedNow && isFootRaised)
        {
            isFootRaised = false;
            state.footRaised = 0.0f;
            state.footLowered = 1.0f;

            if (DebugMode)
                Debug.Log($"FootTrackingModule: FOOT LOWERED!");
        }
        else
        {
            state.footRaised = isFootRaised ? 1.0f : 0.0f;
            state.footLowered = 0.0f;
        }
    }

    private void UpdateHipAbduction(ref CapturyInputState state, Vector3 leftPos, Vector3 rightPos, float leftHeight, float rightHeight)
    {
        Vector2 leftPos2D = new Vector2(leftPos.x, leftPos.z);
        Vector2 rightPos2D = new Vector2(rightPos.x, rightPos.z);
        float currentDistance = Vector2.Distance(leftPos2D, rightPos2D);
        float abductionDistance = currentDistance - defaultFootDistance;

        bool leftLiftedEnough = leftHeight > MinLiftHeight;
        bool rightLiftedEnough = rightHeight > MinLiftHeight;
        bool distanceIncreased = abductionDistance > MinAbductionDistance;

        bool leftAbductedNow = leftLiftedEnough && distanceIncreased;
        bool rightAbductedNow = rightLiftedEnough && distanceIncreased;

        if (leftAbductedNow != isLeftHipAbducted)
        {
            isLeftHipAbducted = leftAbductedNow;
            if (DebugMode) Debug.Log($"LEFT HIP ABDUCTION {(leftAbductedNow ? "ON" : "OFF")}");
        }
        state.leftHipAbducted = isLeftHipAbducted ? 1.0f : 0.0f;

        if (rightAbductedNow != isRightHipAbducted)
        {
            isRightHipAbducted = rightAbductedNow;
            if (DebugMode) Debug.Log($"RIGHT HIP ABDUCTION {(rightAbductedNow ? "ON" : "OFF")}");
        }
        state.rightHipAbducted = isRightHipAbducted ? 1.0f : 0.0f;
    }

    private void UpdateFootPositions(ref CapturyInputState state, Vector3 leftPos, Vector3 rightPos)
    {
        if (UseRelativeFootPosition)
        {
            state.leftFootPosition = (leftPos - neutralLeftFootPosition) * Sensitivity;
            state.rightFootPosition = (rightPos - neutralRightFootPosition) * Sensitivity;
        }
        else
        {
            state.leftFootPosition = leftPos * Sensitivity;
            state.rightFootPosition = rightPos * Sensitivity;
        }
    }

    #endregion
    #region Layer 2: Walk Detection

    private void UpdateDataBuffers()
    {
        float currentTime = Time.time;
        timestampHistory.Enqueue(currentTime);

        // get spine for walk speed calculation
        // NOTE: now using Spine instead of Pelvis to avoid conflict with TorsoModule
        if (trackedSpineForWalk != null)
        {
            spinePositionHistory.Enqueue(trackedSpineForWalk.position);
        }

        // maintain buffer size
        while (spinePositionHistory.Count > PositionHistoryFrames)
        {
            spinePositionHistory.Dequeue();
            timestampHistory.Dequeue();
        }
    }

    private void UpdateWalkDetection(ref CapturyInputState state)
    {
        float currentSpeed = CalculateCurrentSpeed();

        previousWalkState = currentWalkState;

        switch (currentWalkState)
        {
            case WalkState.Idle:
                if (currentSpeed > WalkSpeedThreshold)
                {
                    currentWalkState = WalkState.InitiatingWalk;
                    walkStateChangeTime = Time.time;
                    if (DebugMode) Debug.Log("Walk: Idle -> InitiatingWalk");
                }
                break;

            case WalkState.InitiatingWalk:
                if (currentSpeed < WalkStopThreshold)
                {
                    currentWalkState = WalkState.Idle;
                    if (DebugMode) Debug.Log("Walk: InitiatingWalk -> Idle (false start)");
                }
                else if (Time.time - walkStateChangeTime > MinimumWalkDuration)
                {
                    currentWalkState = WalkState.Walking;
                    if (DebugMode) Debug.Log("Walk: InitiatingWalk -> Walking");
                }
                break;

            case WalkState.Walking:
                if (currentSpeed < WalkStopThreshold)
                {
                    currentWalkState = WalkState.Stopping;
                    walkStateChangeTime = Time.time;
                    if (DebugMode) Debug.Log("Walk: Walking -> Stopping");
                }
                break;

            case WalkState.Stopping:
                if (currentSpeed > WalkSpeedThreshold)
                {
                    currentWalkState = WalkState.Walking;
                    if (DebugMode) Debug.Log("Walk: Stopping -> Walking (resumed)");
                }
                else if (Time.time - walkStateChangeTime > 1.0f)
                {
                    currentWalkState = WalkState.Idle;
                    if (DebugMode) Debug.Log("Walk: Stopping -> Idle");
                }
                break;
        }

        currentWalkSpeed = currentSpeed;
    }

    private float CalculateCurrentSpeed()
    {
        if (spinePositionHistory.Count < 30) return 0f;

        Vector3[] recentPositions = spinePositionHistory.TakeLast(30).ToArray();
        if (recentPositions.Length < 2) return 0f;

        Vector3 movement = recentPositions[recentPositions.Length - 1] - recentPositions[0];
        float timeSpan = 0.5f; // 30 frames at 60fps

        return movement.magnitude / timeSpan;
    }

    #endregion
    #region Layer 3: Gait Analysis

    private void UpdateGaitAnalysis(ref CapturyInputState state, float leftHeight, float rightHeight)
    {
        float currentTime = Time.time;

        // foot contact detection
        bool leftContactNow = leftHeight < (MinLiftHeight * 0.5f);
        bool rightContactNow = rightHeight < (MinLiftHeight * 0.5f);

        // left foot step detection
        if (leftContactNow && !leftFootInContact)
        {
            leftFootInContact = true;
            RecordFootEvent(currentTime, true, true, trackedLeftFoot.position);

            if (lastLeftContactTime > 0)
            {
                float stepTime = currentTime - lastLeftContactTime;
                if (stepTime >= MinReasonableStepTime && stepTime <= MaxReasonableStepTime)
                {
                    lastLeftStepTime = stepTime;
                    state.leftStep = 1.0f;
                    state.leftStepTime = stepTime;
                    recentStepTimes.Add(stepTime);

                    if (DebugMode) Debug.Log($"LEFT STEP: {stepTime:F3}s");
                }
            }
            lastLeftContactTime = currentTime;
        }
        else if (!leftContactNow && leftFootInContact)
        {
            leftFootInContact = false;
            RecordFootEvent(currentTime, true, false, trackedLeftFoot.position);
        }
        else
        {
            state.leftStep = 0.0f;
        }

        // right foot step detection
        if (rightContactNow && !rightFootInContact)
        {
            rightFootInContact = true;
            RecordFootEvent(currentTime, false, true, trackedRightFoot.position);

            if (lastRightContactTime > 0)
            {
                float stepTime = currentTime - lastRightContactTime;
                if (stepTime >= MinReasonableStepTime && stepTime <= MaxReasonableStepTime)
                {
                    lastRightStepTime = stepTime;
                    state.rightStep = 1.0f;
                    state.rightStepTime = stepTime;
                    recentStepTimes.Add(stepTime);

                    if (DebugMode) Debug.Log($"RIGHT STEP: {stepTime:F3}s");
                }
            }
            lastRightContactTime = currentTime;
        }
        else if (!rightContactNow && rightFootInContact)
        {
            rightFootInContact = false;
            RecordFootEvent(currentTime, false, false, trackedRightFoot.position);
        }
        else
        {
            state.rightStep = 0.0f;
        }

        // analyze gait metrics
        AnalyzeGaitMetrics(ref state);
    }

    private void RecordFootEvent(float time, bool isLeftFoot, bool isFootDown, Vector3 position)
    {
        FootEvent evt = new FootEvent(time, isLeftFoot, isFootDown, position);
        footEventHistory.Enqueue(evt);

        while (footEventHistory.Count > EventHistoryCount)
        {
            footEventHistory.Dequeue();
        }
    }

    private void AnalyzeGaitMetrics(ref CapturyInputState state)
    {
        if (lastLeftStepTime > 0 && lastRightStepTime > 0)
        {
            // calculate asymmetry
            float stepTimeAsymmetry = Mathf.Abs(lastLeftStepTime - lastRightStepTime) /
                                    ((lastLeftStepTime + lastRightStepTime) / 2f);

            state.stepTimeAsymmetry = stepTimeAsymmetry;

            // calculate cadence
            float averageStepTime = (lastLeftStepTime + lastRightStepTime) / 2f;
            currentCadence = 60f / averageStepTime;
            state.cadence = currentCadence;

            // gait consistency
            if (recentStepTimes.Count >= MinimumCyclesForAnalysis * 2) // need multiple steps
            {
                // keep only recent step times
                while (recentStepTimes.Count > 20)
                {
                    recentStepTimes.RemoveAt(0);
                }

                float mean = recentStepTimes.Average();
                float variance = recentStepTimes.Select(t => (t - mean) * (t - mean)).Average();
                float stdDev = Mathf.Sqrt(variance);
                float consistency = Mathf.Clamp01(1f - (stdDev / mean));
                state.gaitConsistency = consistency;
            }
        }
    }

    #endregion
    #region Layer 4: State Output

    private void UpdateWalkState(ref CapturyInputState state)
    {
        state.isWalking = (currentWalkState == WalkState.Walking) ? 1.0f : 0.0f;
        state.walkStarted = (previousWalkState != WalkState.Walking && currentWalkState == WalkState.Walking) ? 1.0f : 0.0f;
        state.walkStopped = (previousWalkState == WalkState.Walking && currentWalkState != WalkState.Walking) ? 1.0f : 0.0f;
        state.walkSpeed = currentWalkSpeed;
    }

    #endregion
    #region Utility Methods

    private void ClearHistoryBuffers()
    {
        spinePositionHistory?.Clear();
        timestampHistory?.Clear();
        footEventHistory?.Clear();
        recentStepTimes?.Clear();

        currentWalkState = WalkState.Idle;
        leftFootInContact = false;
        rightFootInContact = false;
    }

    public void RecalibrateFootModule()
    {
        if (trackedLeftFoot != null && trackedRightFoot != null)
        {
            Transform[] joints = { trackedLeftFoot, trackedRightFoot };
            Calibrate(joints);
        }
    }

    public float GetCurrentFootDistance()
    {
        if (trackedLeftFoot == null || trackedRightFoot == null) return 0f;

        Vector3 leftPos = trackedLeftFoot.position;
        Vector3 rightPos = trackedRightFoot.position;
        Vector2 leftPos2D = new Vector2(leftPos.x, leftPos.z);
        Vector2 rightPos2D = new Vector2(rightPos.x, rightPos.z);

        return Vector2.Distance(leftPos2D, rightPos2D);
    }

    public WalkState GetWalkState() => currentWalkState;
    public float GetCurrentCadence() => currentCadence;

    #endregion
}