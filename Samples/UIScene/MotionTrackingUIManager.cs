#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class MotionTrackingUIManager : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private CapturyInputActions inputActions;

    [Header("Module UI Panels")]
    [SerializeField] private ArmTrackingUI armsUI;
    [SerializeField] private HeadTrackingUI headUI;
    [SerializeField] private TorsoTrackingUI torsoUI;
    [SerializeField] private FootTrackingUI footUI;
    [SerializeField] private BalanceTrackingUI balanceUI;

    [Header("System Status")]
    [SerializeField] private TextMeshProUGUI systemStatusText;
    [SerializeField] private Image calibrationIndicator;
    [SerializeField] private Color calibratedColor = Color.green;
    [SerializeField] private Color notCalibratedColor = Color.red;

    [Header("Settings")]
    [SerializeField] private bool showInactiveModules = false;
    [SerializeField] private float updateRate = 60f;
    [SerializeField] private bool autoCreateInputActions = true;

    private float updateTimer = 0f;
    private bool isCalibrated = false;

    void Awake()
    {
        // Create input actions if needed
        if (inputActions == null && autoCreateInputActions)
        {
            inputActions = new CapturyInputActions();
        }
    }

    void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
            SubscribeToInputEvents();
        }
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            UnsubscribeFromInputEvents();
            inputActions.Disable();
        }
    }

    void Start()
    {
        InitializeUI();
        // Assume calibrated after a short delay
        Invoke(nameof(SetCalibrated), 2f);
    }

    void Update()
    {
        updateTimer += Time.deltaTime;
        if (updateTimer >= 1f / updateRate)
        {
            UpdateAllUI();
            updateTimer = 0f;
        }

        UpdateSystemStatus();
    }

    private void SetCalibrated()
    {
        isCalibrated = true;
    }

    private void InitializeUI()
    {
        // Show/hide panels based on settings
        if (armsUI != null)
            armsUI.gameObject.SetActive(true);

        if (headUI != null)
            headUI.gameObject.SetActive(true);

        if (torsoUI != null)
            torsoUI.gameObject.SetActive(true);

        if (footUI != null)
            footUI.gameObject.SetActive(true);

        if (balanceUI != null)
            balanceUI.gameObject.SetActive(true);
    }

    private void SubscribeToInputEvents()
    {
        // Arms
        inputActions.Arms.LeftHandRaised.performed += OnLeftHandRaisedChanged;
        inputActions.Arms.LeftHandRaised.canceled += OnLeftHandRaisedChanged;
        inputActions.Arms.RightHandRaised.performed += OnRightHandRaisedChanged;
        inputActions.Arms.RightHandRaised.canceled += OnRightHandRaisedChanged;

        // Head direction
        inputActions.Head.HeadNodding.performed += OnHeadDirectionChanged;
        inputActions.Head.HeadNodding.canceled += OnHeadDirectionChanged;
        inputActions.Head.HeadShaking.performed += OnHeadDirectionChanged;
        inputActions.Head.HeadShaking.canceled += OnHeadDirectionChanged;

        // Torso
        inputActions.Torso.WeightShiftLeft.performed += OnWeightShiftChanged;
        inputActions.Torso.WeightShiftLeft.canceled += OnWeightShiftChanged;
        inputActions.Torso.WeightShiftRight.performed += OnWeightShiftChanged;
        inputActions.Torso.WeightShiftRight.canceled += OnWeightShiftChanged;
        inputActions.Torso.IsBentOver.performed += OnPostureChanged;
        inputActions.Torso.IsBentOver.canceled += OnPostureChanged;

        // Foot
        inputActions.Foot.LeftStep.performed += OnLeftStepDetected;
        inputActions.Foot.RightStep.performed += OnRightStepDetected;

        // Balance
        inputActions.Balance.BalanceLost.performed += OnBalanceStateChanged;
        inputActions.Balance.BalanceRegained.performed += OnBalanceStateChanged;
    }

    private void UnsubscribeFromInputEvents()
    {
        // Arms
        inputActions.Arms.LeftHandRaised.performed -= OnLeftHandRaisedChanged;
        inputActions.Arms.LeftHandRaised.canceled -= OnLeftHandRaisedChanged;
        inputActions.Arms.RightHandRaised.performed -= OnRightHandRaisedChanged;
        inputActions.Arms.RightHandRaised.canceled -= OnRightHandRaisedChanged;

        // Head
        inputActions.Head.HeadNodding.performed -= OnHeadDirectionChanged;
        inputActions.Head.HeadNodding.canceled -= OnHeadDirectionChanged;
        inputActions.Head.HeadShaking.performed -= OnHeadDirectionChanged;
        inputActions.Head.HeadShaking.canceled -= OnHeadDirectionChanged;

        // Torso
        inputActions.Torso.WeightShiftLeft.performed -= OnWeightShiftChanged;
        inputActions.Torso.WeightShiftLeft.canceled -= OnWeightShiftChanged;
        inputActions.Torso.WeightShiftRight.performed -= OnWeightShiftChanged;
        inputActions.Torso.WeightShiftRight.canceled -= OnWeightShiftChanged;
        inputActions.Torso.IsBentOver.performed -= OnPostureChanged;
        inputActions.Torso.IsBentOver.canceled -= OnPostureChanged;

        // Foot
        inputActions.Foot.LeftStep.performed -= OnLeftStepDetected;
        inputActions.Foot.RightStep.performed -= OnRightStepDetected;

        // Balance
        inputActions.Balance.BalanceLost.performed -= OnBalanceStateChanged;
        inputActions.Balance.BalanceRegained.performed -= OnBalanceStateChanged;
    }

    private void UpdateAllUI()
    {
        if (!isCalibrated || inputActions == null) return;

        // Update Arms UI
        if (armsUI != null)
        {
            bool leftRaised = inputActions.Arms.LeftHandRaised.ReadValue<float>() > 0.5f;
            bool rightRaised = inputActions.Arms.RightHandRaised.ReadValue<float>() > 0.5f;
            Vector3 leftPos = inputActions.Arms.LeftHandPosition.ReadValue<Vector3>();
            Vector3 rightPos = inputActions.Arms.RightHandPosition.ReadValue<Vector3>();
            armsUI.UpdateDisplay(leftRaised, rightRaised, leftPos, rightPos);
        }

        // Update Head UI
        if (headUI != null)
        {
            Vector3 rotation = inputActions.Head.HeadRotation.ReadValue<Vector3>();
            bool nodding = inputActions.Head.HeadNodding.ReadValue<float>() > 0.5f;
            bool shaking = inputActions.Head.HeadShaking.ReadValue<float>() > 0.5f;
            
            // Derive direction states from rotation
            bool up = rotation.z < -5f;
            bool down = rotation.z > 5f;
            bool left = rotation.y < -20f;
            bool right = rotation.y > 20f;
            
            headUI.UpdateDisplay(up, down, left, right, rotation);
        }

        // Update Torso UI
        if (torsoUI != null)
        {
            bool shiftLeft = inputActions.Torso.WeightShiftLeft.ReadValue<float>() > 0.5f;
            bool shiftRight = inputActions.Torso.WeightShiftRight.ReadValue<float>() > 0.5f;
            bool bentOver = inputActions.Torso.IsBentOver.ReadValue<float>() > 0.5f;
            float weightShiftValue = inputActions.Torso.WeightShiftX.ReadValue<float>();
            torsoUI.UpdateDisplay(shiftLeft, shiftRight, bentOver, weightShiftValue);
        }

        // Update Foot UI
        if (footUI != null)
        {
            bool isWalking = inputActions.Foot.IsWalking.ReadValue<float>() > 0.5f;
            float cadence = inputActions.Foot.Cadence.ReadValue<float>();
            float walkSpeed = inputActions.Foot.WalkSpeed.ReadValue<float>();
            bool leftStep = inputActions.Foot.LeftStep.ReadValue<float>() > 0.5f;
            bool rightStep = inputActions.Foot.RightStep.ReadValue<float>() > 0.5f;
            footUI.UpdateDisplay(isWalking, cadence, walkSpeed, leftStep, rightStep);
        }

        // Update Balance UI
        if (balanceUI != null)
        {
            bool balanced = inputActions.Balance.IsBalanced.ReadValue<float>() > 0.5f;
            Vector3 comPosition = inputActions.Balance.CenterOfMassPosition.ReadValue<Vector3>();
            float lateralSway = inputActions.Balance.LateralSway.ReadValue<float>();
            float apSway = inputActions.Balance.AnteriorPosteriorSway.ReadValue<float>();
            float swayMagnitude = inputActions.Balance.SwayMagnitude.ReadValue<float>();
            balanceUI.UpdateDisplay(balanced, comPosition, lateralSway, apSway, swayMagnitude);
        }
    }

    private void UpdateSystemStatus()
    {
        if (systemStatusText != null)
        {
            systemStatusText.text = isCalibrated ? 
                "CALIBRATED | TRACKING ACTIVE" : 
                "CALIBRATING...";
        }

        if (calibrationIndicator != null)
        {
            calibrationIndicator.color = isCalibrated ? 
                calibratedColor : notCalibratedColor;
        }
    }

    // Event callbacks for visual feedback
    private void OnLeftHandRaisedChanged(InputAction.CallbackContext context)
    {
        if (armsUI != null)
            armsUI.TriggerLeftHandFeedback(context.ReadValue<float>() > 0.5f);
    }

    private void OnRightHandRaisedChanged(InputAction.CallbackContext context)
    {
        if (armsUI != null)
            armsUI.TriggerRightHandFeedback(context.ReadValue<float>() > 0.5f);
    }

    private void OnHeadDirectionChanged(InputAction.CallbackContext context)
    {
        if (headUI != null)
            headUI.TriggerDirectionFeedback();
    }

    private void OnWeightShiftChanged(InputAction.CallbackContext context)
    {
        if (torsoUI != null)
            torsoUI.TriggerWeightShiftFeedback();
    }

    private void OnPostureChanged(InputAction.CallbackContext context)
    {
        if (torsoUI != null)
            torsoUI.TriggerPostureFeedback();
    }

    private void OnLeftStepDetected(InputAction.CallbackContext context)
    {
        if (footUI != null)
        {
            footUI.TriggerStepFeedback(false);
        }
    }

    private void OnRightStepDetected(InputAction.CallbackContext context)
    {
        if (footUI != null)
        {
            footUI.TriggerStepFeedback(true);
        }
    }

    private void OnBalanceStateChanged(InputAction.CallbackContext context)
    {
        if (balanceUI != null)
            balanceUI.TriggerBalanceFeedback();
    }

    // Public methods
    public void ToggleModuleUI(string moduleName, bool visible)
    {
        switch (moduleName.ToLower())
        {
            case "arms":
                if (armsUI != null) armsUI.gameObject.SetActive(visible);
                break;
            case "head":
                if (headUI != null) headUI.gameObject.SetActive(visible);
                break;
            case "torso":
                if (torsoUI != null) torsoUI.gameObject.SetActive(visible);
                break;
            case "foot":
                if (footUI != null) footUI.gameObject.SetActive(visible);
                break;
            case "balance":
                if (balanceUI != null) balanceUI.gameObject.SetActive(visible);
                break;
        }
    }

    public void SetCalibrationState(bool calibrated)
    {
        isCalibrated = calibrated;
    }
}
#endif