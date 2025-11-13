using UnityEngine;

/// <summary>
/// Applies a random angular velocity when spawned. Implements IPooledObject so it works with PoolManager reuse.
/// </summary>
public class RandomRotation : MonoBehaviour, IPooledObject
{
    private Rigidbody rb;
    [Tooltip("Maximum angular magnitude applied as initial tumble")]
    public float tumble = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        // Ensure rotation applied when enabled (covers non-pooled activation)
        ApplyRandomAngularVelocity();
    }

    void ApplyRandomAngularVelocity()
    {
        if (rb == null) return;
        Vector3 angularVelocity = Random.insideUnitSphere * tumble;
        rb.angularVelocity = angularVelocity;
    }

    // IPooledObject
    public void OnObjectSpawn()
    {
        ApplyRandomAngularVelocity();
    }

    public void OnObjectReturn()
    {
        if (rb != null)
            rb.angularVelocity = Vector3.zero;
    }
}
