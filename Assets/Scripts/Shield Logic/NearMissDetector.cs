using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NearMissDetector : MonoBehaviour
{
    [Tooltip("Tag used by obstacles")]
    public string obstacleTag = "Obstacle";
    [Tooltip("How much charge to add per near-miss (use same units as ShieldController.maxCharge, e.g. 15 = 15 points)")]
    public float chargePerNearMiss = 15f;

    // obstacles currently inside the near-miss trigger
    HashSet<GameObject> _candidates = new HashSet<GameObject>();
    // obstacles that actually hit player (so we won't award near-miss)
    HashSet<GameObject> _hitSet = new HashSet<GameObject>();

    public ShieldController shield;

    void Reset()
    {
        // Ensure collider is trigger by default (for convenience)
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
#if UNITY_2D
        var col2 = GetComponent<Collider2D>();
        if (col2 != null) col2.isTrigger = true;
#endif
    }

    // 3D:
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(obstacleTag)) return;
        _candidates.Add(other.gameObject);
    }
    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(obstacleTag)) return;
        var go = other.gameObject;
        if (_candidates.Remove(go))
        {
            if (!_hitSet.Contains(go))
            {
                AwardNearMiss(go);
            }
            _hitSet.Remove(go);
        }
    }

    // 2D:
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(obstacleTag)) return;
        _candidates.Add(other.gameObject);
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(obstacleTag)) return;
        var go = other.gameObject;
        if (_candidates.Remove(go))
        {
            if (!_hitSet.Contains(go))
            {
                AwardNearMiss(go);
            }
            _hitSet.Remove(go);
        }
    }

    // Called by player's collision handling when an obstacle actually collides.
    public void MarkHit(GameObject obstacle)
    {
        if (obstacle == null) return;
        _hitSet.Add(obstacle);
    }

    void AwardNearMiss(GameObject obstacle)
    {
        // Optionally vary the amount by distance or velocity
        float amount = chargePerNearMiss;
        if (shield != null)
        {
            shield.AddCharge(amount);
        }
        // Optional: spawn VFX, sound, add streak counters, etc.
    }

    void OnCollisionEnter(Collision collision)
    {
        var near = GetComponent<NearMissDetector>();
        if (near != null && collision.gameObject.CompareTag("Obstacle"))
        {
            near.MarkHit(collision.gameObject);
        }
    }
}