using UnityEngine;

// Tek iş: suda ileri yüzerken karakterin "yükselmiş" görünmesini sağlamak
// (collider'ın dünya pozisyonunu sabit tutarak, sadece mesh'i yükseltmek).
// Hareketi kendisi uygulamaz - sadece ElevationDelta'yı dışa açar, PlayerMotor onu okuyup uygular.
[DefaultExecutionOrder(-50)] // sensor(-100)'dan sonra, motor(0)'dan önce
public class PlayerSwimController : MonoBehaviour, IElevationOffsetProvider
{
    [Header("Swim Rise (collider-based)")]
    [Tooltip("İleri yüzerken karakterin ne kadar 'yükseleceği' (mesh için) - collider dünya pozisyonu sabit kalır")]
    [SerializeField] private float maxSwimRise = 0.6f;
    [SerializeField] private float swimRiseSpeed = 3f;
    [SerializeField] private float colliderCenterUpdateThreshold = 0.001f;

    [SerializeField] private CapsuleCollider capsuleCollider;

    private IWaterProvider waterProvider;
    private IVelocityProvider velocityProvider;

    private Vector3 baseColliderCenter;
    private float currentSwimRise;
    private float previousSwimRise;
    private float lastAppliedColliderRise;

    public float CurrentElevation => currentSwimRise;
    public float ElevationDelta => currentSwimRise - previousSwimRise;

    private void Awake()
    {
        if (capsuleCollider == null)
        {
            Debug.LogWarning($"{nameof(PlayerSwimController)}: CapsuleCollider referansı atanmadı.");
            return;
        }
        baseColliderCenter = capsuleCollider.center;

        waterProvider = GetComponent<IWaterProvider>();
        velocityProvider = GetComponent<IVelocityProvider>();

        if (waterProvider == null)
            Debug.LogWarning($"{nameof(PlayerSwimController)}: IWaterProvider bulunamadı (PlayerGroundWaterSensor eksik mi?).");
        if (velocityProvider == null)
            Debug.LogWarning($"{nameof(PlayerSwimController)}: IVelocityProvider bulunamadı (PlayerMotor eksik mi?).");
    }

    private void FixedUpdate()
    {
        previousSwimRise = currentSwimRise;

        bool isInWater = waterProvider != null && waterProvider.IsInWater;
        float moveSpeed = velocityProvider != null ? velocityProvider.MoveSpeed : 1f;
        float currentSpeed = velocityProvider != null ? velocityProvider.CurrentVelocity.magnitude : 0f;

        float speedRatio = moveSpeed > 0f ? Mathf.Clamp01(currentSpeed / moveSpeed) : 0f;
        float targetRise = isInWater ? maxSwimRise * speedRatio : 0f;

        currentSwimRise = Mathf.Lerp(currentSwimRise, targetRise, Time.fixedDeltaTime * swimRiseSpeed);

        if (Mathf.Abs(currentSwimRise - lastAppliedColliderRise) > colliderCenterUpdateThreshold)
        {
            capsuleCollider.center = baseColliderCenter - new Vector3(0f, currentSwimRise, 0f);
            lastAppliedColliderRise = currentSwimRise;
        }
    }
}
