using System;
using UnityEngine;
using UnityEngine.Events;

public class ShieldController : MonoBehaviour
{
    [Tooltip("Carga máxima del medidor (por ejemplo 100 = valor entero para mostrar 0..100)")]
    public float maxCharge = 100f;
    [Tooltip("Cantidad añadida por cada near-miss en las mismas unidades que maxCharge (por ejemplo 15 = 15 puntos)")]
    public float chargePerNearMiss = 15f;
    [Tooltip("Duración del escudo cuando se activa (segundos)")]
    public float shieldDuration = 5f;
    [Tooltip("Tiempo de recarga después de desactivar (segundos)")]
    public float cooldown = 10f;

    [Header("Visual / Collision")]
    [Tooltip("Optional visual GameObject (e.g. a sphere) that will be enabled while shield is active")]
    public GameObject shieldVisual;
    [Tooltip("If true, the controller will toggle physics layer collisions between playerLayer and obstacleLayer while shield is active")]
    public bool useLayerCollisionToggle = true;
    [Tooltip("Layer index used by the player GameObject (set in Inspector to match your project)")]
    public int playerLayer = 8;
    [Tooltip("Layer index used by obstacles (set in Inspector to match your project)")]
    public int obstacleLayer = 9;

    public UnityEvent<float> OnChargeChanged; // param: currentCharge (0..1)
    public UnityEvent OnShieldActivated;
    public UnityEvent OnShieldDeactivated;

    float _charge = 0f; // 0..maxCharge
    bool _isActive = false;
    bool _isOnCooldown = false;
    float _cooldownTimer = 0f;

    public float CurrentCharge => _charge;
    public bool IsAvailable => !_isOnCooldown && !_isActive && _charge >= maxCharge - 1e-3f;

    void Update()
    {
        if (_isOnCooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _isOnCooldown = false;
                // Optionally notify UI that cooldown finished (reuse OnChargeChanged or new event)
            }
        }
    }

    public void AddCharge(float amount)
    {
        if (_isActive || _isOnCooldown) return;
        _charge = Mathf.Clamp(_charge + amount, 0f, maxCharge);
        OnChargeChanged?.Invoke(_charge);
        if (_charge >= maxCharge)
        {
            // optionally auto-activate or notify ready
        }
    }

    public void SetCharge(float amount)
    {
        _charge = Mathf.Clamp(amount, 0f, maxCharge);
        OnChargeChanged?.Invoke(_charge);
    }

    public bool TryActivate()
    {
        if (_isActive || _isOnCooldown || _charge < maxCharge - 1e-3f) return false;
        StartCoroutine(ActivateRoutine());
        return true;
    }

    System.Collections.IEnumerator ActivateRoutine()
    {
        _isActive = true;
        // Show visual and block collisions if configured
        if (shieldVisual != null) shieldVisual.SetActive(true);
        if (useLayerCollisionToggle)
        {
            Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, true);
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, true);
        }

        OnShieldActivated?.Invoke();
        // consume full charge (or keep some leftover if desired)
        _charge = 0f;
        OnChargeChanged?.Invoke(_charge);

        float t = shieldDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        _isActive = false;
        OnShieldDeactivated?.Invoke();

        // restore collisions and visuals
        if (shieldVisual != null) shieldVisual.SetActive(false);
        if (useLayerCollisionToggle)
        {
            Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, false);
            Physics2D.IgnoreLayerCollision(playerLayer, obstacleLayer, false);
        }

        // start cooldown
        _isOnCooldown = true;
        _cooldownTimer = cooldown;
    }

    // Optional: expose whether shield currently blocks damage
    public bool IsShieldActive() => _isActive;

    public void Activate() {
        TryActivate();
    }
}