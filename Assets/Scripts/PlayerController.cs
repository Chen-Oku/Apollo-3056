using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador simple del jugador que conecta con el asset generado InputSystem_Actions.
/// Lee la acción Player.Move (Vector2) y mueve la nave.
/// - Usa Rigidbody2D si existe.
/// - Si no hay Rigidbody2D, usa Rigidbody (3D) y mapea Vector2 -> Vector3 (x -> x, y -> z).
/// - Si no hay Rigidbody, mueve el Transform directamente.
/// </summary>

[System.Serializable]
public class Boundary
{
    public float xMin, xMax, zMin, zMax;
}

public class PlayerController : MonoBehaviour
{
    [Tooltip("Velocidad de movimiento en unidades por segundo")]
    public float moveSpeed = 6f;
    public Boundary boundary;
    [Tooltip("Ángulo máximo de inclinación (grados) cuando se mueve en X")]
    public float maxTilt = 25f;
    [Tooltip("Rapidez con la que la nave interpola hacia la inclinación objetivo")]
    public float tiltSpeed = 8f;

    [Header("Responsiveness")]
    [Tooltip("Use Rigidbody.velocity instead of MovePosition for more immediate movement (reduces feel of input lag)")]
    public bool useVelocityMovement = true;
    [Tooltip("If true, the ship will instantly adopt the tilt angle instead of interpolating (snappier feel)")]
    public bool instantTilt = false;
    // Componentes opcionales: preferimos Rigidbody2D para un juego tipo top-down 2D.
    Rigidbody2D rb2d;
    Rigidbody rb3d;

    // Valor leído del input (x,y)
    Vector2 moveInput = Vector2.zero;

    // Clase generada por el Input System (archivo InputSystem_Actions.cs)
    private InputSystem_Actions controls;

    [Header("Mobile")]
    [Tooltip("Si se asigna, el PlayerController usará este joystick en pantalla en lugar del Input System (útil en móviles)")]
    public OnScreenJoystick mobileJoystick;

    void Awake()
    {
        // Cacheamos componentes si están presentes
        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();

        // Crear instancia del asset de input y registrar callbacks
        controls = new InputSystem_Actions();

        // Cuando la acción Move se realiza, guardamos el vector; cuando se cancela, lo ponemos a cero.
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnEnable()
    {
            controls?.Player.Enable();
    }

    void OnDisable()
    {
        // Deshabilitar el mapa y liberar la instancia del asset cuando la escena/objeto se desactive.
        if (controls != null)
        {
            controls.Player.Disable();
            // No llamar Dispose aquí si planeas re-usar la instancia en OnEnable de nuevo en la misma vida del objeto.
            // Hacemos Dispose en OnDestroy para asegurar limpieza al destruir el objeto.
        }
    }

    void OnDestroy()
    {
        if (controls != null)
        {
            controls.Dispose();
            controls = null;
        }
    }

    void Update()
    {
        // Read joystick every frame for more immediate response on mobile/editor UI
        if (mobileJoystick != null)
        {
            moveInput = mobileJoystick.Direction;
        }

        // If there is no Rigidbody, move the Transform here for more responsive feedback
        if (rb2d == null && rb3d == null)
        {
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.Self);

            if (boundary != null)
            {
                Vector3 p = transform.position;
                p.x = Mathf.Clamp(p.x, boundary.xMin, boundary.xMax);
                p.z = Mathf.Clamp(p.z, boundary.zMin, boundary.zMax);
                transform.position = p;
            }

            // Tilt (instant or interpolated)
            Quaternion targetRotFallback = Quaternion.Euler(0f, 0f, -moveInput.x * maxTilt);
            if (instantTilt)
                transform.rotation = targetRotFallback;
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotFallback, tiltSpeed * Time.deltaTime);
        }
    }
    // FixedUpdate para movimiento físico estable
    void FixedUpdate()
    {
        // Use the latest sampled input (Update or InputSystem callbacks)
        Vector2 inputToUse = moveInput;

        // Calculamos movimiento en 2D (input) y lo escalamos por velocidad/tiempo físico
        Vector2 movement2D = inputToUse * moveSpeed * Time.fixedDeltaTime;

        // ----- Caso Rigidbody2D -----
        if (rb2d != null)
        {
            // Calcular ángulo objetivo una sola vez para evitar declaraciones duplicadas
            float targetAngle2D = -inputToUse.x * maxTilt;
            float appliedAngle2D = 0f;

            if (useVelocityMovement)
            {
                Vector2 desiredVel = inputToUse * moveSpeed;

                // Si el Rigidbody2D es dinámico, podemos asignar velocity. Si es kinematic, usar MovePosition.
                if (rb2d.bodyType == RigidbodyType2D.Dynamic)
                {
                    rb2d.linearVelocity = desiredVel;

                    if (boundary != null)
                    {
                        Vector2 projected = rb2d.position + desiredVel * Time.fixedDeltaTime;
                        projected.x = Mathf.Clamp(projected.x, boundary.xMin, boundary.xMax);
                        projected.y = Mathf.Clamp(projected.y, boundary.zMin, boundary.zMax);
                        // Si proyectado fue clamped, forzamos la posición y anulamos velocidad
                        if (projected.x != rb2d.position.x || projected.y != rb2d.position.y)
                        {
                            rb2d.position = projected;
                            rb2d.linearVelocity = Vector2.zero;
                        }
                    }
                }
                else
                {
                    // Kinematic body: aplicar por MovePosition para respetar su modo
                    Vector2 newPosK = rb2d.position + movement2D;
                    if (boundary != null)
                    {
                        float clampedX = Mathf.Clamp(newPosK.x, boundary.xMin, boundary.xMax);
                        float clampedY = Mathf.Clamp(newPosK.y, boundary.zMin, boundary.zMax);
                        newPosK = new Vector2(clampedX, clampedY);
                    }
                    rb2d.MovePosition(newPosK);
                }

                if (instantTilt)
                    rb2d.MoveRotation(targetAngle2D);
                else
                {
                    appliedAngle2D = Mathf.LerpAngle(rb2d.rotation, targetAngle2D, tiltSpeed * Time.fixedDeltaTime);
                    rb2d.MoveRotation(appliedAngle2D);
                }

                return;
            }

            Vector2 newPos = rb2d.position + movement2D;

            // Si se definieron límites, aplicarlos (mapeamos zMin/zMax a y en 2D si se desea)
            if (boundary != null)
            {
                float clampedX = Mathf.Clamp(newPos.x, boundary.xMin, boundary.xMax);
                // Para 2D asumimos que zMin/zMax corresponden al eje Y en pantalla (ajusta si usas otra convención)
                float clampedY = Mathf.Clamp(newPos.y, boundary.zMin, boundary.zMax);
                newPos = new Vector2(clampedX, clampedY);
            }

            rb2d.MovePosition(newPos);
            // Rotación/tilt en 2D (rotación alrededor del eje Z)
            appliedAngle2D = Mathf.LerpAngle(rb2d.rotation, targetAngle2D, tiltSpeed * Time.fixedDeltaTime);
            rb2d.MoveRotation(appliedAngle2D);
            return;
        }

        // ----- Caso Rigidbody 3D -----
        if (rb3d != null)
        {
            // Calcular rotación objetivo una sola vez para evitar declaraciones duplicadas
            Quaternion targetRot3D = Quaternion.Euler(transform.eulerAngles.x, 0f, -inputToUse.x * maxTilt);
            Quaternion slerpedRot3D = Quaternion.identity;

            if (useVelocityMovement)
            {
                Vector3 desiredVel = new Vector3(inputToUse.x * moveSpeed, rb3d.linearVelocity.y, inputToUse.y * moveSpeed);

                if (!rb3d.isKinematic)
                {
                    rb3d.linearVelocity = desiredVel;

                    if (boundary != null)
                    {
                        Vector3 projected = rb3d.position + new Vector3(desiredVel.x, 0f, desiredVel.z) * Time.fixedDeltaTime;
                        projected.x = Mathf.Clamp(projected.x, boundary.xMin, boundary.xMax);
                        projected.z = Mathf.Clamp(projected.z, boundary.zMin, boundary.zMax);
                        if (projected.x != rb3d.position.x || projected.z != rb3d.position.z)
                        {
                            rb3d.position = projected;
                            rb3d.linearVelocity = Vector3.zero;
                        }
                    }
                }
                else
                {
                    // Kinematic: usar MovePosition
                    Vector3 movement3DK = new Vector3(movement2D.x, 0f, movement2D.y);
                    Vector3 targetK = rb3d.position + movement3DK;
                    if (boundary != null)
                    {
                        targetK.x = Mathf.Clamp(targetK.x, boundary.xMin, boundary.xMax);
                        targetK.z = Mathf.Clamp(targetK.z, boundary.zMin, boundary.zMax);
                    }
                    rb3d.MovePosition(targetK);
                }

                if (instantTilt)
                    rb3d.MoveRotation(targetRot3D);
                else
                {
                    slerpedRot3D = Quaternion.Slerp(rb3d.rotation, targetRot3D, tiltSpeed * Time.fixedDeltaTime);
                    rb3d.MoveRotation(slerpedRot3D);
                }

                return;
            }

            Vector3 movement3D = new Vector3(movement2D.x, 0f, movement2D.y);
            Vector3 target = rb3d.position + movement3D;

            // Aplicar límites en X/Z si se definieron
            if (boundary != null)
            {
                target.x = Mathf.Clamp(target.x, boundary.xMin, boundary.xMax);
                target.z = Mathf.Clamp(target.z, boundary.zMin, boundary.zMax);
            }

            rb3d.MovePosition(target);
            // Rotación/tilt en 3D (roll alrededor del eje Z)
            slerpedRot3D = Quaternion.Slerp(rb3d.rotation, targetRot3D, tiltSpeed * Time.fixedDeltaTime);
            rb3d.MoveRotation(slerpedRot3D);
            return;
        }

        // ----- Fallback: Transform -----
    Vector3 fallbackMove = new Vector3(movement2D.x, 0f, movement2D.y);
        transform.Translate(fallbackMove, Space.Self);

        // Limitar la posición del Transform si hay límites definidos
        if (boundary != null)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, boundary.xMin, boundary.xMax);
            p.z = Mathf.Clamp(p.z, boundary.zMin, boundary.zMax);
            transform.position = p;
        }

        // Rotación/tilt para fallback Transform
        Quaternion targetRotFallback = Quaternion.Euler(0f, 0f, -inputToUse.x * maxTilt);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotFallback, tiltSpeed * Time.fixedDeltaTime);
    }

    // Dibujar un gizmo en el editor para visualizar los límites (cuando el objeto está seleccionado)
    void OnDrawGizmosSelected()
    {
        if (boundary == null)
            return;

        // Color y transparencia
        Gizmos.color = new Color(0f, 0.75f, 0.9f, 0.9f);

        // Centro y tamaño del rectángulo en el plano XZ
        Vector3 center = new Vector3((boundary.xMin + boundary.xMax) * 0.5f, transform.position.y, (boundary.zMin + boundary.zMax) * 0.5f);
        Vector3 size = new Vector3(Mathf.Abs(boundary.xMax - boundary.xMin), 0.1f, Mathf.Abs(boundary.zMax - boundary.zMin));

        Gizmos.DrawWireCube(center, size);
    }
}
