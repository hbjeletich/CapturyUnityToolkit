#if TMP_PRESENT
using UnityEngine;
using TMPro;

public class Anaglyph3DText : MonoBehaviour
{
    [Header("Offset Settings")]
    [SerializeField] private Vector2 offset = new Vector2(3f, 0f); // Pixel offset for the effect
    
    [Header("Animation")]
    [SerializeField] private bool animateOffset = false;
    [SerializeField] private float animationSpeed = 2f;
    [SerializeField] private float animationAmount = 2f; // How much extra offset when animating
    [SerializeField] private AnimationStyle animationStyle = AnimationStyle.Pulse;
    
    public enum AnimationStyle
    {
        Pulse,      // Smooth in and out
        Wobble,     // Circular motion
        Glitch      // Random jitter
    }
    
    [Header("Colors")]
    [SerializeField] private Color leftColor = new Color(1f, 0f, 1f, 0.8f);  // Magenta
    [SerializeField] private Color rightColor = new Color(0f, 1f, 1f, 0.8f); // Cyan
    [SerializeField] private Color mainColor = Color.white;
    
    [Header("References")]
    [SerializeField] private TextMeshProUGUI sourceText;
    
    private TextMeshProUGUI leftText;
    private TextMeshProUGUI rightText;
    private TextMeshProUGUI mainText;
    private GameObject leftObject;
    private GameObject rightObject;
    private GameObject mainObject;
    private Vector2 currentAnimOffset;
    private float glitchTimer;
    private Vector2 glitchOffset;
    
    void Awake()
    {
        if (sourceText == null)
        {
            sourceText = GetComponent<TextMeshProUGUI>();
        }
        
        if (sourceText == null)
        {
            Debug.LogError("Anaglyph3DText: No TextMeshProUGUI found!");
            return;
        }
        
        CreateAnaglyphLayers();
    }
    
    void Start()
    {
        UpdateEffect();
    }
    
    void LateUpdate()
    {
        // Keep the layers in sync with the source text
        if (leftText != null && rightText != null && mainText != null)
        {
            if (mainText.text != sourceText.text)
            {
                UpdateEffect();
            }
            
            if (animateOffset)
            {
                UpdateAnimation();
                ApplyOffset(offset + currentAnimOffset);
            }
        }
    }
    
    private void UpdateAnimation()
    {
        switch (animationStyle)
        {
            case AnimationStyle.Pulse:
                // Smooth pulsing - offset grows and shrinks
                float pulse = Mathf.Sin(Time.time * animationSpeed) * animationAmount;
                currentAnimOffset = new Vector2(pulse, 0f);
                break;
                
            case AnimationStyle.Wobble:
                // Circular wobble motion
                float wobbleX = Mathf.Sin(Time.time * animationSpeed) * animationAmount;
                float wobbleY = Mathf.Cos(Time.time * animationSpeed * 1.3f) * animationAmount * 0.5f;
                currentAnimOffset = new Vector2(wobbleX, wobbleY);
                break;
                
            case AnimationStyle.Glitch:
                // Random jitter at intervals
                glitchTimer -= Time.deltaTime;
                if (glitchTimer <= 0f)
                {
                    glitchOffset = new Vector2(
                        Random.Range(-animationAmount, animationAmount),
                        Random.Range(-animationAmount * 0.5f, animationAmount * 0.5f)
                    );
                    glitchTimer = Random.Range(0.05f, 0.15f);
                }
                currentAnimOffset = glitchOffset;
                break;
        }
    }
    
    private void ApplyOffset(Vector2 totalOffset)
    {
        RectTransform leftRect = leftObject.GetComponent<RectTransform>();
        RectTransform rightRect = rightObject.GetComponent<RectTransform>();
        
        leftRect.anchoredPosition = -totalOffset;
        rightRect.anchoredPosition = totalOffset;
    }
    
    private void CreateAnaglyphLayers()
    {
        // Create left (magenta) layer - rendered first (behind)
        leftObject = new GameObject("Anaglyph_Left");
        leftObject.transform.SetParent(transform, false);
        leftText = leftObject.AddComponent<TextMeshProUGUI>();
        CopyTextSettings(sourceText, leftText);
        leftText.color = leftColor;
        
        // Create right (cyan) layer - rendered second (middle)
        rightObject = new GameObject("Anaglyph_Right");
        rightObject.transform.SetParent(transform, false);
        rightText = rightObject.AddComponent<TextMeshProUGUI>();
        CopyTextSettings(sourceText, rightText);
        rightText.color = rightColor;
        
        // Create main (white) layer - rendered last (on top)
        mainObject = new GameObject("Anaglyph_Main");
        mainObject.transform.SetParent(transform, false);
        mainText = mainObject.AddComponent<TextMeshProUGUI>();
        CopyTextSettings(sourceText, mainText);
        mainText.color = mainColor;
        
        // Hide the original source text since we're using our copy
        sourceText.enabled = false;
    }
    
    private void CopyTextSettings(TextMeshProUGUI source, TextMeshProUGUI target)
    {
        target.font = source.font;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.alignment = source.alignment;
        target.text = source.text;
        
        // Match RectTransform size but position at zero (relative to parent)
        RectTransform sourceRect = source.GetComponent<RectTransform>();
        RectTransform targetRect = target.GetComponent<RectTransform>();
        
        // Stretch to fill parent
        targetRect.anchorMin = Vector2.zero;
        targetRect.anchorMax = Vector2.one;
        targetRect.pivot = sourceRect.pivot;
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;
        targetRect.anchoredPosition = Vector2.zero;
    }
    
    private void UpdateEffect()
    {
        if (leftText == null || rightText == null || mainText == null) return;
        
        // Update text content on all layers
        leftText.text = sourceText.text;
        rightText.text = sourceText.text;
        mainText.text = sourceText.text;
        
        // Update font settings in case they changed
        leftText.font = sourceText.font;
        leftText.fontSize = sourceText.fontSize;
        leftText.fontStyle = sourceText.fontStyle;
        leftText.alignment = sourceText.alignment;
        
        rightText.font = sourceText.font;
        rightText.fontSize = sourceText.fontSize;
        rightText.fontStyle = sourceText.fontStyle;
        rightText.alignment = sourceText.alignment;
        
        mainText.font = sourceText.font;
        mainText.fontSize = sourceText.fontSize;
        mainText.fontStyle = sourceText.fontStyle;
        mainText.alignment = sourceText.alignment;
        
        // Position the offset layers (if not animating, animation will handle this)
        if (!animateOffset)
        {
            ApplyOffset(offset);
        }
    }
    
    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
        if (!animateOffset)
        {
            ApplyOffset(offset);
        }
    }
    
    public void SetAnimated(bool animate)
    {
        animateOffset = animate;
        if (!animate)
        {
            currentAnimOffset = Vector2.zero;
            ApplyOffset(offset);
        }
    }
    
    public void SetColors(Color left, Color right, Color main)
    {
        leftColor = left;
        rightColor = right;
        mainColor = main;
        
        if (leftText != null) leftText.color = leftColor;
        if (rightText != null) rightText.color = rightColor;
        if (mainText != null) mainText.color = mainColor;
    }
    
    void OnDestroy()
    {
        if (leftObject != null) Destroy(leftObject);
        if (rightObject != null) Destroy(rightObject);
        if (mainObject != null) Destroy(mainObject);
        
        // Re-enable source text if we're being destroyed
        if (sourceText != null) sourceText.enabled = true;
    }
    
    // Call this if you manually change the source text and want immediate update
    public void ForceUpdate()
    {
        UpdateEffect();
    }
}
#endif