#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArmTrackingUI : MonoBehaviour
{
    [Header("Hand Raised Indicators")]
    [SerializeField] private GameObject leftHandRaisedIndicator;
    [SerializeField] private GameObject rightHandRaisedIndicator;
    [SerializeField] private Image leftHandIcon;
    [SerializeField] private Image rightHandIcon;

    [Header("Position Display")]
    [SerializeField] private TextMeshProUGUI leftHandPositionText;
    [SerializeField] private TextMeshProUGUI rightHandPositionText;

    [Header("Visual Bars")]
    [SerializeField] private Slider leftHandXBar;
    [SerializeField] private Slider leftHandYBar;
    [SerializeField] private Slider leftHandZBar;
    [SerializeField] private Slider rightHandXBar;
    [SerializeField] private Slider rightHandYBar;
    [SerializeField] private Slider rightHandZBar;

    [Header("Colors")]
    [SerializeField] private Color raisedColor = new Color(0.2f, 1f, 0.2f);
    [SerializeField] private Color loweredColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color feedbackColor = new Color(1f, 1f, 0.2f);

    [Header("Settings")]
    [SerializeField] private float maxDisplayRange = 1f;
    [SerializeField] private bool showDetailedPosition = true;
    [SerializeField] private float feedbackDuration = 0.2f;

    private float leftHandFeedbackTimer = 0f;
    private float rightHandFeedbackTimer = 0f;

    void Update()
    {
        // Handle feedback timers
        if (leftHandFeedbackTimer > 0f)
        {
            leftHandFeedbackTimer -= Time.deltaTime;
            if (leftHandFeedbackTimer <= 0f && leftHandIcon != null)
            {
                // Return to normal state color based on current raised state
                leftHandIcon.color = leftHandRaisedIndicator != null && leftHandRaisedIndicator.activeSelf ? raisedColor : loweredColor;
            }
        }

        if (rightHandFeedbackTimer > 0f)
        {
            rightHandFeedbackTimer -= Time.deltaTime;
            if (rightHandFeedbackTimer <= 0f && rightHandIcon != null)
            {
                rightHandIcon.color = rightHandRaisedIndicator != null && rightHandRaisedIndicator.activeSelf ? raisedColor : loweredColor;
            }
        }
    }

    public void UpdateDisplay(bool leftRaised, bool rightRaised, Vector3 leftPos, Vector3 rightPos)
    {
        // Update hand raised indicators
        UpdateHandRaised(leftRaised, leftHandRaisedIndicator, leftHandIcon, leftHandFeedbackTimer <= 0f);
        UpdateHandRaised(rightRaised, rightHandRaisedIndicator, rightHandIcon, rightHandFeedbackTimer <= 0f);

        // Update position displays
        if (showDetailedPosition)
        {
            UpdatePositionText(leftPos, leftHandPositionText, "LEFT");
            UpdatePositionText(rightPos, rightHandPositionText, "RIGHT");
        }

        // Update visual bars
        UpdatePositionBars(leftPos, leftHandXBar, leftHandYBar, leftHandZBar);
        UpdatePositionBars(rightPos, rightHandXBar, rightHandYBar, rightHandZBar);
    }

    private void UpdateHandRaised(bool isRaised, GameObject indicator, Image icon, bool allowColorUpdate)
    {
        if (indicator != null)
            indicator.SetActive(isRaised);

        if (icon != null && allowColorUpdate)
            icon.color = isRaised ? raisedColor : loweredColor;
    }

    private void UpdatePositionText(Vector3 position, TextMeshProUGUI textField, string label)
    {
        if (textField != null)
        {
            textField.text = $"{label}\n" +
                           $"X: {position.x:F2}\n" +
                           $"Y: {position.y:F2}\n" +
                           $"Z: {position.z:F2}";
        }
    }

    private void UpdatePositionBars(Vector3 position, Slider xBar, Slider yBar, Slider zBar)
    {
        if (xBar != null)
        {
            xBar.minValue = -maxDisplayRange;
            xBar.maxValue = maxDisplayRange;
            xBar.value = Mathf.Clamp(position.x, -maxDisplayRange, maxDisplayRange);
        }

        if (yBar != null)
        {
            yBar.minValue = -maxDisplayRange;
            yBar.maxValue = maxDisplayRange;
            yBar.value = Mathf.Clamp(position.y, -maxDisplayRange, maxDisplayRange);
        }

        if (zBar != null)
        {
            zBar.minValue = -maxDisplayRange;
            zBar.maxValue = maxDisplayRange;
            zBar.value = Mathf.Clamp(position.z, -maxDisplayRange, maxDisplayRange);
        }
    }

    // Feedback methods called by UI manager
    public void TriggerLeftHandFeedback(bool raised)
    {
        if (leftHandIcon != null)
        {
            leftHandIcon.color = feedbackColor;
            leftHandFeedbackTimer = feedbackDuration;
        }
    }

    public void TriggerRightHandFeedback(bool raised)
    {
        if (rightHandIcon != null)
        {
            rightHandIcon.color = feedbackColor;
            rightHandFeedbackTimer = feedbackDuration;
        }
    }
}
#endif