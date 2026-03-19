#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Automatically builds the complete Motion Tracking UI with one click!
/// Menu: Tools/Motion Tracking/Build Complete UI
/// </summary>
public class MotionTrackingUIBuilder : EditorWindow
{
    [MenuItem("Tools/Motion Tracking/Build Complete UI")]
    public static void BuildCompleteUI()
    {
        if (EditorUtility.DisplayDialog("Build Motion Tracking UI",
            "This will create a complete UI for your motion tracking system.\n\nContinue?",
            "Yes, Build It!", "Cancel"))
        {
            BuildUI();
        }
    }

    private static void BuildUI()
    {
        Debug.Log("=== Building Motion Tracking UI ===");

        // Create or find canvas
        Canvas canvas = FindOrCreateCanvas();

        // Create UI Manager
        GameObject uiManager = CreateUIManager(canvas);
        MotionTrackingUIManager manager = uiManager.GetComponent<MotionTrackingUIManager>();

        // Create system status panel
        CreateSystemStatusPanel(canvas);

        // Build all module panels
        GameObject armsPanel = BuildArmsPanel(canvas);
        GameObject headPanel = BuildHeadPanel(canvas);
        GameObject torsoPanel = BuildTorsoPanel(canvas);
        GameObject footPanel = BuildFootPanel(canvas);
        GameObject balancePanel = BuildBalancePanel(canvas);

        // Assign references to manager using SerializedObject
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("armsUI").objectReferenceValue = armsPanel.GetComponent<ArmTrackingUI>();
        serializedManager.FindProperty("headUI").objectReferenceValue = headPanel.GetComponent<HeadTrackingUI>();
        serializedManager.FindProperty("torsoUI").objectReferenceValue = torsoPanel.GetComponent<TorsoTrackingUI>();
        serializedManager.FindProperty("footUI").objectReferenceValue = footPanel.GetComponent<FootTrackingUI>();
        serializedManager.FindProperty("balanceUI").objectReferenceValue = balancePanel.GetComponent<BalanceTrackingUI>();
        serializedManager.ApplyModifiedProperties();

        // Select the manager in hierarchy
        Selection.activeGameObject = uiManager;

        Debug.Log("=== Motion Tracking UI Built Successfully! ===");
        EditorUtility.DisplayDialog("Success!", 
            "Motion Tracking UI has been created!\n\n" +
            "The UI Manager is selected in the hierarchy.\n" +
            "Press Play to test it out!", 
            "Awesome!");
    }

    #region Canvas Setup

    private static Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("Creating new Canvas...");
            GameObject canvasObj = new GameObject("MotionTrackingCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Canvas created successfully");
        }
        else
        {
            Debug.Log("Using existing Canvas");
        }
        return canvas;
    }

    private static GameObject CreateUIManager(Canvas canvas)
    {
        Debug.Log("Creating UI Manager...");
        GameObject uiManager = new GameObject("MotionTrackingUIManager");
        uiManager.transform.SetParent(canvas.transform, false);
        
        MotionTrackingUIManager manager = uiManager.AddComponent<MotionTrackingUIManager>();
        
        // Set default values
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("autoCreateInputActions").boolValue = true;
        serializedManager.FindProperty("updateRate").floatValue = 60f;
        serializedManager.ApplyModifiedProperties();
        
        return uiManager;
    }

    #endregion

    #region System Status Panel

    private static void CreateSystemStatusPanel(Canvas canvas)
    {
        Debug.Log("Creating System Status Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "SystemStatusPanel", 
            new Vector2(0, -50), new Vector2(500, 100), new Color(0.1f, 0.1f, 0.1f, 0.9f));

        // Status text
        GameObject statusTextObj = CreateText(panel.transform, "StatusText", 
            new Vector2(-100, 0), new Vector2(300, 80), 
            "CALIBRATING...", 32, TextAlignmentOptions.Center, Color.white);

        // Calibration indicator
        GameObject indicator = CreateImage(panel.transform, "CalibrationIndicator", 
            new Vector2(180, 0), new Vector2(60, 60), Color.red);
        
        // Assign to manager (we'll need to find it)
        MotionTrackingUIManager manager = Object.FindObjectOfType<MotionTrackingUIManager>();
        if (manager != null)
        {
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("systemStatusText").objectReferenceValue = statusTextObj.GetComponent<TextMeshProUGUI>();
            serializedManager.FindProperty("calibrationIndicator").objectReferenceValue = indicator.GetComponent<Image>();
            serializedManager.ApplyModifiedProperties();
        }
    }

    #endregion

    #region Arms Panel

    private static GameObject BuildArmsPanel(Canvas canvas)
    {
        Debug.Log("Building Arms Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "ArmsPanel", 
            new Vector2(-700, -150), new Vector2(400, 500), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        
        ArmTrackingUI armUI = panel.AddComponent<ArmTrackingUI>();

        // Title
        CreateText(panel.transform, "Title", new Vector2(0, 210), new Vector2(380, 50), 
            "ARMS TRACKING", 36, TextAlignmentOptions.Center, Color.white);

        // Left hand section
        GameObject leftSection = CreateSection(panel.transform, "LeftHandSection", new Vector2(-100, 0));
        CreateText(leftSection.transform, "Label", new Vector2(0, 150), new Vector2(160, 40), 
            "LEFT HAND", 24, TextAlignmentOptions.Center, Color.cyan);
        
        GameObject leftIcon = CreateImage(leftSection.transform, "Icon", new Vector2(0, 90), 
            new Vector2(80, 80), Color.white);
        
        GameObject leftRaisedIndicator = CreateText(leftSection.transform, "RaisedIndicator", 
            new Vector2(0, 30), new Vector2(160, 40), "RAISED", 28, TextAlignmentOptions.Center, Color.green);
        leftRaisedIndicator.SetActive(false);
        
        GameObject leftPosText = CreateText(leftSection.transform, "PositionText", 
            new Vector2(0, -40), new Vector2(160, 100), "X: 0.00\nY: 0.00\nZ: 0.00", 20, TextAlignmentOptions.Left, Color.white);

        // Left sliders
        GameObject leftXSlider = CreateSlider(leftSection.transform, "XSlider", new Vector2(0, -110), new Vector2(150, 20), Color.red);
        GameObject leftYSlider = CreateSlider(leftSection.transform, "YSlider", new Vector2(0, -140), new Vector2(150, 20), Color.green);
        GameObject leftZSlider = CreateSlider(leftSection.transform, "ZSlider", new Vector2(0, -170), new Vector2(150, 20), Color.blue);

        // Right hand section (mirror)
        GameObject rightSection = CreateSection(panel.transform, "RightHandSection", new Vector2(100, 0));
        CreateText(rightSection.transform, "Label", new Vector2(0, 150), new Vector2(160, 40), 
            "RIGHT HAND", 24, TextAlignmentOptions.Center, Color.cyan);
        
        GameObject rightIcon = CreateImage(rightSection.transform, "Icon", new Vector2(0, 90), 
            new Vector2(80, 80), Color.white);
        
        GameObject rightRaisedIndicator = CreateText(rightSection.transform, "RaisedIndicator", 
            new Vector2(0, 30), new Vector2(160, 40), "RAISED", 28, TextAlignmentOptions.Center, Color.green);
        rightRaisedIndicator.SetActive(false);
        
        GameObject rightPosText = CreateText(rightSection.transform, "PositionText", 
            new Vector2(0, -40), new Vector2(160, 100), "X: 0.00\nY: 0.00\nZ: 0.00", 20, TextAlignmentOptions.Left, Color.white);

        // Right sliders
        GameObject rightXSlider = CreateSlider(rightSection.transform, "XSlider", new Vector2(0, -110), new Vector2(150, 20), Color.red);
        GameObject rightYSlider = CreateSlider(rightSection.transform, "YSlider", new Vector2(0, -140), new Vector2(150, 20), Color.green);
        GameObject rightZSlider = CreateSlider(rightSection.transform, "ZSlider", new Vector2(0, -170), new Vector2(150, 20), Color.blue);

        // Assign references
        SerializedObject serializedUI = new SerializedObject(armUI);
        serializedUI.FindProperty("leftHandRaisedIndicator").objectReferenceValue = leftRaisedIndicator;
        serializedUI.FindProperty("rightHandRaisedIndicator").objectReferenceValue = rightRaisedIndicator;
        serializedUI.FindProperty("leftHandIcon").objectReferenceValue = leftIcon.GetComponent<Image>();
        serializedUI.FindProperty("rightHandIcon").objectReferenceValue = rightIcon.GetComponent<Image>();
        serializedUI.FindProperty("leftHandPositionText").objectReferenceValue = leftPosText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("rightHandPositionText").objectReferenceValue = rightPosText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("leftHandXBar").objectReferenceValue = leftXSlider.GetComponent<Slider>();
        serializedUI.FindProperty("leftHandYBar").objectReferenceValue = leftYSlider.GetComponent<Slider>();
        serializedUI.FindProperty("leftHandZBar").objectReferenceValue = leftZSlider.GetComponent<Slider>();
        serializedUI.FindProperty("rightHandXBar").objectReferenceValue = rightXSlider.GetComponent<Slider>();
        serializedUI.FindProperty("rightHandYBar").objectReferenceValue = rightYSlider.GetComponent<Slider>();
        serializedUI.FindProperty("rightHandZBar").objectReferenceValue = rightZSlider.GetComponent<Slider>();
        serializedUI.ApplyModifiedProperties();

        return panel;
    }

    #endregion

    #region Head Panel

    private static GameObject BuildHeadPanel(Canvas canvas)
    {
        Debug.Log("Building Head Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "HeadPanel", 
            new Vector2(700, -150), new Vector2(400, 500), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        
        HeadTrackingUI headUI = panel.AddComponent<HeadTrackingUI>();

        // Title
        CreateText(panel.transform, "Title", new Vector2(0, 210), new Vector2(380, 50), 
            "HEAD TRACKING", 36, TextAlignmentOptions.Center, Color.white);

        // Direction indicators in cross pattern
        GameObject upIndicator = CreateText(panel.transform, "UpIndicator", new Vector2(0, 120), 
            new Vector2(80, 40), "▲ UP", 24, TextAlignmentOptions.Center, Color.yellow);
        upIndicator.SetActive(false);
        
        GameObject downIndicator = CreateText(panel.transform, "DownIndicator", new Vector2(0, -120), 
            new Vector2(80, 40), "▼ DOWN", 24, TextAlignmentOptions.Center, Color.yellow);
        downIndicator.SetActive(false);
        
        GameObject leftIndicator = CreateText(panel.transform, "LeftIndicator", new Vector2(-120, 0), 
            new Vector2(80, 40), "◄ LEFT", 24, TextAlignmentOptions.Center, Color.yellow);
        leftIndicator.SetActive(false);
        
        GameObject rightIndicator = CreateText(panel.transform, "RightIndicator", new Vector2(120, 0), 
            new Vector2(80, 40), "RIGHT ►", 24, TextAlignmentOptions.Center, Color.yellow);
        rightIndicator.SetActive(false);

        // Arrow icons
        GameObject upArrow = CreateImage(panel.transform, "UpArrow", new Vector2(0, 90), 
            new Vector2(40, 40), Color.gray);
        GameObject downArrow = CreateImage(panel.transform, "DownArrow", new Vector2(0, -90), 
            new Vector2(40, 40), Color.gray);
        GameObject leftArrow = CreateImage(panel.transform, "LeftArrow", new Vector2(-90, 0), 
            new Vector2(40, 40), Color.gray);
        GameObject rightArrow = CreateImage(panel.transform, "RightArrow", new Vector2(90, 0), 
            new Vector2(40, 40), Color.gray);

        // Center head icon
        GameObject headIcon = CreateImage(panel.transform, "HeadIcon", Vector2.zero, 
            new Vector2(60, 60), Color.cyan);

        // Rotation display
        GameObject rotationText = CreateText(panel.transform, "RotationText", new Vector2(0, -170), 
            new Vector2(380, 80), "Pitch: 0°\nYaw: 0°\nRoll: 0°", 20, TextAlignmentOptions.Center, Color.white);

        // Sliders
        GameObject pitchSlider = CreateSlider(panel.transform, "PitchSlider", new Vector2(-120, -60), new Vector2(200, 20), Color.red);
        GameObject yawSlider = CreateSlider(panel.transform, "YawSlider", new Vector2(-120, -90), new Vector2(200, 20), Color.green);
        GameObject rollSlider = CreateSlider(panel.transform, "RollSlider", new Vector2(-120, -120), new Vector2(200, 20), Color.blue);

        // Assign references
        SerializedObject serializedUI = new SerializedObject(headUI);
        serializedUI.FindProperty("headUpIndicator").objectReferenceValue = upIndicator;
        serializedUI.FindProperty("headDownIndicator").objectReferenceValue = downIndicator;
        serializedUI.FindProperty("headLeftIndicator").objectReferenceValue = leftIndicator;
        serializedUI.FindProperty("headRightIndicator").objectReferenceValue = rightIndicator;
        serializedUI.FindProperty("upArrow").objectReferenceValue = upArrow.GetComponent<Image>();
        serializedUI.FindProperty("downArrow").objectReferenceValue = downArrow.GetComponent<Image>();
        serializedUI.FindProperty("leftArrow").objectReferenceValue = leftArrow.GetComponent<Image>();
        serializedUI.FindProperty("rightArrow").objectReferenceValue = rightArrow.GetComponent<Image>();
        serializedUI.FindProperty("headVisual").objectReferenceValue = headIcon.GetComponent<Image>();
        serializedUI.FindProperty("rotationText").objectReferenceValue = rotationText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("pitchSlider").objectReferenceValue = pitchSlider.GetComponent<Slider>();
        serializedUI.FindProperty("yawSlider").objectReferenceValue = yawSlider.GetComponent<Slider>();
        serializedUI.FindProperty("rollSlider").objectReferenceValue = rollSlider.GetComponent<Slider>();
        serializedUI.ApplyModifiedProperties();

        return panel;
    }

    #endregion

    #region Torso Panel

    private static GameObject BuildTorsoPanel(Canvas canvas)
    {
        Debug.Log("Building Torso Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "TorsoPanel", 
            new Vector2(-700, 350), new Vector2(400, 400), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        
        TorsoTrackingUI torsoUI = panel.AddComponent<TorsoTrackingUI>();

        // Title
        CreateText(panel.transform, "Title", new Vector2(0, 170), new Vector2(380, 50), 
            "TORSO TRACKING", 36, TextAlignmentOptions.Center, Color.white);

        // Weight shift label
        CreateText(panel.transform, "WeightLabel", new Vector2(0, 100), new Vector2(380, 40), 
            "WEIGHT SHIFT", 28, TextAlignmentOptions.Center, Color.white);

        // Left/Right indicators
        GameObject leftIndicator = CreateText(panel.transform, "ShiftLeftIndicator", 
            new Vector2(-150, 50), new Vector2(100, 40), "◄ LEFT", 24, TextAlignmentOptions.Center, Color.cyan);
        leftIndicator.SetActive(false);
        
        GameObject rightIndicator = CreateText(panel.transform, "ShiftRightIndicator", 
            new Vector2(150, 50), new Vector2(100, 40), "RIGHT ►", 24, TextAlignmentOptions.Center, Color.magenta);
        rightIndicator.SetActive(false);

        // Icons
        GameObject leftIcon = CreateImage(panel.transform, "ShiftLeftIcon", new Vector2(-150, 50), 
            new Vector2(50, 50), Color.gray);
        GameObject rightIcon = CreateImage(panel.transform, "ShiftRightIcon", new Vector2(150, 50), 
            new Vector2(50, 50), Color.gray);

        // Weight shift slider
        GameObject weightSlider = CreateSlider(panel.transform, "WeightShiftSlider", 
            new Vector2(0, 50), new Vector2(350, 30), Color.white);

        // Balance indicator (moves on slider)
        GameObject balanceIndicator = CreateImage(weightSlider.transform, "BalanceIndicator", 
            Vector2.zero, new Vector2(20, 50), Color.yellow);

        // Value text
        GameObject valueText = CreateText(panel.transform, "WeightShiftValueText", 
            new Vector2(0, 10), new Vector2(380, 60), "WEIGHT: NEUTRAL\n0.00", 24, TextAlignmentOptions.Center, Color.white);

        // Posture section
        CreateText(panel.transform, "PostureLabel", new Vector2(0, -50), new Vector2(380, 40), 
            "POSTURE", 28, TextAlignmentOptions.Center, Color.white);

        GameObject postureText = CreateText(panel.transform, "PostureText", 
            new Vector2(0, -100), new Vector2(380, 50), "UPRIGHT", 32, TextAlignmentOptions.Center, Color.green);

        GameObject bentIndicator = CreateText(panel.transform, "BentOverIndicator", 
            new Vector2(0, -140), new Vector2(200, 40), "BENT OVER", 24, TextAlignmentOptions.Center, Color.red);
        bentIndicator.SetActive(false);
        
        GameObject uprightIndicator = CreateText(panel.transform, "UprightIndicator", 
            new Vector2(0, -140), new Vector2(200, 40), "UPRIGHT", 24, TextAlignmentOptions.Center, Color.green);

        GameObject postureIcon = CreateImage(panel.transform, "PostureIcon", 
            new Vector2(0, -170), new Vector2(60, 60), Color.green);

        // Assign references
        SerializedObject serializedUI = new SerializedObject(torsoUI);
        serializedUI.FindProperty("shiftLeftIndicator").objectReferenceValue = leftIndicator;
        serializedUI.FindProperty("shiftRightIndicator").objectReferenceValue = rightIndicator;
        serializedUI.FindProperty("shiftLeftIcon").objectReferenceValue = leftIcon.GetComponent<Image>();
        serializedUI.FindProperty("shiftRightIcon").objectReferenceValue = rightIcon.GetComponent<Image>();
        serializedUI.FindProperty("bentOverIndicator").objectReferenceValue = bentIndicator;
        serializedUI.FindProperty("uprightIndicator").objectReferenceValue = uprightIndicator;
        serializedUI.FindProperty("postureIcon").objectReferenceValue = postureIcon.GetComponent<Image>();
        serializedUI.FindProperty("weightShiftSlider").objectReferenceValue = weightSlider.GetComponent<Slider>();
        serializedUI.FindProperty("weightShiftValueText").objectReferenceValue = valueText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("postureText").objectReferenceValue = postureText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("balanceIndicator").objectReferenceValue = balanceIndicator.GetComponent<RectTransform>();
        serializedUI.FindProperty("balanceBarWidth").floatValue = 350f;
        serializedUI.ApplyModifiedProperties();

        return panel;
    }

    #endregion

    #region Foot Panel

    private static GameObject BuildFootPanel(Canvas canvas)
    {
        Debug.Log("Building Foot Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "FootPanel", 
            new Vector2(0, 350), new Vector2(350, 400), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        
        FootTrackingUI footUI = panel.AddComponent<FootTrackingUI>();

        // Title
        CreateText(panel.transform, "Title", new Vector2(0, 170), new Vector2(330, 50), 
            "FOOT TRACKING", 36, TextAlignmentOptions.Center, Color.white);

        // Walking indicator
        GameObject walkingIndicator = CreateText(panel.transform, "WalkingIndicator", 
            new Vector2(0, 120), new Vector2(200, 40), "🚶 WALKING", 28, TextAlignmentOptions.Center, Color.green);
        walkingIndicator.SetActive(false);

        GameObject walkingIcon = CreateImage(panel.transform, "WalkingIcon", 
            new Vector2(0, 80), new Vector2(60, 60), Color.green);

        // Foot icons
        GameObject leftFootIcon = CreateImage(panel.transform, "LeftFootIcon", 
            new Vector2(-80, 30), new Vector2(70, 70), Color.gray);
        GameObject rightFootIcon = CreateImage(panel.transform, "RightFootIcon", 
            new Vector2(80, 30), new Vector2(70, 70), Color.gray);

        CreateText(panel.transform, "LeftLabel", new Vector2(-80, -20), new Vector2(100, 30), 
            "LEFT", 20, TextAlignmentOptions.Center, Color.white);
        CreateText(panel.transform, "RightLabel", new Vector2(80, -20), new Vector2(100, 30), 
            "RIGHT", 20, TextAlignmentOptions.Center, Color.white);

        // Metrics
        GameObject cadenceText = CreateText(panel.transform, "CadenceText", 
            new Vector2(0, -60), new Vector2(330, 30), "Cadence: 0 SPM", 22, TextAlignmentOptions.Center, Color.white);

        GameObject speedText = CreateText(panel.transform, "SpeedText", 
            new Vector2(0, -90), new Vector2(330, 30), "Speed: 0.00 m/s", 22, TextAlignmentOptions.Center, Color.white);

        GameObject statusText = CreateText(panel.transform, "StatusText", 
            new Vector2(0, -130), new Vector2(330, 60), "STANDING\nSteps: 0", 26, TextAlignmentOptions.Center, Color.white);

        // Sliders
        GameObject cadenceSlider = CreateSlider(panel.transform, "CadenceSlider", 
            new Vector2(0, -165), new Vector2(300, 20), Color.cyan);
        GameObject speedSlider = CreateSlider(panel.transform, "SpeedSlider", 
            new Vector2(0, -190), new Vector2(300, 20), Color.magenta);

        // Assign references
        SerializedObject serializedUI = new SerializedObject(footUI);
        serializedUI.FindProperty("walkingIndicator").objectReferenceValue = walkingIndicator;
        serializedUI.FindProperty("walkingIcon").objectReferenceValue = walkingIcon.GetComponent<Image>();
        serializedUI.FindProperty("leftFootIcon").objectReferenceValue = leftFootIcon.GetComponent<Image>();
        serializedUI.FindProperty("rightFootIcon").objectReferenceValue = rightFootIcon.GetComponent<Image>();
        serializedUI.FindProperty("cadenceText").objectReferenceValue = cadenceText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("walkSpeedText").objectReferenceValue = speedText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("cadenceSlider").objectReferenceValue = cadenceSlider.GetComponent<Slider>();
        serializedUI.FindProperty("walkSpeedSlider").objectReferenceValue = speedSlider.GetComponent<Slider>();
        serializedUI.ApplyModifiedProperties();

        return panel;
    }

    #endregion

    #region Balance Panel

    private static GameObject BuildBalancePanel(Canvas canvas)
    {
        Debug.Log("Building Balance Panel...");
        
        GameObject panel = CreatePanel(canvas.transform, "BalancePanel", 
            new Vector2(700, 350), new Vector2(400, 400), new Color(0.15f, 0.15f, 0.15f, 0.9f));
        
        BalanceTrackingUI balanceUI = panel.AddComponent<BalanceTrackingUI>();

        // Title
        CreateText(panel.transform, "Title", new Vector2(0, 170), new Vector2(380, 50), 
            "BALANCE", 36, TextAlignmentOptions.Center, Color.white);

        // Balance zone visualization
        GameObject balanceZone = CreateImage(panel.transform, "BalanceZone", 
            new Vector2(0, 60), new Vector2(200, 200), new Color(0.2f, 0.2f, 0.2f, 0.8f));
        
        GameObject comIndicator = CreateImage(balanceZone.transform, "CenterOfMassIndicator", 
            Vector2.zero, new Vector2(20, 20), Color.red);

        // Status
        GameObject statusText = CreateText(panel.transform, "BalanceStateText", 
            new Vector2(0, -70), new Vector2(380, 50), "BALANCED", 32, TextAlignmentOptions.Center, Color.green);

        GameObject balanceIndicator = CreateImage(panel.transform, "BalanceIndicator", 
            new Vector2(-160, -70), new Vector2(50, 50), Color.green);

        // Sway text
        GameObject swayText = CreateText(panel.transform, "SwayText", 
            new Vector2(0, -120), new Vector2(380, 80), "Lateral: 0.000\nAP: 0.000\nMagnitude: 0.000", 
            18, TextAlignmentOptions.Center, Color.white);

        // Sway sliders
        GameObject lateralSlider = CreateSlider(panel.transform, "LateralSwaySlider", 
            new Vector2(0, -165), new Vector2(350, 15), Color.cyan);
        GameObject apSlider = CreateSlider(panel.transform, "APSwaySlider", 
            new Vector2(0, -185), new Vector2(350, 15), Color.magenta);
        GameObject magnitudeSlider = CreateSlider(panel.transform, "SwayMagnitudeSlider", 
            new Vector2(0, -205), new Vector2(350, 15), Color.yellow);

        // Assign references
        SerializedObject serializedUI = new SerializedObject(balanceUI);
        serializedUI.FindProperty("balanceStateText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("balanceIndicator").objectReferenceValue = balanceIndicator.GetComponent<Image>();
        serializedUI.FindProperty("centerOfMassIndicator").objectReferenceValue = comIndicator.GetComponent<RectTransform>();
        serializedUI.FindProperty("balanceZone").objectReferenceValue = balanceZone.GetComponent<RectTransform>();
        serializedUI.FindProperty("swayText").objectReferenceValue = swayText.GetComponent<TextMeshProUGUI>();
        serializedUI.FindProperty("lateralSwaySlider").objectReferenceValue = lateralSlider.GetComponent<Slider>();
        serializedUI.FindProperty("anteriorPosteriorSwaySlider").objectReferenceValue = apSlider.GetComponent<Slider>();
        serializedUI.FindProperty("swayMagnitudeSlider").objectReferenceValue = magnitudeSlider.GetComponent<Slider>();
        serializedUI.FindProperty("visualizationScale").floatValue = 100f;
        serializedUI.ApplyModifiedProperties();

        return panel;
    }

    #endregion

    #region Helper Methods

    private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    private static GameObject CreateText(Transform parent, string name, Vector2 position, Vector2 size, 
        string text, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;

        return textObj;
    }

    private static GameObject CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObj = new GameObject(name);
        imageObj.transform.SetParent(parent, false);

        RectTransform rt = imageObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image image = imageObj.AddComponent<Image>();
        image.color = color;

        return imageObj;
    }

    private static GameObject CreateSlider(Transform parent, string name, Vector2 position, Vector2 size, Color fillColor)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform rt = sliderObj.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = -1f;
        slider.maxValue = 1f;
        slider.value = 0f;

        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = background.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f);
        faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.sizeDelta = new Vector2(-10, 0);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;

        slider.fillRect = fillRt;
        slider.targetGraphic = fillImage;

        return sliderObj;
    }

    private static GameObject CreateSection(Transform parent, string name, Vector2 position)
    {
        GameObject section = new GameObject(name);
        section.transform.SetParent(parent, false);

        RectTransform rt = section.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(180, 400);

        return section;
    }

    #endregion
}
#endif
#endif