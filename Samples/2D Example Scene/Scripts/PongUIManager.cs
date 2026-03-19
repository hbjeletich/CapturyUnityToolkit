using System.Collections;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

public class PongUIManager : MonoBehaviour
{
    [Header("UI References")]
    #if TMP_PRESENT
    [SerializeField] private TextMeshProUGUI centerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    #endif
    [SerializeField] private CanvasGroup scoreCanvasGroup;
    
    [Header("Colors")]
    [SerializeField] private Color player1Color = new Color(1f, 0f, 1f);
    [SerializeField] private Color player2Color = new Color(0f, 1f, 1f);
    [SerializeField] private Color defaultTextColor = Color.white;
    
    [Header("Animation Settings")]
    [SerializeField] private float countdownInterval = 1f;
    [SerializeField] private float textPulseScale = 1.3f;
    [SerializeField] private float scoreAnimDuration = 0.5f;
    [SerializeField] private float glowIntensity = 2f;
    
    private Vector3 centerTextOriginalScale;
    private Vector3 scoreTextOriginalScale;
    private int player1Score = 0;
    private int player2Score = 0;
    
    private Material centerTextMaterial;
    private Material scoreTextMaterial;
    
    public static PongUIManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        #if TMP_PRESENT
        if (centerText != null)
        {
            centerTextOriginalScale = centerText.transform.localScale;
            centerTextMaterial = centerText.fontMaterial;
        }
        
        if (scoreText != null)
        {
            scoreTextOriginalScale = scoreText.transform.localScale;
            scoreTextMaterial = scoreText.fontMaterial;
            scoreText.gameObject.SetActive(false);
        }
        #endif
        
        if (scoreCanvasGroup != null)
        {
            scoreCanvasGroup.alpha = 0f;
        }
        
        ShowIntroText();
    }
    
    public void ShowIntroText()
    {
        #if TMP_PRESENT
        if (centerText != null)
        {
            centerText.text = "RAISE BOTH HANDS\nTO START";
            centerText.color = defaultTextColor;
            centerText.gameObject.SetActive(true);
            StartCoroutine(PulseText(centerText, centerTextOriginalScale, true));
        }
        #endif
    }
    
    public void StartCountdown()
    {
        StartCoroutine(CountdownSequence());
    }
    
    private IEnumerator CountdownSequence()
    {
        #if TMP_PRESENT
        StopCoroutine(nameof(PulseText));
        centerText.transform.localScale = centerTextOriginalScale;
        
        string[] countdownTexts = { "3", "2", "1", "GO!" };
        
        foreach (int i in System.Linq.Enumerable.Range(0, countdownTexts.Length))
        {
            centerText.text = countdownTexts[i];
            centerText.color = defaultTextColor;
            
            yield return StartCoroutine(PunchScale(centerText.transform, centerTextOriginalScale, textPulseScale, 0.3f));
            
            if (i < countdownTexts.Length - 1)
            {
                yield return new WaitForSeconds(countdownInterval - 0.3f);
            }
        }

        #endif
        
        yield return new WaitForSeconds(0.5f);
        
        #if TMP_PRESENT
        centerText.gameObject.SetActive(false);
        #endif
        
        ShowScore();
    }
    
    private void ShowScore()
    {
        #if TMP_PRESENT
        scoreText.gameObject.SetActive(true);
        UpdateScoreText();
        StartCoroutine(AnimateScoreIn());
        #endif
    }
    
    private IEnumerator AnimateScoreIn()
    {
        #if TMP_PRESENT
        float elapsed = 0f;
        
        scoreText.transform.localScale = Vector3.zero;
        if (scoreCanvasGroup != null) scoreCanvasGroup.alpha = 0f;
        
        while (elapsed < scoreAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scoreAnimDuration;
            
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
            
            scoreText.transform.localScale = scoreTextOriginalScale * easeOut * overshoot;
            
            if (scoreCanvasGroup != null)
            {
                scoreCanvasGroup.alpha = easeOut;
            }
            
            SetTextGlow(scoreTextMaterial, glowIntensity * (1f - t));
            
            yield return null;
        }
        
        scoreText.transform.localScale = scoreTextOriginalScale;
        if (scoreCanvasGroup != null) scoreCanvasGroup.alpha = 1f;
        SetTextGlow(scoreTextMaterial, 0f);
        #endif

        yield return null;
    }
    
    public void Player1Scored()
    {
        player1Score++;
        UpdateScoreText();
        #if TMP_PRESENT
        StartCoroutine(ScorePulse(player1Color));
        #endif
    }
    
    public void Player2Scored()
    {
        player2Score++;
        UpdateScoreText();
        #if TMP_PRESENT
        StartCoroutine(ScorePulse(player2Color));
        #endif
    }
    
    private void UpdateScoreText()
    {
        #if TMP_PRESENT
        if (scoreText != null)
        {
            scoreText.text = $"{player1Score} - {player2Score}";
        }
        #endif
    }
    
    #if TMP_PRESENT
    private IEnumerator ScorePulse(Color pulseColor)
    {
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            scoreText.transform.localScale = scoreTextOriginalScale * scale;
            
            SetTextGlow(scoreTextMaterial, glowIntensity * (1f - t));
            
            yield return null;
        }
        
        scoreText.transform.localScale = scoreTextOriginalScale;
        SetTextGlow(scoreTextMaterial, 0f);
    }
    
    private IEnumerator PulseText(TextMeshProUGUI text, Vector3 originalScale, bool loop)
    {
        float pulseSpeed = 2f;
        float pulseAmount = 0.05f;
        
        while (loop && text.gameObject.activeSelf)
        {
            float scale = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
            text.transform.localScale = originalScale * scale;
            yield return null;
        }
    }
    
    private IEnumerator PunchScale(Transform target, Vector3 originalScale, float punchScale, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float scale;
            if (t < 0.3f)
            {
                scale = Mathf.Lerp(1f, punchScale, t / 0.3f);
            }
            else
            {
                scale = Mathf.Lerp(punchScale, 1f, (t - 0.3f) / 0.7f);
            }
            
            target.localScale = originalScale * scale;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    private void SetTextGlow(Material mat, float intensity)
    {
        if (mat != null && mat.HasProperty("_GlowPower"))
        {
            mat.SetFloat("_GlowPower", intensity);
        }
    }

    #endif
    
    public Color GetPlayerColor(int playerNumber)
    {
        return playerNumber == 1 ? player1Color : player2Color;
    }
}