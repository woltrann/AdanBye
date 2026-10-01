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
    private IStaminaReadout stamina; // opsiyonel: çökme animasyonu için

    private static readonly int IsCollapsedHash = Animator.StringToHash("IsCollapsed");
    // Parametre yoksa Unity her SetBool'da uyarı basar; bu yüzden varlığı bir kez kontrol edilir.
    private bool canWriteIsCollapsed;

    private void Awake()
    {
        groundedProvider = GetComponent<IGroundedProvider>();
        waterProvider = GetComponent<IWaterProvider>();
        jumpController = GetComponent<PlayerJumpController>();
        if (motor == null) motor = GetComponent<PlayerMotor>();
        stamina = GetComponent<IStaminaReadout>();
        canWriteIsCollapsed = stamina != null && HasParameter(IsCollapsedHash, AnimatorControllerParameterType.Bool);
        if (stamina != null && !canWriteIsCollapsed)
            Debug.LogWarning("[PlayerAnimatorSync] Animator'da 'IsCollapsed' (bool) parametresi yok; çökme animasyonu tetiklenmeyecek.", this);
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
        if (canWriteIsCollapsed) animator.SetBool(IsCollapsedHash, stamina.IsCollapsed);

        Vector3 localVelocity = cameraTransform.InverseTransformDirection(motor.CurrentVelocity);
        animator.SetFloat("x", Mathf.Clamp(localVelocity.x / motor.MoveSpeed, -1f, 1f));
        animator.SetFloat("y", Mathf.Clamp(localVelocity.z / motor.MoveSpeed, -1f, 1f));
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.nameHash == hash && p.type == type) return true;
        return false;
    }
}
