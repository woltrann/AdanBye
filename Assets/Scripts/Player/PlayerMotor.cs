using UnityEngine;
using UnityEngine.InputSystem;

// Tek iş: input'a göre Rigidbody'yi hareket ettirmek ve döndürmek.
// Su, zıplama, animasyon derdi yok - sadece "nereye, ne hızla" sorusuna cevap verir.
[DefaultExecutionOrder(0)] // sensor(-100) ve swim(-50)'den SONRA, animator(100)'dan ÖNCE
[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour, IVelocityProvider
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float moveSmoothTime = 0.1f;

    [Header("Curve-Driven Movement (opsiyonel)")]
    [SerializeField] private bool useCurveDrivenMovement = false;
    [SerializeField] private MovementCurveProfile curveProfile;
    private CurveMovementSolver curveSolver;
    private bool curveProfileWarningLogged;

    private Rigidbody rb;

    // Opsiyonel: swim rise gibi dış kaynaklı bir yükseklik değişimi varsa onu da uygularız.
    private IElevationOffsetProvider elevationProvider;

    private InputAction moveAction;
    private InputAction runAction;

    private Vector2 moveInput;
    private bool runInput;
    private float turnSmoothVelocity;
    private Vector3 velocitySmoothRef;

    public Vector3 CurrentVelocity { get; private set; }
    public float MoveSpeed => moveSpeed;
    public float RunSpeed => runSpeed;
    public bool IsRunning => runInput && moveInput.sqrMagnitude > 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // daha dengeli zıplama

        elevationProvider = GetComponent<IElevationOffsetProvider>();

        var input = GetComponent<PlayerManager>().InputActions;
        moveAction = input.FindAction("PlayerController/Move");
        runAction = input.FindAction("PlayerController/Run");

        if (curveProfile != null)
            curveSolver = new CurveMovementSolver(curveProfile);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        runAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        runAction.Disable();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        runInput = runAction.ReadValue<float>() > 0.5f;
    }

    private void FixedUpdate()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        float targetSpeed = 0f;
        if (moveInput.sqrMagnitude >= 0.01f)
        {
            targetSpeed = runInput ? runSpeed : moveSpeed;
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));
        }

        bool hasInput = moveInput.sqrMagnitude >= 0.01f;
        if (useCurveDrivenMovement && curveSolver != null)
        {
            CurrentVelocity = curveSolver.Evaluate(moveDir, targetSpeed, hasInput, Time.fixedTime);
        }
        else
        {
            if (useCurveDrivenMovement && !curveProfileWarningLogged)
            {
                Debug.LogWarning($"{name}: useCurveDrivenMovement açık ama curveProfile atanmamış, SmoothDamp yoluna düşülüyor.", this);
                curveProfileWarningLogged = true;
            }
            Vector3 targetVelocity = moveDir.normalized * targetSpeed;
            CurrentVelocity = Vector3.SmoothDamp(CurrentVelocity, targetVelocity, ref velocitySmoothRef, moveSmoothTime);
        }

        Vector3 newPos = rb.position + CurrentVelocity * Time.fixedDeltaTime;

        if (elevationProvider != null)
        {
            newPos.y += elevationProvider.ElevationDelta;
        }

        rb.MovePosition(newPos);
    }

    public void ApplyImpulse(Vector3 impulse) => rb.AddForce(impulse, ForceMode.Impulse);

    public void MultiplySpeed(float multiplier)
    {
        moveSpeed *= multiplier;
        runSpeed *= multiplier;
    }
}