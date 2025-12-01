using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PongBall : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        LaunchBall();
    }
    
    void LaunchBall()
    {
        float randomY = Random.Range(-1f, 1f);
        float randomX = Random.value > 0.5f ? 1f : -1f;
        
        Vector3 direction = new Vector3(randomX, randomY, 0f).normalized;
        rb.velocity = direction * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        rb.velocity = rb.velocity.normalized * speed;
        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y + Random.Range(-0.5f, 0.5f));
    }
}