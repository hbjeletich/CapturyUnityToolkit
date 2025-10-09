using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Captury;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class MotionTrackingManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MotionTrackingConfiguration config;
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool enableDebugLogging = true;

    // core components
    private CapturyInput capturyInput;
    private CapturyNetworkPlugin networkPlugin;
    private Dictionary<string, Transform> jointLookup = new Dictionary<string, Transform>();

    // modules
    private TorsoTrackingModule torsoModule;
    private FootTrackingModule footModule;
    private ArmTrackingModule armsModule;
    private HeadTrackingModule headModule;
    private BalanceTrackingModule balanceModule;
    private List<MotionTrackingModule> allModules;

    // state
    private bool isSystemCalibrated = false;

    // singleton
    private static MotionTrackingManager instance;
    public static MotionTrackingManager Instance => instance;

    // public config
    public MotionTrackingConfiguration Config => config;

    #region Awake, Start, Update, Destroy

    void Awake()
    {
        SetupSingleton();
        LoadDefaultConfiguration();
    }

    void Start()
    {
        InitializeCapturyInput();
        FindNetworkPlugin();
    }

    void Update()
    {
        if (isSystemCalibrated && capturyInput != null)
        {
            UpdateAllModules();
        }
        else
        {
            if (enableDebugLogging && Time.frameCount % 600 == 0)
            {
                Debug.Log($"MotionTrackingManager: Not updating modules - Calibrated: {isSystemCalibrated}, CapturyInput present: {capturyInput != null}");
            }
        }
    }

    void OnDestroy()
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: OnDestroy() called");
        CleanupSystem();
    }

    #endregion
    #region Setup and Configuration

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
    }

    public void LoadConfiguration(MotionTrackingConfiguration newConfig)
    {
        if (newConfig == null)
        {
            Debug.LogError("MotionTrackingManager: Cannot load null configuration!");
            return;
        }

        config = newConfig;

        if (enableDebugLogging)
            Debug.Log($"MotionTrackingManager: Loading configuration '{config.configurationName}'");

        // initialize modules - this will create allModules if needed
        InitializeModules();

        if (isSystemCalibrated)
        {
            Recalibrate();
        }

        if (enableDebugLogging)
            Debug.Log($"MotionTrackingManager: Loaded configuration '{config.configurationName}' with {allModules?.Count ?? 0} active modules");
    }

    private void LoadDefaultConfiguration()
    {
        if (config != null)
        {
            LoadConfiguration(config);
        }
        else
        {
            Debug.LogWarning("MotionTrackingManager: No configuration assigned, using runtime defaults");
            config = ScriptableObject.CreateInstance<MotionTrackingConfiguration>();
            LoadConfiguration(config);
        }
    }

    private void InitializeModules()
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Initializing modules...");

        if (allModules == null)
            allModules = new List<MotionTrackingModule>();

        if (config.enableTorsoModule)
        {
            GameObject torsoObj = new GameObject("TorsoTrackingModule");
            torsoObj.transform.SetParent(transform);
            torsoModule = torsoObj.AddComponent<TorsoTrackingModule>();
            allModules.Add(torsoModule);
        }

        if (config.enableFootModule)
        {
            GameObject footObj = new GameObject("FootTrackingModule");
            footObj.transform.SetParent(transform);
            footModule = footObj.AddComponent<FootTrackingModule>();
            allModules.Add(footModule);
        }

        if (config.enableArmsModule)
        {
            GameObject armsObj = new GameObject("ArmTrackingModule");
            armsObj.transform.SetParent(transform);
            armsModule = armsObj.AddComponent<ArmTrackingModule>();
            allModules.Add(armsModule);
        }

        if (config.enableHeadModule)
        {
            GameObject headObj = new GameObject("HeadTrackingModule");
            headObj.transform.SetParent(transform);
            headModule = headObj.AddComponent<HeadTrackingModule>();
            allModules.Add(headModule);
        }

        if (config.enableBalanceModule)
        {
            GameObject balanceObj = new GameObject("BalanceTrackingModule");
            balanceObj.transform.SetParent(transform);
            balanceModule = balanceObj.AddComponent<BalanceTrackingModule>();
            allModules.Add(balanceModule);
        }

        foreach (var module in allModules)
        {
            module.Initialize(this);
            if (enableDebugLogging)
                Debug.Log($"MotionTrackingManager: Initialized {module.GetType().Name} - Enabled: {module.IsEnabled}");
        }
    }

    private void InitializeCapturyInput()
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Initializing CapturyInput...");

        capturyInput = InputSystem.GetDevice<CapturyInput>();
        if (capturyInput == null)
        {
            CapturyInput.Register();
            capturyInput = InputSystem.AddDevice<CapturyInput>();
            if (enableDebugLogging) Debug.Log("MotionTrackingManager: Created new CapturyInput device");
        }
        else
        {
            if (enableDebugLogging) Debug.Log("MotionTrackingManager: Using existing CapturyInput device");
        }
    }

    private void FindNetworkPlugin()
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Looking for CapturyNetworkPlugin...");

        networkPlugin = FindObjectOfType<CapturyNetworkPlugin>();
        if (networkPlugin != null)
        {
            networkPlugin.SkeletonFound -= OnSkeletonFound;
            networkPlugin.SkeletonFound += OnSkeletonFound;
            if (enableDebugLogging) Debug.Log("MotionTrackingManager: Connected to CapturyNetworkPlugin");
        }
        else
        {
            Debug.LogError("MotionTrackingManager: Could not find CapturyNetworkPlugin! Make sure it exists in the scene.");
        }
    }

    #endregion
    #region Skeleton and Calibration

    private void OnSkeletonFound(CapturySkeleton skeleton)
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Skeleton found, setting up...");
        skeleton.OnSkeletonSetupComplete += OnSkeletonSetupComplete;
    }

    private void OnSkeletonSetupComplete(CapturySkeleton skeleton)
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Skeleton setup complete, building joint lookup...");
        BuildJointLookup(skeleton);
        StartCoroutine(CalibrateSystem());
    }

    private void BuildJointLookup(CapturySkeleton skeleton)
    {
        jointLookup.Clear();
        foreach (var joint in skeleton.joints)
        {
            jointLookup[joint.name] = joint.transform;
            if (enableDebugLogging) Debug.Log($"MotionTrackingManager: Added joint: {joint.name}");
        }

        if (enableDebugLogging) Debug.Log($"MotionTrackingManager: Built joint lookup with {jointLookup.Count} joints");
    }

    private IEnumerator CalibrateSystem()
    {
        if (enableDebugLogging) Debug.Log($"MotionTrackingManager: Starting calibration in {config.calibrationDelay} seconds...");

        yield return new WaitForSeconds(config.calibrationDelay);

        Transform[] joints = new Transform[jointLookup.Count];
        jointLookup.Values.CopyTo(joints, 0);

        if (enableDebugLogging) Debug.Log($"MotionTrackingManager: Calibrating {allModules.Count} modules...");

        foreach (var module in allModules)
        {
            if (module.HasRequiredJoints(joints))
            {
                if (enableDebugLogging) Debug.Log($"MotionTrackingManager: Calibrating {module.GetType().Name}...");
                module.Calibrate(joints);
            }
            else
            {
                Debug.LogWarning($"MotionTrackingManager: {module.GetType().Name} missing required joints!");
            }
        }

        isSystemCalibrated = true;
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: System calibrated and ready!");
    }

    private void CleanupSystem()
    {
        if (networkPlugin != null)
            networkPlugin.SkeletonFound -= OnSkeletonFound;

        if (capturyInput != null && !dontDestroyOnLoad)
            InputSystem.RemoveDevice(capturyInput);

        // clean up module game objects
        if (allModules != null)
        {
            foreach (var module in allModules)
            {
                if (module != null && module.gameObject != null)
                {
                    DestroyImmediate(module.gameObject);
                }
            }
            allModules.Clear();
        }
    }

    #endregion
    #region Tracking Updates

    private void UpdateAllModules()
    {
        CapturyInputState state = new CapturyInputState();

        Transform[] joints = new Transform[jointLookup.Count];
        jointLookup.Values.CopyTo(joints, 0);

        foreach (var module in allModules)
        {
            module.UpdateTracking(ref state, joints);
        }

        InputSystem.QueueStateEvent(capturyInput, state);
    }

    #endregion
    #region Public Methods

    public Transform GetJointByName(string jointName)
    {
        jointLookup.TryGetValue(jointName, out Transform joint);
        return joint;
    }

    public void Recalibrate()
    {
        if (enableDebugLogging) Debug.Log("MotionTrackingManager: Manual recalibration requested");
        isSystemCalibrated = false;
        StartCoroutine(CalibrateSystem());
    }

    // public accessors for modules
    public TorsoTrackingModule GetTorsoModule() => torsoModule;
    public FootTrackingModule GetFootModule() => footModule;
    public ArmTrackingModule GetArmsModule() => armsModule;
    public HeadTrackingModule GetHeadModule() => headModule;
    public BalanceTrackingModule GetBalanceModule() => balanceModule;

    // quick access to module states
    public bool IsTorsoModuleEnabled => config.enableTorsoModule && torsoModule?.IsCalibrated == true;
    public bool IsFootModuleEnabled => config.enableFootModule && footModule?.IsCalibrated == true;
    public bool IsArmsModuleEnabled => config.enableArmsModule && armsModule?.IsCalibrated == true;
    public bool IsHeadModuleEnabled => config.enableHeadModule && headModule?.IsCalibrated == true;
    public bool IsBalanceModuleEnabled => config.enableBalanceModule && balanceModule?.IsCalibrated == true;


    #endregion
}