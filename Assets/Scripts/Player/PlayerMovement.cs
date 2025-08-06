using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputActionAsset InputActions;
    public Animator animator;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    private Rigidbody rb;

    private InputAction moveAction;
    private InputAction lookAction;

    private Vector2 moveInput;
    private float turnSmoothVelocity;


    private void Awake()
    {
        InputActions = GetComponent<PlayerManager>().InputActions;

        moveAction = InputActions.FindAction("PlayerController/Move");
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Cinemachine kamerasının forward/right'ı
        Transform cam = Camera.main.transform;

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;

        if (moveDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);

            rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));

            Vector3 move = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(rb.position + move.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }
}
