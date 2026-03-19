using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderHitEffect : MonoBehaviour
{
    private Material borderMaterial;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        borderMaterial = spriteRenderer.material;
    }
    
    public void TriggerHit(Vector3 worldHitPoint)
    {
            // Convert world position to sprite's local space
        Vector3 localPoint = transform.InverseTransformPoint(worldHitPoint);
        
        // Get sprite bounds
        Bounds bounds = spriteRenderer.sprite.bounds;
        
        Vector2 uv = new Vector2(
            1.0f - (localPoint.x - bounds.min.x) / bounds.size.x,  // Flip X
            1.0f - (localPoint.y - bounds.min.y) / bounds.size.y   // Flip Y
        );

        
        borderMaterial.SetVector("_HitPosition", uv);
        borderMaterial.SetFloat("_HitTime", Time.time);
        
        Debug.Log($"Hit at UV: {uv}");
    }
}