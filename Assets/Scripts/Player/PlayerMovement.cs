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

    [Header("Water Settings")]
    [SerializeField] private float waterCheckDistance = 0.2f;
    [SerializeField] private LayerMask waterLayer;

    [Header("Swim Rise (collider-based)")]
    [Tooltip("İleri yüzerken karakterin ne kadar 'yükseleceği' (mesh için) - collider dünya pozisyonu sabit kalır")]
    [SerializeField] private float maxSwimRise = 0.6f;
    [SerializeField] private float swimRiseSpeed = 3f; // yavaşça geçiş için lerp hızı
    [SerializeField] private float colliderCenterUpdateThreshold = 0.001f; // gereksiz collider rebuild'lerini önlemek için

    [SerializeField] private CapsuleCollider capsuleCollider;
    private Vector3 baseColliderCenter;  // orijinal (kara) center değeri
    private float currentSwimRise;       // şu anki uygulanan rise miktarı
    private float previousSwimRise;      // bir önceki frame'de uygulanmış rise (delta almak için)
    private float lastAppliedColliderRise; // collider.center'a en son yazılan rise değeri

    private bool wasInWater;


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
    private bool isInWater;

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

        if (capsuleCollider == null)
        {
            Debug.LogWarning("CapsuleCollider is not assigned in PlayerMovement. Please assign it in the inspector.");
            return;
        }

        baseColliderCenter = capsuleCollider.center;
        lastAppliedColliderRise = 0f;
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

        if (!isGrounded)
        {
            isInWater = IsInWater();
        }
        else
        {
            isInWater = false;
        }

        if (isInWater != wasInWater)
        {
            wasInWater = isInWater;
        }

        if (animator)
        {
            bool isRunning = runInput && moveInput.sqrMagnitude > 0.01f;
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsInWater", isInWater);

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
        if (isInWater)
        {
            UpdateSwimRise(); // sadece currentSwimRise ve collider.center'ı güncelle, pozisyona dokunma

        }
        HandleMovement(); // yatay hareket + rise deltasını tek MovePosition'da uygula
    }

    private void UpdateSwimRise()
    {
        float speedRatio = Mathf.Clamp01(currentMoveVelocity.magnitude / moveSpeed);
        float targetRise = isInWater ? maxSwimRise * speedRatio : 0f;

        currentSwimRise = Mathf.Lerp(currentSwimRise, targetRise, Time.fixedDeltaTime * swimRiseSpeed);

        if (Mathf.Abs(currentSwimRise - lastAppliedColliderRise) > colliderCenterUpdateThreshold)
        {
            capsuleCollider.center = baseColliderCenter - new Vector3(0f, currentSwimRise, 0f);
            lastAppliedColliderRise = currentSwimRise;
        }
    }

    private void HandleMovement()
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

        Vector3 targetVelocity = moveDir.normalized * targetSpeed;
        currentMoveVelocity = Vector3.SmoothDamp(currentMoveVelocity, targetVelocity, ref velocitySmoothRef, moveSmoothTime);

        Vector3 newPos = rb.position + currentMoveVelocity * Time.fixedDeltaTime;

        // rb.position.y zaten önceki frame'in rise'ını içeriyor.
        // Katlanarak büyümesini önlemek için sadece DELTA'yı ekliyoruz.
        //newPos.y += currentSwimRise - previousSwimRise;
        //previousSwimRise = currentSwimRise;

        rb.MovePosition(newPos);
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

    private bool IsInWater()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, waterCheckDistance + currentSwimRise, waterLayer);
    }

    private bool CheckGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + currentSwimRise, groundLayer);
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