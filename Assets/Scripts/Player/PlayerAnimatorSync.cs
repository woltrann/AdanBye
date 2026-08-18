using UnityEngine;

// Tek iş: diğer bileşenlerin durumunu okuyup animator parametrelerine yazmak.
// Hareket/su/zıplama mantığının hiçbiri burada yok - sadece "durumu görselleştirme" sorumluluğu.
[DefaultExecutionOrder(100)] // en son çalışsın ki o frame'in kesinleşmiş durumunu okusun
public class PlayerAnimatorSync : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMotor motor;

    private IGroundedProvider groundedProvider;
    private IWaterProvider waterProvider;
    private PlayerJumpController jumpController; // opsiyonel: Jump tetiklemesi için

    private void Awake()
    {
        groundedProvider = GetComponent<IGroundedProvider>();
        waterProvider = GetComponent<IWaterProvider>();
        jumpController = GetComponent<PlayerJumpController>();
        if (motor == null) motor = GetComponent<PlayerMotor>();
    }

    private void OnEnable()
    {
        if (jumpController != null) jumpController.OnJumped += HandleJumped;
    }

    private void OnDisable()
    {
        if (jumpController != null) jumpController.OnJumped -= HandleJumped;
    }

    private void HandleJumped()
    {
        if (animator) animator.SetTrigger("Jump");
    }

    private void LateUpdate()
    {
        if (animator == null || motor == null) return;

        animator.SetBool("IsGrounded", groundedProvider != null && groundedProvider.IsGrounded);
        animator.SetBool("IsInWater", waterProvider != null && waterProvider.IsInWater);
        animator.SetBool("IsRunning", motor.IsRunning);

        Vector3 localVelocity = cameraTransform.InverseTransformDirection(motor.CurrentVelocity);
        animator.SetFloat("x", Mathf.Clamp(localVelocity.x / motor.MoveSpeed, -1f, 1f));
        animator.SetFloat("y", Mathf.Clamp(localVelocity.z / motor.MoveSpeed, -1f, 1f));
    }
}
