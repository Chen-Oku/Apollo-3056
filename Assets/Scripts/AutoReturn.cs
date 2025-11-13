using System.Collections;
using UnityEngine;

/// <summary>
/// AutoReturn: component that enforces a lifetime for a GameObject.
/// - If a poolTag is provided and PoolManager contains that tag, the object will be returned to the pool when time expires.
/// - Otherwise the object will be destroyed when the timer expires.
/// - Implements IPooledObject so it restarts the timer when the object is spawned from a pool.
/// </summary>
public class AutoReturn : MonoBehaviour, IPooledObject
{
    [Tooltip("Lifetime in seconds. If <= 0, the auto-return is disabled.")]
    public float lifeTime = 5f;

    [Tooltip("Optional: pool tag to return this object to. If empty or pool not found, object is destroyed on expiry.")]
    public string poolTag;

    Coroutine _lifeCoroutine;

    void OnEnable()
    {
        StartTimerIfNeeded();
    }

    void OnDisable()
    {
        StopTimer();
    }

    void StartTimerIfNeeded()
    {
        if (lifeTime > 0f)
        {
            // ensure only one coroutine
            if (_lifeCoroutine != null) StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = StartCoroutine(LifeCoroutine());
        }
    }

    void StopTimer()
    {
        if (_lifeCoroutine != null)
        {
            StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = null;
        }
    }

    IEnumerator LifeCoroutine()
    {
        yield return new WaitForSeconds(lifeTime);
        ExpireNow();
    }

    void ExpireNow()
    {
        // Prefer returning to pool if a valid pool tag is set
        if (!string.IsNullOrEmpty(poolTag) && PoolManager.Instance != null && PoolManager.Instance.HasPool(poolTag))
        {
            PoolManager.Instance.ReturnToPool(gameObject);
            return;
        }

        // Otherwise, if this object is part of a pool but poolTag wasn't assigned, attempt best-effort destruction
        if (PoolManager.Instance != null)
        {
            // We don't have a direct mapping from instance->tag, so fall back to Destroy to avoid orphaned objects
            Destroy(gameObject);
            return;
        }

        // No PoolManager present: destroy the object
        Destroy(gameObject);
    }

    // IPooledObject callbacks
    public void OnObjectSpawn()
    {
        // Restart lifetime when taken from pool
        StartTimerIfNeeded();
    }

    public void OnObjectReturn()
    {
        // Stop the timer when returned to pool
        StopTimer();
    }
}
