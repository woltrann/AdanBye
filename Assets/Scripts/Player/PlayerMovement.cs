using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance;
    public MainCharacter mainCharacter;
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement Settings")]
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] public float runSpeed = 10f;
    [SerializeField] private float rotationSmoothTime = 0.1f;
    [SerializeField] private float moveSmoothTime = 0.1f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    

    private Rigidbody rb;

    private Vector3 currentMoveVelocity;
    private Vector3 velocitySmoothRef;

    private PlayerInput input;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction runAction;

    private Vector2 moveInput;
    private bool runInput;
    private float turnSmoothVelocity;
    private bool isGrounded;

    public bool isOutSide = true;
    private Coroutine poisonRoutine;


    private void Awake()
    {
        Instance = this;
        var input = GetComponent<PlayerManager>().InputActions;
        moveAction = input.FindAction("PlayerController/Move");
        jumpAction = input.FindAction("PlayerController/Jump");
        runAction = input.FindAction("PlayerController/Run");

        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0); // daha dengeli zıplama
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        jumpAction.performed += OnJump;
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        jumpAction.performed -= OnJump;
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        runInput = runAction.ReadValue<float>() > 0.5f;
        isGrounded = CheckGrounded();

        if (animator)
        {
            // Koşma durumunu belirle
            bool isRunning = runInput && moveInput.sqrMagnitude > 0.01f;
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsGrounded", isGrounded);

            Vector3 localVelocity = cameraTransform.InverseTransformDirection(currentMoveVelocity);

            float animX = Mathf.Clamp(localVelocity.x / moveSpeed, -1f, 1f);
            float animY = Mathf.Clamp(localVelocity.z / moveSpeed, -1f, 1f);

            animator.SetFloat("x", animX);
            animator.SetFloat("y", animY);
        }

        // Dışarı çıkınca coroutine başlasın
        if (isOutSide && poisonRoutine == null)
        {
            poisonRoutine = StartCoroutine(PoisonOverTime());
        }

        // İçeri girince coroutine dursun
        if (!isOutSide && poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }


    private void HandleMovement()
    {
        // Kamera yönlerine göre hareket yönünü hesapla
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        // Input'a göre ham hareket yönü
        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        // Hedef hızı belirle (Input yoksa 0 olacak)
        float targetSpeed = 0f;

        // Eğer oyuncu bir tuşa basıyorsa rotasyonu ve hedef hızı ayarla
        if (moveInput.sqrMagnitude >= 0.01f)
        {
            targetSpeed = runInput ? runSpeed : moveSpeed;

            // Rotasyon (Sadece hareket etmeye çalışırken dönmeli)
            float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            rb.MoveRotation(Quaternion.Euler(0f, angle, 0f));
        }

        // Karakterin o an ulaşmak istediği nihai vektör
        Vector3 targetVelocity = moveDir.normalized * targetSpeed;

        // Sihrin gerçekleştiği yer: Mevcut hızı, hedef hıza doğru yumuşakça (ivmeli) geçir
        currentMoveVelocity = Vector3.SmoothDamp(currentMoveVelocity, targetVelocity, ref velocitySmoothRef, moveSmoothTime);

        // Rigidbody'yi hesaplanan bu yumuşak hız ile hareket ettir
        rb.MovePosition(rb.position + currentMoveVelocity * Time.fixedDeltaTime);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!isGrounded) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        if (animator)
        {
            animator.SetTrigger("Jump");
        }
    }

    private bool CheckGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }
    private IEnumerator PoisonOverTime()
    {
        while (true)
        {
            if (UXobjects.Instance.gassFilter <= 0)
            {
                mainCharacter.IncreasePoison(1); // senin zehir artırma fonksiyonun
            }
            yield return new WaitForSeconds(2f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.down * groundCheckDistance);
    }
#endif
}
