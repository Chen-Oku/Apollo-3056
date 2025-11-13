using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif


public class OnScreenJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    [Tooltip("RectTransform for joystick background (drag area). Should be a child of a Canvas that covers the screen")]
    [SerializeField] RectTransform background;

    [Tooltip("RectTransform for joystick handle (moveable) - child of background")]
    [SerializeField] RectTransform handle;

    [Header("Settings")]
    [Tooltip("Maximum handle movement (in pixels) from center")]
    [SerializeField] float handleRange = 100f;

    [Tooltip("If true, the joystick appears where the user touches the screen")]
    [SerializeField] bool dynamic = true;

    [Tooltip("If dynamic, hide the background until the player touches the screen")]
    [SerializeField] bool hideBackgroundWhenIdle = true;

    // Internal state
    Vector2 _input = Vector2.zero;
    int _activePointerId = -1; // -1 none, -2 mouse

    // Cached refs
    Canvas _canvas;
    RectTransform _canvasRect;

    /// <summary>Normalized joystick direction in range [-1,1].</summary>
    public Vector2 Direction => _input;

    /// <summary>True when joystick is currently active (pointer down)</summary>
    public bool IsActive => _activePointerId != -1;

    void Awake()
    {
        // Cache canvas (if any) for correct ScreenPoint conversions
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null)
            _canvasRect = _canvas.GetComponent<RectTransform>();

        if (background == null || handle == null)
            Debug.LogWarning("OnScreenJoystick: assign 'background' and 'handle' in the inspector.");
    }

    void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // Enable EnhancedTouch only when using the new Input System pipeline
        EnhancedTouchSupport.Enable();
#endif

        if (dynamic && hideBackgroundWhenIdle && background != null)
            background.gameObject.SetActive(false);
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EnhancedTouchSupport.Disable();
#endif
        // Reset state
        _activePointerId = -1;
        _input = Vector2.zero;
    }

    void Update()
    {
        // Prefer new Input System when project is configured for it
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        HandleEnhancedTouch();
        HandleMouseViaInputSystem();
#else
        HandleLegacyTouch();
        HandleMouseLegacy();
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    void HandleEnhancedTouch()
    {
    var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
    if (touches.Count == 0) return;

        foreach (var t in touches)
        {
            int id = t.touchId;
            var phase = t.phase;
            var pos = t.screenPosition;

            if (phase == UnityEngine.InputSystem.TouchPhase.Began && _activePointerId == -1)
                BeginPointer(id, pos);
            else if (id == _activePointerId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                    ProcessPointer(pos);
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    EndPointer();
            }
        }
    }

    void HandleMouseViaInputSystem()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        var pos = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame && _activePointerId == -1)
            BeginPointer(-2, pos);
        else if (mouse.leftButton.isPressed && _activePointerId == -2)
            ProcessPointer(pos);
        else if (mouse.leftButton.wasReleasedThisFrame && _activePointerId == -2)
            EndPointer();
    }
#endif

    void HandleLegacyTouch()
    {
        if (Input.touchCount == 0) return;

        foreach (UnityEngine.Touch t in Input.touches)
        {
            if (t.phase == UnityEngine.TouchPhase.Began && _activePointerId == -1)
                BeginPointer(t.fingerId, t.position);
            else if (t.fingerId == _activePointerId)
            {
                if (t.phase == UnityEngine.TouchPhase.Moved || t.phase == UnityEngine.TouchPhase.Stationary)
                    ProcessPointer(t.position);
                else if (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled)
                    EndPointer();
            }
        }
    }

    void HandleMouseLegacy()
    {
        if (!Application.isMobilePlatform)
        {
            if (Input.GetMouseButtonDown(0) && _activePointerId == -1)
                BeginPointer(-2, Input.mousePosition);
            else if (Input.GetMouseButton(0) && _activePointerId == -2)
                ProcessPointer(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0) && _activePointerId == -2)
                EndPointer();
        }
    }

    /// <summary>
    /// Called when a pointer begins. pointerId: -2 = mouse, >=0 = touch id from Input System or legacy
    /// </summary>
    void BeginPointer(int pointerId, Vector2 screenPos)
    {
        _activePointerId = pointerId;

        if (background == null || handle == null) return;

        if (dynamic && hideBackgroundWhenIdle)
            background.gameObject.SetActive(true);

        // Place background using Canvas rect as reference (preferred) to avoid offsets
        var reference = _canvasRect != null ? _canvasRect : background.parent as RectTransform;
        var cam = _canvas != null ? _canvas.worldCamera : null;
        if (reference != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, screenPos, cam, out Vector2 localPoint))
        {
            background.anchoredPosition = localPoint;
            background.SetAsLastSibling(); // bring to front
        }

        handle.anchoredPosition = Vector2.zero;
        _input = Vector2.zero;
    }

    /// <summary>
    /// Convert screenPos to relative handle position and update input
    /// </summary>
    void ProcessPointer(Vector2 screenPos)
    {
        if (background == null || handle == null) return;

        var reference = _canvasRect != null ? _canvasRect : background.parent as RectTransform;
        var cam = _canvas != null ? _canvas.worldCamera : null;
        if (reference == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, screenPos, cam, out Vector2 localPoint))
            return;

        Vector2 bgCenter = background.anchoredPosition;
        Vector2 relative = localPoint - bgCenter;

        Vector2 clamped = Vector2.ClampMagnitude(relative, handleRange);
        handle.anchoredPosition = clamped;
        _input = clamped / (handleRange <= 0f ? 1f : handleRange);
    }

    void EndPointer()
    {
        _activePointerId = -1;
        _input = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;

        if (dynamic && hideBackgroundWhenIdle && background != null)
            background.gameObject.SetActive(false);
    }

    // UI pointer handlers - still supported if you prefer using EventSystem raycasts
    public void OnPointerDown(PointerEventData eventData) => BeginPointer(eventData.pointerId, eventData.position);
    public void OnDrag(PointerEventData eventData) => ProcessPointer(eventData.position);
    public void OnPointerUp(PointerEventData eventData) => EndPointer();
}
