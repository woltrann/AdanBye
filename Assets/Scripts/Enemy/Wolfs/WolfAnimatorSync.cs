using UnityEngine;

// Tek iş: mover'ın hızını Animator'a yazmak, attacker/howler event'lerine abone olup
// Attack/Howl trigger'larını tetiklemek. Hareket/saldırı/uluma kararlarının hiçbiri
// burada yok - sadece "durumu görselleştirme" sorumluluğu (PlayerAnimatorSync ile aynı desen).
[RequireComponent(typeof(WolfMotor))]
public class WolfAnimatorSync : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("Speed parametresine geçişi yumuşatır")]
    [SerializeField] private float animSpeedSmoothing = 8f;

    private IWolfMover mover;
    private IWolfAttacker attacker; // opsiyonel: saldırı sırasında hız 0'a insin, Attack trigger'ı tetiklensin diye
    private IWolfHowler howler;     // opsiyonel: Howl trigger'ı tetiklensin diye
    private float currentAnimSpeed;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HowlHash = Animator.StringToHash("Howl");

    private void Awake()
    {
        mover = GetComponent<IWolfMover>();
        attacker = GetComponent<IWolfAttacker>();
        howler = GetComponent<IWolfHowler>();

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (attacker != null) attacker.OnAttack += HandleAttack;
        if (howler != null) howler.OnHowl += HandleHowl;
    }

    private void OnDisable()
    {
        if (attacker != null) attacker.OnAttack -= HandleAttack;
        if (howler != null) howler.OnHowl -= HandleHowl;
    }

    private void Update()
    {
        if (animator == null) return;

        float targetSpeed = (attacker != null && attacker.IsPaused) ? 0f : mover.CurrentSpeed;
        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * animSpeedSmoothing);
        animator.SetFloat(SpeedHash, currentAnimSpeed);
    }

    private void HandleAttack()
    {
        if (animator) animator.SetTrigger(AttackHash);
    }

    private void HandleHowl()
    {
        if (animator) animator.SetTrigger(HowlHash);
    }
}
