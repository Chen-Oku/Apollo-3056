using UnityEngine;

/// <summary>
/// Hace parpadear el visual del escudo cada vez más rápido y brillante
/// a medida que se acerca el momento en que se desactivará.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ShieldFlicker : MonoBehaviour
{
    [Tooltip("Controlador del escudo (se autocompleta desde el padre si se deja vacío)")]
    public ShieldController shield;

    [Header("Aviso de escudo por agotarse")]
    [Tooltip("Segundos restantes a partir de los cuales el escudo empieza a parpadear")]
    public float warningTime = 2f;
    [Tooltip("Parpadeos por segundo al iniciar el aviso")]
    public float minFlickerSpeed = 4f;
    [Tooltip("Parpadeos por segundo justo antes de que el escudo se apague")]
    public float maxFlickerSpeed = 14f;
    [Tooltip("Multiplicador de brillo en el punto más brillante del parpadeo")]
    public float brightnessMultiplier = 2.5f;

    static readonly int ColorAID = Shader.PropertyToID("_ColorA");
    static readonly int ColorBID = Shader.PropertyToID("_ColorB");

    Renderer _renderer;
    MaterialPropertyBlock _propertyBlock;
    Color _baseColorA;
    Color _baseColorB;
    bool _isFlickering;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propertyBlock = new MaterialPropertyBlock();

        if (shield == null) shield = GetComponentInParent<ShieldController>();

        var mat = _renderer.sharedMaterial;
        _baseColorA = mat.GetColor(ColorAID);
        _baseColorB = mat.GetColor(ColorBID);
    }

    void Update()
    {
        if (shield == null || !shield.IsShieldActive() || shield.ShieldTimeRemaining > warningTime)
        {
            if (_isFlickering)
            {
                _renderer.SetPropertyBlock(null);
                _isFlickering = false;
            }
            return;
        }

        float warningProgress = 1f - Mathf.Clamp01(shield.ShieldTimeRemaining / warningTime);
        float speed = Mathf.Lerp(minFlickerSpeed, maxFlickerSpeed, warningProgress);
        float pulse = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(1f, brightnessMultiplier, pulse);

        _propertyBlock.SetColor(ColorAID, ScaleRGB(_baseColorA, intensity));
        _propertyBlock.SetColor(ColorBID, ScaleRGB(_baseColorB, intensity));
        _renderer.SetPropertyBlock(_propertyBlock);
        _isFlickering = true;
    }

    static Color ScaleRGB(Color c, float factor)
    {
        return new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
    }
}
