using UnityEngine;

/// <summary>
/// Simple mover: sets Rigidbody velocity when spawned/activated.
/// Implements IPooledObject so it also works with PoolManager reuse.
/// </summary>
public class Mover : MonoBehaviour, IPooledObject
{
    [Tooltip("Movement speed in units/second")]
    public float speed;

    Rigidbody rb;
    float _baseSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _baseSpeed = speed;
    }

    void OnEnable()
    {
        // In case this object wasn't pooled or Start is relied upon, ensure velocity is set when enabled
        ApplyVelocity();
    }

    void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = transform.forward * speed;
    }

    // IPooledObject: called by PoolManager when object is taken from the pool
    public void OnObjectSpawn()
    {
        ApplyVelocity();
    }

    // IPooledObject: called by PoolManager when object is returned to pool
    public void OnObjectReturn()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // restore inspector/base speed when returned to pool
        speed = _baseSpeed;
    }

    /// <summary>
    /// Apply a multiplier to the original base speed for this spawn.
    /// This does not modify the saved base speed.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speed = _baseSpeed * multiplier;
        ApplyVelocity();
    }
}
