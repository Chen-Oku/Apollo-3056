using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controla las vidas del jugador. Cuando un obstáculo choca físicamente con la nave,
/// resta una vida (o ninguna si el escudo está activo) y notifica a GameController
/// cuando se acaban las vidas.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Tooltip("Vidas iniciales del jugador")]
    public int maxLives = 3;
    [Tooltip("Tag usado por los obstáculos")]
    public string obstacleTag = "Obstacle";

    [Header("Referencias")]
    public ShieldController shield;
    public NearMissDetector nearMissDetector;
    public GameController gameController;

    [Header("Feedback")]
    [Tooltip("VFX (sistema de partículas) instanciado en el punto de impacto al recibir daño")]
    public GameObject hitVFX;

    [Header("Eventos")]
    public UnityEvent<int> OnLivesChanged; // vidas restantes
    public UnityEvent OnPlayerHit; // golpe que resta una vida (sin escudo)
    public UnityEvent OnPlayerDied;

    int _lives;
    bool _isDead;

    public int CurrentLives => _lives;
    public int MaxLives => maxLives;

    void Awake()
    {
        _lives = maxLives;
        if (shield == null) shield = GetComponent<ShieldController>();
        if (nearMissDetector == null) nearMissDetector = GetComponentInChildren<NearMissDetector>();
        if (gameController == null) gameController = FindAnyObjectByType<GameController>();
    }

    void Start()
    {
        OnLivesChanged?.Invoke(_lives);
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleObstacle(collision.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleObstacle(collision.gameObject);
    }

    void HandleObstacle(GameObject obstacle)
    {
        if (_isDead) return;
        if (!obstacle.CompareTag(obstacleTag)) return;

        // Evita que NearMissDetector premie este obstáculo como "casi choque"
        if (nearMissDetector != null) nearMissDetector.MarkHit(obstacle);

        bool shielded = shield != null && shield.IsShieldActive();
        Vector3 impactPosition = obstacle.transform.position;

        ReturnObstacle(obstacle);

        if (shielded) return; // el escudo absorbe el golpe, no se resta vida

        TakeDamage(1, impactPosition);
    }

    void ReturnObstacle(GameObject obstacle)
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.ReturnToPool(obstacle);
        else
            Destroy(obstacle);
    }

    void TakeDamage(int amount, Vector3 impactPosition)
    {
        _lives = Mathf.Max(0, _lives - amount);

        SpawnHitVFX(impactPosition);

        OnPlayerHit?.Invoke();
        OnLivesChanged?.Invoke(_lives);

        if (_lives <= 0 && !_isDead)
        {
            _isDead = true;
            OnPlayerDied?.Invoke();
            if (gameController != null) gameController.GameOver();
        }
    }

    void SpawnHitVFX(Vector3 position)
    {
        if (hitVFX == null) return;

        var vfxInstance = Instantiate(hitVFX, position, Quaternion.identity);

        var systems = vfxInstance.GetComponentsInChildren<ParticleSystem>();
        float maxLifetime = 0f;
        foreach (var ps in systems)
        {
            var main = ps.main;
            float lifetime = main.duration + main.startLifetime.constantMax;
            if (lifetime > maxLifetime) maxLifetime = lifetime;
            ps.Play();
        }

        if (maxLifetime <= 0f) maxLifetime = 2f;
        Destroy(vfxInstance, maxLifetime + 0.25f);
    }
}
