#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeadTrackingUI : MonoBehaviour
{
    [Header("Direction Indicators")]
    [SerializeField] private GameObject headUpIndicator;
    [SerializeField] private GameObject headDownIndicator;
    [SerializeField] private GameObject headLeftIndicator;
    [SerializeField] private GameObject headRightIndicator;

    [Header("Direction Icons")]
    [SerializeField] private Image upArrow;
    [SerializeField] private Image downArrow;
    [SerializeField] private Image leftArrow;
    [SerializeField] private Image rightArrow;

    [Header("Rotation Display")]
    [SerializeField] private TextMeshProUGUI rotationText;
    [SerializeField] private Slider pitchSlider; // Up/Down (Z rotation)
    [SerializeField] private Slider yawSlider;   // Left/Right (Y rotation)
    [SerializeField] private Slider rollSlider;  // Tilt (X rotation)

    [Header("Visual Feedback")]
    [SerializeField] private Image headVisual;
    [SerializeField] private RectTransform headIndicatorArrow;

    [Header("Colors")]
    [SerializeField] private Color activeDirectionColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color inactiveDirectionColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color feedbackColor = new Color(0.2f, 1f, 0.5f);

    [Header("Settings")]
    [SerializeField] private float maxRotationDisplay = 45f;
    [SerializeField] private float feedbackDuration = 0.3f;

    private float directionFeedbackTimer = 0f;

    void Update()
    {
        // Handle feedback timer
        if (directionFeedbackTimer > 0f)
        {
            directionFeedbackTimer -= Time.deltaTime;
        }
    }

    public void UpdateDisplay(bool up, bool down, bool left, bool right, Vector3 rotation)
    {
        // Update direction indicators
        UpdateDirectionIndicator(up, headUpIndicator, upArrow);
        UpdateDirectionIndicator(down, headDownIndicator, downArrow);
        UpdateDirectionIndicator(left, headLeftIndicator, leftArrow);
        UpdateDirectionIndicator(right, headRightIndicator, rightArrow);

        // Update rotation text
        if (rotationText != null)
        {
            rotationText.text = $"HEAD ROTATION\n" +
                              $"Pitch: {rotation.z:F1}°\n" +
                              $"Yaw: {rotation.y:F1}°\n" +
                              $"Roll: {rotation.x:F1}°";
        }

        // Update sliders
        UpdateRotationSliders(rotation);

        // Update visual head indicator
        UpdateHeadVisual(rotation);
    }

    private void UpdateDirectionIndicator(bool isActive, GameObject indicator, Image arrow)
    {
        if (indicator != null)
            indicator.SetActive(isActive);

        if (arrow != null && directionFeedbackTimer <= 0f)
            arrow.color = isActive ? activeDirectionColor : inactiveDirectionColor;
    }

    private void UpdateRotationSliders(Vector3 rotation)
    {
        if (pitchSlider != null)
        {
            pitchSlider.minValue = -maxRotationDisplay;
            pitchSlider.maxValue = maxRotationDisplay;
            pitchSlider.value = Mathf.Clamp(rotation.z, -maxRotationDisplay, maxRotationDisplay);
        }

        if (yawSlider != null)
        {
            yawSlider.minValue = -maxRotationDisplay;
            yawSlider.maxValue = maxRotationDisplay;
            yawSlider.value = Mathf.Clamp(rotation.y, -maxRotationDisplay, maxRotationDisplay);
        }

        if (rollSlider != null)
        {
            rollSlider.minValue = -maxRotationDisplay;
            rollSlider.maxValue = maxRotationDisplay;
            rollSlider.value = Mathf.Clamp(rotation.x, -maxRotationDisplay, maxRotationDisplay);
        }
    }

    private void UpdateHeadVisual(Vector3 rotation)
    {
        if (headVisual != null && headIndicatorArrow != null)
        {
            // Rotate the arrow to show head direction (using yaw for horizontal rotation)
            float angle = rotation.y;
            headIndicatorArrow.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }

    // Feedback method called by UI manager
    public void TriggerDirectionFeedback()
    {
        directionFeedbackTimer = feedbackDuration;
        
        // Flash all arrows with feedback color
        if (upArrow != null)
            upArrow.color = feedbackColor;
        if (downArrow != null)
            downArrow.color = feedbackColor;
        if (leftArrow != null)
            leftArrow.color = feedbackColor;
        if (rightArrow != null)
            rightArrow.color = feedbackColor;
    }
}
#endif