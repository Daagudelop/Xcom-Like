using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public static CameraMovement instance;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineOrbitalFollow virtualCamera;
    [SerializeField] private GameObject pivot;
    [Tooltip("La Camera real que renderiza (la que mueve/rota Cinemachine). Si se deja vacío, usa Camera.main.")]
    [SerializeField] private Camera mainCamera;

    [Header("Configuración de movimiento")]
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float movementLerpAmount = 10f;
    [SerializeField] private float moveAccel = 6f;
    [SerializeField] private float moveDecel = 6f;

    [Header("Rotación")]
    [Tooltip("Grados por segundo, a máxima aceleración, mientras se mantiene Q o E presionado.")]
    [SerializeField] private float rotationSensitivity = 90f;
    [Tooltip("Qué tan rápido acelera/frena el giro (más alto = responde más rápido).")]
    [SerializeField] private float rotationSmoothing = 8f;
    private float currentAngularSpeed = 0f;

    [Header("Zoom (CinemachineOrbitalFollow.RadialAxis)")]
    [SerializeField] private float zoomSensitivity = 0.15f;
    [SerializeField] private float minZoom = 0.4f;
    [SerializeField] private float maxZoom = 2.5f;

    [Header("Cambio de piso")]
    [SerializeField] private float yPosition = 0.8f;
    [SerializeField] private float yPositionLerpAmount = 5f;
    [SerializeField] private float yPositionIntervals = 3.5f;
    [SerializeField] private float superiorLimitYPos = 20f;
    [SerializeField] private float inferiorLimitYPos = 0f;

    [Header("Otros")]
    [SerializeField] private Animator animator;
    [SerializeField] private int life = 3;

    public int Life { get => life; set => life = value; }
    public Animator Animator { get => animator; set => animator = value; }
    public float YPosition { get => yPosition; set => yPosition = value; }

    // --- Input System: acciones creadas por código, sin necesidad de un asset .inputactions ---
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction zoomAction;
    private InputAction floorUpAction;
    private InputAction floorDownAction;

    private Vector2 moveInput;
    private float rotateInput;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        yPosition = transform.position.y;

        if (mainCamera == null)
            mainCamera = Camera.main;

        BuildInputActions();
    }

    private void BuildInputActions()
    {
        // Movimiento: WASD en teclado + stick izquierdo en mando.
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        // Rotación: Q (izquierda) / E (derecha) como un eje 1D.
        rotateAction = new InputAction("Rotate", InputActionType.Value, expectedControlType: "Axis");
        rotateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        // Zoom: rueda del mouse.
        zoomAction = new InputAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");

        // Cambio de piso: Shift izquierdo (subir) / Ctrl izquierdo (bajar).
        floorUpAction = new InputAction("FloorUp", InputActionType.Button, "<Keyboard>/leftShift");
        floorDownAction = new InputAction("FloorDown", InputActionType.Button, "<Keyboard>/leftCtrl");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        rotateAction.Enable();
        zoomAction.Enable();
        floorUpAction.Enable();
        floorDownAction.Enable();

        floorUpAction.performed += OnFloorUp;
        floorDownAction.performed += OnFloorDown;
    }

    private void OnDisable()
    {
        floorUpAction.performed -= OnFloorUp;
        floorDownAction.performed -= OnFloorDown;

        moveAction.Disable();
        rotateAction.Disable();
        zoomAction.Disable();
        floorUpAction.Disable();
        floorDownAction.Disable();
    }

    private void OnFloorUp(InputAction.CallbackContext ctx)
    {
        yPosition = Mathf.Min(yPosition + yPositionIntervals, superiorLimitYPos);
    }

    private void OnFloorDown(InputAction.CallbackContext ctx)
    {
        yPosition = Mathf.Max(yPosition - yPositionIntervals, inferiorLimitYPos);
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        rotateInput = rotateAction.ReadValue<float>();

        float scroll = zoomAction.ReadValue<float>();
        if (Mathf.Abs(scroll) > 0.01f && virtualCamera != null)
        {
            InputAxis radial = virtualCamera.RadialAxis;
            radial.Value = Mathf.Clamp(radial.Value - scroll * zoomSensitivity, minZoom, maxZoom);
            virtualCamera.RadialAxis = radial;
        }
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        AlignPivotToCamera();
        MoveBetweenFloor(yPositionLerpAmount);
        HorizontalMovement3D(rigidBody, movementLerpAmount, moveInput.x, maxSpeed, moveAccel, moveDecel, pivot.transform);
        VerticalMovement3D(rigidBody, movementLerpAmount, moveInput.y, maxSpeed, moveAccel, moveDecel, pivot.transform);
    }

    /// <summary>
    /// El "frente" para el movimiento pasa a ser, literalmente, el forward de la
    /// cámara real (aplanado sobre el plano XZ) — lo que el jugador ve en pantalla
    /// como "adelante", sin depender de que nuestro cálculo manual coincida con lo
    /// que Cinemachine termina renderizando (que puede tener su propio damping).
    /// </summary>
    private void AlignPivotToCamera()
    {
        if (mainCamera == null || pivot == null)
            return;

        Vector3 flatForward = mainCamera.transform.forward;
        flatForward.y = 0f;

        // Si la cámara mira casi derecho hacia arriba/abajo, flatForward puede
        // quedar casi en cero: en ese caso no tocamos la rotación para evitar NaN.
        if (flatForward.sqrMagnitude < 0.0001f)
            return;

        pivot.transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    public void MoveBetweenFloor(float lerpAmount)
    {
        Vector3 targetYPos = new Vector3(transform.position.x, yPosition, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetYPos, Time.deltaTime * lerpAmount);
    }

    #region Parent & UnParent

    public void MoveTo(GameObject target)
    {
        transform.position = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);
    }

    public void UnParent()
    {
        transform.parent = null;
    }

    #endregion

    #region Rotación Y

    /// <summary>
    /// Suaviza la VELOCIDAD de giro (no el ángulo). Al soltar Q/E, rotateInput pasa
    /// a 0, así que currentAngularSpeed se va frenando suave hacia 0 en vez de seguir
    /// girando hacia un ángulo objetivo que ya no tiene sentido perseguir.
    /// Esto solo mueve el HorizontalAxis de Cinemachine (el input del orbit); quién
    /// termina definiendo el "frente" real es AlignPivotToCamera(), leyendo la
    /// cámara ya renderizada.
    /// </summary>
    private void ApplyRotation()
    {
        float targetSpeed = rotateInput * rotationSensitivity;
        currentAngularSpeed = Mathf.Lerp(currentAngularSpeed, targetSpeed, Time.fixedDeltaTime * rotationSmoothing);

        if (Mathf.Approximately(currentAngularSpeed, 0f))
            return;

        float delta = currentAngularSpeed * Time.fixedDeltaTime;

        // HorizontalAxis es un struct (InputAxis): hay que leerlo, modificarlo y
        // reasignarlo, igual que con RadialAxis en el zoom.
        InputAxis horizontal = virtualCamera.HorizontalAxis;
        horizontal.Value += delta;
        virtualCamera.HorizontalAxis = horizontal;
    }

    #endregion

    #region Movimiento

    public void VerticalMovement3D(Rigidbody rb, float lerpAmount, float axisValue, float maxSpd, float accel, float decel, Transform pivotTransform)
    {
        Vector3 moveDir = pivotTransform.forward;
        float currentSpeed = Vector3.Dot(rb.linearVelocity, moveDir);
        float targetSpeed = Mathf.Lerp(currentSpeed, axisValue * maxSpd, lerpAmount);
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;
        float speedDiff = targetSpeed - currentSpeed;
        float movement = speedDiff * accelRate;

        rb.AddForce(moveDir * movement, ForceMode.Force);
    }

    public void HorizontalMovement3D(Rigidbody rb, float lerpAmount, float axisValue, float maxSpd, float accel, float decel, Transform pivotTransform)
    {
        Vector3 moveDir = pivotTransform.right;
        float currentSpeed = Vector3.Dot(rb.linearVelocity, moveDir);
        float targetSpeed = Mathf.Lerp(currentSpeed, axisValue * maxSpd, lerpAmount);
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? accel : decel;
        float speedDiff = targetSpeed - currentSpeed;
        float movement = speedDiff * accelRate;

        rb.AddForce(moveDir * movement, ForceMode.Force);
    }

    #endregion
}
