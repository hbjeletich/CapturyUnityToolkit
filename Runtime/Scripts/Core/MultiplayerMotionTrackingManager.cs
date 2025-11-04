using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Captury;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class MultiplayerMotionTrackingManager : MonoBehaviour, IMotionTrackingManager
{
    #region Configuration and Settings

    [Header("Configuration")]
    [SerializeField] private MotionTrackingConfiguration config;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool enableDebugLogging = true;

    [Header("Multiplayer Settings")]
    [SerializeField] private bool automaticCalibration = true;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float calibrationDelayPerSkeleton = 2.0f;

    #endregion

    #region Skeleton Tracking Data Structure

    private class SkeletonTrackingData
    {
        public int capturyId;
        public int playerNumber;
        public string skeletonName;
        public string playerLabel;
        
        public Dictionary<string, Transform> joints;
        public List<MotionTrackingModule> modules;
        public CapturyInput inputDevice;
        
        public bool isCalibrated;
        public Coroutine calibrationCoroutine;
        
        public TorsoTrackingModule torsoModule;
        public FootTrackingModule footModule;
        public ArmTrackingModule armsModule;
        public HeadTrackingModule headModule;
        public BalanceTrackingModule balanceModule;

        public SkeletonTrackingData(int capturyId, string skeletonName, int playerNumber)
        {
            this.capturyId = capturyId;
            this.skeletonName = skeletonName;
            this.playerNumber = playerNumber;
            this.playerLabel = $"Player {playerNumber}";
            
            joints = new Dictionary<string, Transform>();
            modules = new List<MotionTrackingModule>();
            isCalibrated = false;
        }
    }

    #endregion

    #region Private Variables

    private CapturyNetworkPlugin networkPlugin;
    
    private Dictionary<int, SkeletonTrackingData> trackedSkeletons = new Dictionary<int, SkeletonTrackingData>();
    private int nextPlayerNumber = 1;

    private readonly object skeletonQueueLock = new object();
    private Queue<CapturySkeleton> skeletonsToAdd = new Queue<CapturySkeleton>();
    private Queue<CapturySkeleton> skeletonsToRemove = new Queue<CapturySkeleton>();

    private static MultiplayerMotionTrackingManager instance;
    public static MultiplayerMotionTrackingManager Instance => instance;

    #endregion

    #region Public Properties

    public MotionTrackingConfiguration Config => config;
    public int TrackedSkeletonCount => trackedSkeletons.Count;
    public bool AutomaticCalibration => automaticCalibration;
    public int MaxPlayers => maxPlayers;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        SetupSingleton();
        ValidateConfiguration();
    }

    void Start()
    {
        InitializeCapturyConnection();
    }

    void Update()
    {
        UpdateAllSkeletons();
    }

    void OnDestroy()
    {
        if (enableDebugLogging) Debug.Log("MultiplayerMotionTrackingManager: OnDestroy() called");
        CleanupAllSkeletons();
        DisconnectFromCaptury();
    }

    #endregion

    #region Initialization

    private void SetupSingleton()
    {
        if (dontDestroyOnLoad)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            instance = this;
        }

        if (enableDebugLogging)
            Debug.Log("MultiplayerMotionTrackingManager: Singleton initialized");
    }

    private void ValidateConfiguration()
    {
        if (config == null)
        {
            Debug.LogWarning("MultiplayerMotionTrackingManager: No configuration assigned, creating default");
            config = ScriptableObject.CreateInstance<MotionTrackingConfiguration>();
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Loaded configuration '{config.configurationName}'");
    }

    private void InitializeCapturyConnection()
    {
        if (enableDebugLogging) Debug.Log("MultiplayerMotionTrackingManager: Looking for CapturyNetworkPlugin...");

        networkPlugin = FindObjectOfType<CapturyNetworkPlugin>();
        if (networkPlugin != null)
        {
            networkPlugin.SkeletonFound -= OnSkeletonFound;
            networkPlugin.SkeletonFound += OnSkeletonFound;
            networkPlugin.SkeletonLost -= OnSkeletonLost;
            networkPlugin.SkeletonLost += OnSkeletonLost;

            if (enableDebugLogging)
                Debug.Log("MultiplayerMotionTrackingManager: Connected to CapturyNetworkPlugin");
        }
        else
        {
            Debug.LogError("MultiplayerMotionTrackingManager: Could not find CapturyNetworkPlugin! Make sure it exists in the scene.");
        }
    }

    #endregion

    #region Skeleton Event Handlers

    private void OnSkeletonFound(CapturySkeleton skeleton)
    {
        lock (skeletonQueueLock)
        {
            skeletonsToAdd.Enqueue(skeleton);
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Queued new skeleton - ID: {skeleton.id}, Name: {skeleton.name}");
    }

    private void OnSkeletonLost(CapturySkeleton skeleton)
    {
        lock (skeletonQueueLock)
        {
            skeletonsToRemove.Enqueue(skeleton);
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Queued skeleton removal - ID: {skeleton.id}");
    }

    #endregion

    #region Skeleton Setup

    private void ProcessSkeletonQueues()
    {
        lock (skeletonQueueLock)
        {
            while (skeletonsToAdd.Count > 0)
            {
                CapturySkeleton skeleton = skeletonsToAdd.Dequeue();
                ProcessSkeletonFound(skeleton);
            }

            while (skeletonsToRemove.Count > 0)
            {
                CapturySkeleton skeleton = skeletonsToRemove.Dequeue();
                CleanupSkeleton(skeleton.id);
            }
        }
    }

    private void ProcessSkeletonFound(CapturySkeleton skeleton)
    {
        if (trackedSkeletons.Count >= maxPlayers)
        {
            Debug.LogWarning($"MultiplayerMotionTrackingManager: Max players ({maxPlayers}) reached. Ignoring skeleton {skeleton.id}");
            return;
        }

        if (trackedSkeletons.ContainsKey(skeleton.id))
        {
            Debug.LogWarning($"MultiplayerMotionTrackingManager: Skeleton {skeleton.id} already being tracked!");
            return;
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Processing new skeleton - ID: {skeleton.id}, Name: {skeleton.name}");

        SkeletonTrackingData skeletonData = new SkeletonTrackingData(skeleton.id, skeleton.name, nextPlayerNumber);
        nextPlayerNumber++;

        trackedSkeletons.Add(skeleton.id, skeletonData);

        skeleton.OnSkeletonSetupComplete += OnIndividualSkeletonSetupComplete;

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: {skeletonData.playerLabel} waiting for skeleton setup to complete...");
    }

    private void OnIndividualSkeletonSetupComplete(CapturySkeleton skeleton)
    {
        if (!trackedSkeletons.ContainsKey(skeleton.id))
        {
            Debug.LogWarning($"MultiplayerMotionTrackingManager: Skeleton {skeleton.id} setup complete but not in tracked list!");
            return;
        }

        SkeletonTrackingData skeletonData = trackedSkeletons[skeleton.id];

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Skeleton setup complete for {skeletonData.playerLabel}, building joint lookup...");

        BuildJointLookup(skeleton, skeletonData);

        CreateInputDevice(skeletonData);

        CreateModules(skeletonData);

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: {skeletonData.playerLabel} initialized with {skeletonData.modules.Count} modules");

        if (automaticCalibration)
        {
            StartCoroutine(CalibrateSkeleton(skeleton.id));
        }
    }

    private void BuildJointLookup(CapturySkeleton skeleton, SkeletonTrackingData skeletonData)
    {
        skeletonData.joints.Clear();
        
        if (skeleton.Reference == null)
        {
            Debug.LogWarning($"MultiplayerMotionTrackingManager: Skeleton {skeleton.id} has no reference skeleton yet");
            return;
        }

        foreach (var joint in skeleton.joints)
        {
            if (joint.transform != null)
            {
                skeletonData.joints[joint.name] = joint.transform;
            }
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Built joint lookup with {skeletonData.joints.Count} joints for {skeletonData.playerLabel}");
    }

    private void CreateInputDevice(SkeletonTrackingData skeletonData)
    {
        string deviceName = $"CapturyInput_{skeletonData.playerLabel.Replace(" ", "")}";
        
        skeletonData.inputDevice = InputSystem.AddDevice<CapturyInput>(deviceName);
        
        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Created input device '{deviceName}' for {skeletonData.playerLabel}");
    }

    private void CreateModules(SkeletonTrackingData skeletonData)
    {
        skeletonData.modules.Clear();

        GameObject moduleParent = new GameObject($"{skeletonData.playerLabel}_Modules");
        moduleParent.transform.SetParent(transform);

        if (config.enableTorsoModule)
        {
            GameObject torsoObj = new GameObject("TorsoTrackingModule");
            torsoObj.transform.SetParent(moduleParent.transform);
            skeletonData.torsoModule = torsoObj.AddComponent<TorsoTrackingModule>();
            skeletonData.torsoModule.Initialize(this);
            skeletonData.modules.Add(skeletonData.torsoModule);
        }

        if (config.enableFootModule)
        {
            GameObject footObj = new GameObject("FootTrackingModule");
            footObj.transform.SetParent(moduleParent.transform);
            skeletonData.footModule = footObj.AddComponent<FootTrackingModule>();
            skeletonData.footModule.Initialize(this);
            skeletonData.modules.Add(skeletonData.footModule);
        }

        if (config.enableArmsModule)
        {
            GameObject armsObj = new GameObject("ArmTrackingModule");
            armsObj.transform.SetParent(moduleParent.transform);
            skeletonData.armsModule = armsObj.AddComponent<ArmTrackingModule>();
            skeletonData.armsModule.Initialize(this);
            skeletonData.modules.Add(skeletonData.armsModule);
        }

        if (config.enableHeadModule)
        {
            GameObject headObj = new GameObject("HeadTrackingModule");
            headObj.transform.SetParent(moduleParent.transform);
            skeletonData.headModule = headObj.AddComponent<HeadTrackingModule>();
            skeletonData.headModule.Initialize(this);
            skeletonData.modules.Add(skeletonData.headModule);
        }

        if (config.enableBalanceModule)
        {
            GameObject balanceObj = new GameObject("BalanceTrackingModule");
            balanceObj.transform.SetParent(moduleParent.transform);
            skeletonData.balanceModule = balanceObj.AddComponent<BalanceTrackingModule>();
            skeletonData.balanceModule.Initialize(this);
            skeletonData.modules.Add(skeletonData.balanceModule);
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Created {skeletonData.modules.Count} modules for {skeletonData.playerLabel}");
    }

    #endregion

    #region Calibration

    private IEnumerator CalibrateSkeleton(int skeletonId)
    {
        if (!trackedSkeletons.ContainsKey(skeletonId))
        {
            Debug.LogError($"MultiplayerMotionTrackingManager: Cannot calibrate - skeleton {skeletonId} not found");
            yield break;
        }

        SkeletonTrackingData skeletonData = trackedSkeletons[skeletonId];

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Starting calibration for {skeletonData.playerLabel} in {calibrationDelayPerSkeleton} seconds...");

        yield return new WaitForSeconds(calibrationDelayPerSkeleton);

        if (!trackedSkeletons.ContainsKey(skeletonId))
        {
            Debug.LogWarning($"MultiplayerMotionTrackingManager: Skeleton {skeletonId} was lost during calibration delay");
            yield break;
        }

        Transform[] joints = skeletonData.joints.Values.ToArray();

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Calibrating {skeletonData.modules.Count} modules for {skeletonData.playerLabel}...");

        foreach (var module in skeletonData.modules)
        {
            if (module.HasRequiredJoints(joints))
            {
                module.Calibrate(joints);
                if (enableDebugLogging)
                    Debug.Log($"MultiplayerMotionTrackingManager: Calibrated {module.GetType().Name} for {skeletonData.playerLabel}");
            }
            else
            {
                Debug.LogWarning($"MultiplayerMotionTrackingManager: {module.GetType().Name} missing required joints for {skeletonData.playerLabel}");
            }
        }

        skeletonData.isCalibrated = true;
        skeletonData.calibrationCoroutine = null;

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: {skeletonData.playerLabel} calibration complete!");
    }

    #endregion

    #region Update Loop

    private void UpdateAllSkeletons()
    {
        ProcessSkeletonQueues();

        foreach (var kvp in trackedSkeletons)
        {
            UpdateSkeleton(kvp.Key, kvp.Value);
        }
    }

    private void UpdateSkeleton(int skeletonId, SkeletonTrackingData skeletonData)
    {
        if (!skeletonData.isCalibrated || skeletonData.inputDevice == null)
        {
            if (enableDebugLogging && Time.frameCount % 600 == 0)
            {
                Debug.Log($"MultiplayerMotionTrackingManager: {skeletonData.playerLabel} not ready - Calibrated: {skeletonData.isCalibrated}");
            }
            return;
        }

        CapturyInputState state = new CapturyInputState();

        Transform[] joints = skeletonData.joints.Values.ToArray();

        foreach (var module in skeletonData.modules)
        {
            module.UpdateTracking(ref state, joints);
        }

        InputSystem.QueueStateEvent(skeletonData.inputDevice, state);
    }

    #endregion

    #region Cleanup

    private void CleanupSkeleton(int skeletonId)
    {
        if (!trackedSkeletons.ContainsKey(skeletonId))
            return;

        SkeletonTrackingData skeletonData = trackedSkeletons[skeletonId];

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Cleaning up {skeletonData.playerLabel}");

        if (skeletonData.calibrationCoroutine != null)
        {
            StopCoroutine(skeletonData.calibrationCoroutine);
        }

        foreach (var module in skeletonData.modules)
        {
            if (module != null && module.gameObject != null)
            {
                DestroyImmediate(module.gameObject.transform.parent.gameObject); // Destroy module parent
            }
        }

        if (skeletonData.inputDevice != null && !dontDestroyOnLoad)
        {
            InputSystem.RemoveDevice(skeletonData.inputDevice);
        }

        trackedSkeletons.Remove(skeletonId);

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: {skeletonData.playerLabel} cleanup complete");
    }

    private void CleanupAllSkeletons()
    {
        if (enableDebugLogging)
            Debug.Log("MultiplayerMotionTrackingManager: Cleaning up all skeletons");

        List<int> skeletonIds = new List<int>(trackedSkeletons.Keys);
        foreach (int skeletonId in skeletonIds)
        {
            CleanupSkeleton(skeletonId);
        }
    }

    private void DisconnectFromCaptury()
    {
        if (networkPlugin != null)
        {
            networkPlugin.SkeletonFound -= OnSkeletonFound;
            networkPlugin.SkeletonLost -= OnSkeletonLost;
        }
    }

    #endregion

    #region Public API - Skeleton Access

    public List<int> GetAllSkeletonIds()
    {
        return new List<int>(trackedSkeletons.Keys);
    }

    public bool TryGetSkeletonById(int skeletonId, out int playerNumber, out string playerLabel)
    {
        if (trackedSkeletons.TryGetValue(skeletonId, out SkeletonTrackingData data))
        {
            playerNumber = data.playerNumber;
            playerLabel = data.playerLabel;
            return true;
        }
        playerNumber = -1;
        playerLabel = null;
        return false;
    }

    public int GetSkeletonIdByPlayerNumber(int playerNumber)
    {
        foreach (var kvp in trackedSkeletons)
        {
            if (kvp.Value.playerNumber == playerNumber)
                return kvp.Key;
        }
        return -1;
    }

    public List<int> GetAllPlayerNumbers()
    {
        return trackedSkeletons.Values.Select(s => s.playerNumber).ToList();
    }

    public CapturyInput GetInputDeviceByPlayerNumber(int playerNumber)
    {
        int skeletonId = GetSkeletonIdByPlayerNumber(playerNumber);
        if (skeletonId != -1 && trackedSkeletons.TryGetValue(skeletonId, out SkeletonTrackingData data))
        {
            return data.inputDevice;
        }
        return null;
    }

    public bool IsSkeletonCalibrated(int skeletonId)
    {
        return trackedSkeletons.TryGetValue(skeletonId, out SkeletonTrackingData data) && data.isCalibrated;
    }

    #endregion

    #region Public API - Module Access

    public T GetModule<T>(int playerNumber) where T : MotionTrackingModule
    {
        int skeletonId = GetSkeletonIdByPlayerNumber(playerNumber);
        if (skeletonId == -1) return null;

        if (trackedSkeletons.TryGetValue(skeletonId, out SkeletonTrackingData data))
        {
            return data.modules.OfType<T>().FirstOrDefault();
        }
        return null;
    }

    public List<T> GetAllModules<T>() where T : MotionTrackingModule
    {
        List<T> modules = new List<T>();
        foreach (var skeletonData in trackedSkeletons.Values)
        {
            T module = skeletonData.modules.OfType<T>().FirstOrDefault();
            if (module != null)
                modules.Add(module);
        }
        return modules;
    }

    public BalanceTrackingModule GetBalanceModule(int playerNumber) => GetModule<BalanceTrackingModule>(playerNumber);
    public FootTrackingModule GetFootModule(int playerNumber) => GetModule<FootTrackingModule>(playerNumber);
    public TorsoTrackingModule GetTorsoModule(int playerNumber) => GetModule<TorsoTrackingModule>(playerNumber);
    public ArmTrackingModule GetArmsModule(int playerNumber) => GetModule<ArmTrackingModule>(playerNumber);
    public HeadTrackingModule GetHeadModule(int playerNumber) => GetModule<HeadTrackingModule>(playerNumber);

    public List<BalanceTrackingModule> GetAllBalanceModules() => GetAllModules<BalanceTrackingModule>();
    public List<FootTrackingModule> GetAllFootModules() => GetAllModules<FootTrackingModule>();
    public List<TorsoTrackingModule> GetAllTorsoModules() => GetAllModules<TorsoTrackingModule>();
    public List<ArmTrackingModule> GetAllArmsModules() => GetAllModules<ArmTrackingModule>();
    public List<HeadTrackingModule> GetAllHeadModules() => GetAllModules<HeadTrackingModule>();

    #endregion

    #region Public API - Joint Access

    public Transform GetJointByName(int skeletonId, string jointName)
    {
        if (trackedSkeletons.TryGetValue(skeletonId, out SkeletonTrackingData data))
        {
            data.joints.TryGetValue(jointName, out Transform joint);
            return joint;
        }
        return null;
    }

    public Transform GetJointByName(string jointName)
    {
        // old, deprecated
        return null;
    }

    #endregion

    #region Public API - Calibration Control

    public void RecalibrateSkeleton(int skeletonId)
    {
        if (!trackedSkeletons.ContainsKey(skeletonId))
        {
            Debug.LogError($"MultiplayerMotionTrackingManager: Cannot recalibrate - skeleton {skeletonId} not found");
            return;
        }

        SkeletonTrackingData skeletonData = trackedSkeletons[skeletonId];

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Manual recalibration requested for {skeletonData.playerLabel}");

        if (skeletonData.calibrationCoroutine != null)
        {
            StopCoroutine(skeletonData.calibrationCoroutine);
        }

        skeletonData.isCalibrated = false;

        skeletonData.calibrationCoroutine = StartCoroutine(CalibrateSkeleton(skeletonId));
    }

    public void RecalibratePlayer(int playerNumber)
    {
        int skeletonId = GetSkeletonIdByPlayerNumber(playerNumber);
        if (skeletonId != -1)
        {
            RecalibrateSkeleton(skeletonId);
        }
        else
        {
            Debug.LogError($"MultiplayerMotionTrackingManager: Cannot recalibrate - player {playerNumber} not found");
        }
    }

    public void RecalibrateAllSkeletons()
    {
        if (enableDebugLogging)
            Debug.Log("MultiplayerMotionTrackingManager: Recalibrating all skeletons");

        foreach (int skeletonId in trackedSkeletons.Keys.ToList())
        {
            RecalibrateSkeleton(skeletonId);
        }
    }

    #endregion

    #region Public API - Configuration

    public void SwapConfiguration(MotionTrackingConfiguration newConfig)
    {
        if (newConfig == null)
        {
            Debug.LogError("MultiplayerMotionTrackingManager: Cannot swap to null configuration!");
            return;
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Swapping configuration to '{newConfig.configurationName}'");

        var oldSkeletons = new Dictionary<int, SkeletonTrackingData>(trackedSkeletons);

        foreach (var skeletonData in oldSkeletons.Values)
        {
            foreach (var module in skeletonData.modules)
            {
                if (module != null && module.gameObject != null)
                {
                    DestroyImmediate(module.gameObject.transform.parent.gameObject);
                }
            }
            skeletonData.modules.Clear();
            skeletonData.isCalibrated = false;
        }

        config = newConfig;

        foreach (var skeletonData in oldSkeletons.Values)
        {
            CreateModules(skeletonData);
            
            if (automaticCalibration)
            {
                StartCoroutine(CalibrateSkeleton(skeletonData.capturyId));
            }
        }

        if (enableDebugLogging)
            Debug.Log($"MultiplayerMotionTrackingManager: Configuration swap complete");
    }

    #endregion
}