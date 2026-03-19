using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TrailRenderer))]
public class BallTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private float trailTime = 0.15f;
    [SerializeField] private float startWidth = 0.3f;
    [SerializeField] private float endWidth = 0.05f;
    
    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color player1Color = new Color(1f, 0f, 1f); // Magenta
    [SerializeField] private Color player2Color = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private float glowAlpha = 0.8f;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeBackDuration = 1.5f; // Time to fade back to white
    
    private TrailRenderer trail;
    private int lastHitPlayer = 0;
    private Coroutine fadeCoroutine;
    
    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        SetupTrail();
    }
    
    void Start()
    {
        // Sync colors with UI manager if available
        if (PongUIManager.Instance != null)
        {
            player1Color = PongUIManager.Instance.GetPlayerColor(1);
            player2Color = PongUIManager.Instance.GetPlayerColor(2);
        }
        
        SetTrailColor(defaultColor);
    }
    
    private void SetupTrail()
    {
        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.minVertexDistance = 0.05f;
        
        trail.numCapVertices = 5;
        trail.numCornerVertices = 5;
        
        SetTrailColor(defaultColor);
    }
    
    public void OnPaddleHit(int playerNumber)
    {
        lastHitPlayer = playerNumber;
        
        // Stop any existing fade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        // Immediately pulse to player color
        Color hitColor = playerNumber == 1 ? player1Color : player2Color;
        SetTrailColor(hitColor);
        
        // Start fading back to white
        fadeCoroutine = StartCoroutine(FadeToDefault(hitColor));
        
        Debug.Log($"BallTrail: Pulsed to Player {playerNumber} color, fading back to white");
    }
    
    private IEnumerator FadeToDefault(Color startColor)
    {
        float elapsed = 0f;
        
        while (elapsed < fadeBackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeBackDuration;
            
            // Ease out for smooth fade
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            
            Color currentColor = Color.Lerp(startColor, defaultColor, easedT);
            SetTrailColor(currentColor);
            
            yield return null;
        }
        
        SetTrailColor(defaultColor);
        fadeCoroutine = null;
    }
    
    private void SetTrailColor(Color baseColor)
    {
        // Create gradient: bright core fading to transparent
        Gradient gradient = new Gradient();
        
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(Color.white, 0f),      // Bright white core
                new GradientColorKey(baseColor, 0.3f),      // Transition to color
                new GradientColorKey(baseColor, 1f)         // Fade out in color
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(glowAlpha, 0.3f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        
        trail.colorGradient = gradient;
    }
    
    public void ResetTrail()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        lastHitPlayer = 0;
        SetTrailColor(defaultColor);
        trail.Clear();
    }
}