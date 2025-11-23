using UnityEngine;

/// <summary>
/// Attach this to the shield visual GameObject (must have a trigger Collider).
/// When the shield is active and an obstacle touches it, the obstacle will be returned to the pool (if PoolManager exists)
/// or destroyed. This prevents the obstacle from damaging the player.
/// </summary>
public class ShieldImpactHandler : MonoBehaviour
{
    [Tooltip("Reference to the ShieldController (auto-resolved to parent if left empty)")]
    public ShieldController shieldController;
    [Tooltip("Tag used by obstacle prefabs")]
    public string obstacleTag = "Obstacle";
    [Tooltip("Optional VFX spawned where the obstacle was destroyed/returned")]
    public GameObject impactVFX;
    [Tooltip("Points awarded when an obstacle is destroyed by the shield")]
    public int scorePerImpact = 10;
    [Tooltip("Optional reference to GameController to add score; if empty it will be resolved at runtime")]
    public GameController gameController;

    void Awake()
    {
        if (shieldController == null)
            shieldController = GetComponentInParent<ShieldController>();
        if (gameController == null)
            gameController = FindAnyObjectByType<GameController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag(obstacleTag)) return;
        if (shieldController == null || !shieldController.IsShieldActive()) return;

        HandleImpact(other.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag(obstacleTag)) return;
        if (shieldController == null || !shieldController.IsShieldActive()) return;

        HandleImpact(other.gameObject);
    }

    void HandleImpact(GameObject obstacle)
    {
        if (impactVFX != null)
        {
            // Instantiate VFX at obstacle position, play any ParticleSystems and destroy after they finish
            var vfxInstance = Instantiate(impactVFX, obstacle.transform.position, Quaternion.identity);

            // Try to play particle systems and compute lifetime to schedule destroy
            var systems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
            float maxLifetime = 0f;
            foreach (var ps in systems)
            {
                var main = ps.main;
                // startLifetime can be a constant or a range; use constantMax as conservative estimate when available
                float lifetime = 0f;
                try
                {
                    lifetime = main.duration + main.startLifetime.constantMax;
                }
                catch
                {
                    // Fallback: try constant
                    lifetime = main.duration + main.startLifetime.constant;
                }
                if (lifetime > maxLifetime) maxLifetime = lifetime;
                ps.Play();
            }

            // If no ParticleSystem found, still destroy after a small timeout to avoid clutter
            if (maxLifetime <= 0f) maxLifetime = 2f;
            Destroy(vfxInstance, maxLifetime + 0.25f);
        }
        // Award points if a GameController is available
        if (gameController == null)
            gameController = FindAnyObjectByType<GameController>();
        if (gameController != null && scorePerImpact != 0)
        {
            gameController.AddScore(scorePerImpact);
        }

        // If there's a pool manager, return the object to the pool. Otherwise destroy it.
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(obstacle);
        }
        else
        {
            Destroy(obstacle);
        }
    }
}
