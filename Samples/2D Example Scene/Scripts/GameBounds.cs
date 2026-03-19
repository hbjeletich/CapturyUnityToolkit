using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBounds : MonoBehaviour
{
    public BorderHitEffect borderHitEffect;
    private Collider2D col;
    public float xMin { get; private set; }
    public float xMax { get; private set; }

    public float yMin { get; private set; }
    public float yMax { get; private set; }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        UpdateBounds();
    }

    public void UpdateBounds()
    {
        Bounds b = col.bounds;
        xMin = b.min.x;
        xMax = b.max.x;
        yMin = b.min.y;
        yMax = b.max.y;
    }

    public void ActivateCollider(bool active)
    {
        Debug.Log($"GameBounds: Setting collider for {gameObject.name} active: {active}");
        col.isTrigger = active;
    }


}
