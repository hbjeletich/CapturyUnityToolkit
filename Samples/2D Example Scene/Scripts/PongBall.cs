using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongBall : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minHorizontalRatio = 0.3f; // Minimum X component to prevent vertical movement
    public float Speed { get => speed; set => speed = value; }
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        LaunchBall();
    }
    
    void LaunchBall()
    {
        float randomY = Random.Range(-0.7f, 0.7f);
        float randomX = Random.value > 0.5f ? 1f : -1f;
        
        Vector2 direction = new Vector2(randomX, randomY).normalized;
        rb.velocity = direction * speed;
        
        // Reset trail color
        BallTrail trail = GetComponent<BallTrail>();
        if (trail != null)
        {
            trail.ResetTrail();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Get current direction and add slight randomness
        Vector2 currentDir = rb.velocity.normalized;
        float randomAdjustment = Random.Range(-0.15f, 0.15f);
        
        Vector2 newDir = new Vector2(currentDir.x, currentDir.y + randomAdjustment).normalized;
        
        // Prevent near-vertical movement by enforcing minimum horizontal component
        if (Mathf.Abs(newDir.x) < minHorizontalRatio)
        {
            // Preserve the sign of X but enforce minimum magnitude
            float signX = newDir.x >= 0 ? 1f : -1f;
            newDir.x = signX * minHorizontalRatio;
            newDir = newDir.normalized;
        }
        
        rb.velocity = newDir * speed;

        if (collision.gameObject.CompareTag("Respawn"))
        {
            Debug.Log("PongBall: Hit border " + collision.gameObject.name);
            Vector3 hitPoint = collision.contacts[0].point;
        
            GameBounds gameBounds = collision.gameObject.GetComponent<GameBounds>();        
            BorderHitEffect glow = gameBounds.borderHitEffect;
            glow?.TriggerHit(hitPoint);
        }
    }
}