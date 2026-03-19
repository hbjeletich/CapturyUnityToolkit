using System.Collections;
using UnityEngine;

public class PaddleHitEffect : MonoBehaviour
{
    [Header("Scale Punch Settings")]
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.15f;
    
    [Header("Flash Settings")]
    [SerializeField] private float flashIntensity = 2f;
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    private Vector3 originalScale;
    private Color originalColor;
    private Color playerColor;
    private Coroutine currentEffect;
    private PongPlayer pongPlayer;
    
    void Awake()
    {
        originalScale = transform.localScale;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        pongPlayer = GetComponent<PongPlayer>();
    }
    
    void Start()
    {
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Get player color from UI manager
        if (PongUIManager.Instance != null && pongPlayer != null)
        {
            playerColor = PongUIManager.Instance.GetPlayerColor(pongPlayer.PlayerNumber);
        }
        else
        {
            playerColor = Color.white;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit the ball
        if (collision.gameObject.name.Contains("Ball") || collision.gameObject.GetComponent<PongBall>() != null)
        {
            TriggerHitEffect();
            
            // Notify the ball trail to change color
            BallTrail ballTrail = collision.gameObject.GetComponentInChildren<BallTrail>();
            if (ballTrail != null && pongPlayer != null)
            {
                ballTrail.OnPaddleHit(pongPlayer.PlayerNumber);
            }
        }
    }
    
    public void TriggerHitEffect()
    {
        if (currentEffect != null)
        {
            StopCoroutine(currentEffect);
            // Reset to original state
            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
        
        currentEffect = StartCoroutine(HitEffectSequence());
    }
    
    private IEnumerator HitEffectSequence()
    {
        float elapsed = 0f;
        float totalDuration = Mathf.Max(punchDuration, flashDuration);
        
        // Calculate flash color (intensified player color)
        Color flashColor = new Color(
            Mathf.Min(playerColor.r * flashIntensity, 1f),
            Mathf.Min(playerColor.g * flashIntensity, 1f),
            Mathf.Min(playerColor.b * flashIntensity, 1f),
            1f
        );
        
        // For true HDR glow, we can go above 1 if using HDR rendering
        Color hdrFlashColor = playerColor * flashIntensity;
        
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            
            // Scale punch - quick up, slower down
            if (elapsed < punchDuration)
            {
                float scaleT = elapsed / punchDuration;
                float scaleCurve;
                
                if (scaleT < 0.3f)
                {
                    // Quick scale up
                    scaleCurve = Mathf.Lerp(1f, punchScale, scaleT / 0.3f);
                }
                else
                {
                    // Slower ease back down
                    float downT = (scaleT - 0.3f) / 0.7f;
                    // Use elastic ease out for a snappy feel
                    float elastic = 1f + Mathf.Sin(downT * Mathf.PI * 2f) * (1f - downT) * 0.1f;
                    scaleCurve = Mathf.Lerp(punchScale, 1f, downT) * elastic;
                }
                
                transform.localScale = originalScale * scaleCurve;
            }
            
            // Color flash - quick bright flash, then fade
            if (elapsed < flashDuration && spriteRenderer != null)
            {
                float flashT = elapsed / flashDuration;
                
                if (flashT < 0.2f)
                {
                    // Quick flash to bright
                    spriteRenderer.color = Color.Lerp(originalColor, flashColor, flashT / 0.2f);
                }
                else
                {
                    // Fade back to original
                    float fadeT = (flashT - 0.2f) / 0.8f;
                    spriteRenderer.color = Color.Lerp(flashColor, originalColor, fadeT);
                }
            }
            
            yield return null;
        }
        
        // Ensure we end at original values
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        currentEffect = null;
    }
    
    public void UpdatePlayerColor(Color newColor)
    {
        playerColor = newColor;
    }
}