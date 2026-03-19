#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BalanceTrackingUI : MonoBehaviour
{
    [Header("Balance State Display")]
    [SerializeField] private TextMeshProUGUI balanceStateText;
    [SerializeField] private Image balanceIndicator;

    [Header("Center of Mass Visualization")]
    [SerializeField] private RectTransform centerOfMassIndicator;
    [SerializeField] private RectTransform balanceZone;
    [SerializeField] private float visualizationScale = 100f;

    [Header("Sway Display")]
    [SerializeField] private Slider lateralSwaySlider;
    [SerializeField] private Slider anteriorPosteriorSwaySlider;
    [SerializeField] private Slider swayMagnitudeSlider;
    [SerializeField] private TextMeshProUGUI swayText;

    [Header("Colors")]
    [SerializeField] private Color balancedColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color unbalancedColor = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color feedbackColor = new Color(1f, 1f, 0.2f);

    [Header("Settings")]
    [SerializeField] private float maxSwayDisplay = 0.2f;
    [SerializeField] private float feedbackDuration = 0.5f;

    private float balanceFeedbackTimer = 0f;

    void Update()
    {
        // Handle feedback timer
        if (balanceFeedbackTimer > 0f)
        {
            balanceFeedbackTimer -= Time.deltaTime;
        }
    }

    public void UpdateDisplay(bool isBalanced, Vector3 comPosition, float lateralSway, float apSway, float swayMagnitude)
    {
        UpdateBalanceState(isBalanced, swayMagnitude);
        UpdateCenterOfMass(comPosition);
        UpdateSwayMeters(lateralSway, apSway, swayMagnitude);
    }

    private void UpdateBalanceState(bool isBalanced, float swayMagnitude)
    {
        // Update text
        if (balanceStateText != null)
            balanceStateText.text = isBalanced ? "BALANCED" : "UNBALANCED";

        // Update indicator color (unless in feedback mode)
        if (balanceIndicator != null && balanceFeedbackTimer <= 0f)
        {
            if (isBalanced)
                balanceIndicator.color = balancedColor;
            else if (swayMagnitude > maxSwayDisplay * 0.5f)
                balanceIndicator.color = warningColor;
            else
                balanceIndicator.color = unbalancedColor;
        }
    }

    private void UpdateCenterOfMass(Vector3 comPosition)
    {
        if (centerOfMassIndicator != null)
        {
            // Position the indicator based on center of mass
            // Using X and Z (lateral and AP) for 2D visualization
            Vector2 position = new Vector2(comPosition.x, comPosition.z) * visualizationScale;
            centerOfMassIndicator.anchoredPosition = position;
        }
    }

    private void UpdateSwayMeters(float lateralSway, float apSway, float swayMagnitude)
    {
        // Update lateral sway slider
        if (lateralSwaySlider != null)
        {
            lateralSwaySlider.minValue = -maxSwayDisplay;
            lateralSwaySlider.maxValue = maxSwayDisplay;
            lateralSwaySlider.value = Mathf.Clamp(lateralSway, -maxSwayDisplay, maxSwayDisplay);
        }

        // Update anterior-posterior sway slider
        if (anteriorPosteriorSwaySlider != null)
        {
            anteriorPosteriorSwaySlider.minValue = -maxSwayDisplay;
            anteriorPosteriorSwaySlider.maxValue = maxSwayDisplay;
            anteriorPosteriorSwaySlider.value = Mathf.Clamp(apSway, -maxSwayDisplay, maxSwayDisplay);
        }

        // Update sway magnitude slider
        if (swayMagnitudeSlider != null)
        {
            swayMagnitudeSlider.maxValue = maxSwayDisplay * 1.5f;
            swayMagnitudeSlider.value = Mathf.Clamp(swayMagnitude, 0f, maxSwayDisplay * 1.5f);
        }

        // Update sway text
        if (swayText != null)
        {
            swayText.text = $"Lateral: {lateralSway:F3}\n" +
                          $"AP: {apSway:F3}\n" +
                          $"Magnitude: {swayMagnitude:F3}";
        }
    }

    // Feedback method called by UI manager
    public void TriggerBalanceFeedback()
    {
        balanceFeedbackTimer = feedbackDuration;
        if (balanceIndicator != null)
            balanceIndicator.color = feedbackColor;
    }
}
#endif