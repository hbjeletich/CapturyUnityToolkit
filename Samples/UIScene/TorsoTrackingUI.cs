#if TMP_PRESENT
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TorsoTrackingUI : MonoBehaviour
{
    [Header("Weight Shift Indicators")]
    [SerializeField] private GameObject shiftLeftIndicator;
    [SerializeField] private GameObject shiftRightIndicator;
    [SerializeField] private Image shiftLeftIcon;
    [SerializeField] private Image shiftRightIcon;

    [Header("Posture Indicators")]
    [SerializeField] private GameObject bentOverIndicator;
    [SerializeField] private GameObject uprightIndicator;
    [SerializeField] private Image postureIcon;

    [Header("Visual Displays")]
    [SerializeField] private Slider weightShiftSlider;
    [SerializeField] private TextMeshProUGUI weightShiftValueText;
    [SerializeField] private TextMeshProUGUI postureText;

    [Header("Balance Bar")]
    [SerializeField] private RectTransform balanceIndicator;
    [SerializeField] private float balanceBarWidth = 200f;

    [Header("Colors")]
    [SerializeField] private Color shiftLeftColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color shiftRightColor = new Color(1f, 0.6f, 0.3f);
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color bentOverColor = new Color(1f, 0.5f, 0.2f);
    [SerializeField] private Color uprightColor = new Color(0.2f, 1f, 0.5f);
    [SerializeField] private Color feedbackColor = new Color(1f, 1f, 0.2f);

    [Header("Settings")]
    [SerializeField] private float feedbackDuration = 0.3f;

    private float weightShiftFeedbackTimer = 0f;
    private float postureFeedbackTimer = 0f;

    void Update()
    {
        // Handle feedback timers
        if (weightShiftFeedbackTimer > 0f)
        {
            weightShiftFeedbackTimer -= Time.deltaTime;
        }

        if (postureFeedbackTimer > 0f)
        {
            postureFeedbackTimer -= Time.deltaTime;
            if (postureFeedbackTimer <= 0f && postureIcon != null)
            {
                // Return to normal posture color
                bool isBentOver = bentOverIndicator != null && bentOverIndicator.activeSelf;
                postureIcon.color = isBentOver ? bentOverColor : uprightColor;
            }
        }
    }

    public void UpdateDisplay(bool shiftLeft, bool shiftRight, bool bentOver, float weightShiftValue)
    {
        UpdateWeightShift(shiftLeft, shiftRight, weightShiftValue);
        UpdatePosture(bentOver);
    }

    private void UpdateWeightShift(bool shiftLeft, bool shiftRight, float shiftValue)
    {
        // Update indicators
        if (shiftLeftIndicator != null)
            shiftLeftIndicator.SetActive(shiftLeft);

        if (shiftRightIndicator != null)
            shiftRightIndicator.SetActive(shiftRight);

        // Update icons (only if not in feedback mode)
        if (weightShiftFeedbackTimer <= 0f)
        {
            if (shiftLeftIcon != null)
                shiftLeftIcon.color = shiftLeft ? shiftLeftColor : neutralColor;

            if (shiftRightIcon != null)
                shiftRightIcon.color = shiftRight ? shiftRightColor : neutralColor;
        }

        // Update slider
        if (weightShiftSlider != null)
        {
            weightShiftSlider.minValue = -1f;
            weightShiftSlider.maxValue = 1f;
            weightShiftSlider.value = shiftValue;
        }

        // Update value text
        if (weightShiftValueText != null)
        {
            string direction = shiftLeft ? "LEFT" : shiftRight ? "RIGHT" : "NEUTRAL";
            weightShiftValueText.text = $"WEIGHT: {direction}\n{Mathf.Abs(shiftValue):F2}";
        }

        // Update balance indicator position
        if (balanceIndicator != null)
        {
            float xPos = shiftValue * (balanceBarWidth / 2f);
            balanceIndicator.anchoredPosition = new Vector2(xPos, balanceIndicator.anchoredPosition.y);
        }
    }

    private void UpdatePosture(bool bentOver)
    {
        // Update indicators
        if (bentOverIndicator != null)
            bentOverIndicator.SetActive(bentOver);

        if (uprightIndicator != null)
            uprightIndicator.SetActive(!bentOver);

        // Update posture icon (only if not in feedback mode)
        if (postureFeedbackTimer <= 0f && postureIcon != null)
            postureIcon.color = bentOver ? bentOverColor : uprightColor;

        // Update text
        if (postureText != null)
            postureText.text = bentOver ? "BENT OVER" : "UPRIGHT";
    }

    // Feedback methods called by UI manager
    public void TriggerWeightShiftFeedback()
    {
        weightShiftFeedbackTimer = feedbackDuration;
        if (shiftLeftIcon != null)
            shiftLeftIcon.color = feedbackColor;
        if (shiftRightIcon != null)
            shiftRightIcon.color = feedbackColor;
    }

    public void TriggerPostureFeedback()
    {
        postureFeedbackTimer = feedbackDuration;
        if (postureIcon != null)
            postureIcon.color = feedbackColor;
    }
}
#endif