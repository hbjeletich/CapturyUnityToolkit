#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FootTrackingUI : MonoBehaviour
{
    [Header("Walking Indicators")]
    [SerializeField] private GameObject walkingIndicator;
    [SerializeField] private Image walkingIcon;
    [SerializeField] private Image leftFootIcon;
    [SerializeField] private Image rightFootIcon;

    [Header("Display Text")]
    [SerializeField] private TextMeshProUGUI cadenceText;
    [SerializeField] private TextMeshProUGUI walkSpeedText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Visual Meters")]
    [SerializeField] private Slider cadenceSlider;
    [SerializeField] private Slider walkSpeedSlider;

    [Header("Colors")]
    [SerializeField] private Color walkingColor = new Color(0.2f, 1f, 0.5f);
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color stepFeedbackColor = new Color(1f, 1f, 0.2f);

    [Header("Settings")]
    [SerializeField] private float maxCadence = 180f; // steps per minute
    [SerializeField] private float maxWalkSpeed = 2f; // m/s
    [SerializeField] private float feedbackDuration = 0.15f;

    private float stepFeedbackTimer = 0f;
    private int totalSteps = 0;

    void Update()
    {
        // Handle feedback timer
        if (stepFeedbackTimer > 0f)
        {
            stepFeedbackTimer -= Time.deltaTime;
            if (stepFeedbackTimer <= 0f)
            {
                // Return foot icons to normal color
                bool isWalking = walkingIndicator != null && walkingIndicator.activeSelf;
                Color normalColor = isWalking ? walkingColor : idleColor;
                if (leftFootIcon != null)
                    leftFootIcon.color = normalColor;
                if (rightFootIcon != null)
                    rightFootIcon.color = normalColor;
            }
        }
    }

    public void UpdateDisplay(bool isWalking, float cadence, float walkSpeed, bool leftStep, bool rightStep)
    {
        // Update walking indicator
        if (walkingIndicator != null)
            walkingIndicator.SetActive(isWalking);

        Color currentColor = stepFeedbackTimer > 0f ? stepFeedbackColor : (isWalking ? walkingColor : idleColor);
        
        if (walkingIcon != null)
            walkingIcon.color = currentColor;

        if (leftFootIcon != null && leftStep)
        {
            if(isWalking) leftFootIcon.color = walkingColor;
            else leftFootIcon.color = stepFeedbackColor;
        }
        if (rightFootIcon != null && rightStep)
        {
            if(isWalking) rightFootIcon.color = walkingColor;
            else rightFootIcon.color = stepFeedbackColor;
        }

        // Update cadence display
        if (cadenceText != null)
            cadenceText.text = $"Cadence: {cadence:F1} SPM";

        if (cadenceSlider != null)
        {
            cadenceSlider.maxValue = maxCadence;
            cadenceSlider.value = Mathf.Clamp(cadence, 0f, maxCadence);
        }

        // Update walk speed display
        if (walkSpeedText != null)
            walkSpeedText.text = $"Speed: {walkSpeed:F2} m/s";

        if (walkSpeedSlider != null)
        {
            walkSpeedSlider.maxValue = maxWalkSpeed;
            walkSpeedSlider.value = Mathf.Clamp(walkSpeed, 0f, maxWalkSpeed);
        }

        // Update status
        if (statusText != null)
        {
            if (isWalking)
                statusText.text = $"WALKING\nSteps: {totalSteps}";
            else
                statusText.text = $"STANDING\nSteps: {totalSteps}";
        }
    }

    // Feedback method called by UI manager
    public void TriggerStepFeedback(bool isRightStep)
    {
        totalSteps++;
        stepFeedbackTimer = feedbackDuration;
        
        if (leftFootIcon != null && !isRightStep)
            leftFootIcon.color = stepFeedbackColor;
        if (rightFootIcon != null && isRightStep)
            rightFootIcon.color = stepFeedbackColor;
    }

    public void ResetStepCount()
    {
        totalSteps = 0;
    }

    public int GetTotalSteps()
    {
        return totalSteps;
    }
}
#endif