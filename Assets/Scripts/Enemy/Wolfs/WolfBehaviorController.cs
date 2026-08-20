using UnityEngine;

// İnce orkestratör: hangi component'in ne zaman tetikleneceğine karar vermez, sadece
// isAlpha'ya göre Alpha/Beta stratejisini seçer (Strategy pattern - OCP) ve her karede
// Tick() eder. Ayarlar burada (Inspector'dan) toplanır, davranış sınıflarına salt-okunur
// property'ler üzerinden sunulur.
[RequireComponent(typeof(WolfIdentity))]
[RequireComponent(typeof(WolfMotor))]
[RequireComponent(typeof(WolfWanderer))]
[RequireComponent(typeof(WolfAttackController))]
[RequireComponent(typeof(WolfHowlController))]
public class WolfBehaviorController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;
    [SerializeField] private float chaseDistance = 8f;
    [Tooltip("Kovalamada oyuncunun etrafında durma mesafesi")]
    [SerializeField] private float formationRadius = 3f;

    [Header("Follow Alpha Settings")]
    [Tooltip("Kovalamıyorken beta'nın alpha'nın arkasında durmaya çalışacağı mesafe")]
    [SerializeField] private float followDistance = 4f;

    [Header("Retreat")]
    [SerializeField] private float retreatDuration = 3f;

    [Header("Territory")]
    [Tooltip("Sürünün terk etmemesi gereken alan (WolfPack altındaki Territory child'ı). Atanmazsa sınır kontrolü yapılmaz.")]
    [SerializeField] private WolfTerritory territory;

    [Header("Howl")]
    [Tooltip("Idle'da yeni wander hedefi seçilince howl atma ihtimali")]
    [SerializeField] private float idleHowlChance = 0.15f;

    private WolfIdentity identity;
    private IWolfAttacker attacker;
    private IWolfBehavior behavior;

    public WolfState CurrentState { get; private set; } = WolfState.Idle;
    public float Speed => speed;
    public float ChaseDistance => chaseDistance;
    public float FormationRadius => formationRadius;
    public float FollowDistance => followDistance;
    public float RetreatDuration => retreatDuration;
    public float IdleHowlChance => idleHowlChance;
    public WolfTerritory Territory => territory;

    private void Awake()
    {
        identity = GetComponent<WolfIdentity>();
        attacker = GetComponent<IWolfAttacker>();

        var mover = GetComponent<IWolfMover>();
        var howler = GetComponent<IWolfHowler>();
        var wanderer = GetComponent<WolfWanderer>();

        behavior = identity.IsAlpha
            ? new AlphaWolfBehavior(this, transform, identity, mover, attacker, howler, wanderer, territory)
            : new BetaWolfBehavior(this, transform, identity, mover, attacker);
    }

    private void Update()
    {
        // Saldırı sonrası duraklama sırasında hareket/state güncellemesi durur
        // (hız 0'a inmesi WolfAnimatorSync'in kendi işi).
        if (attacker != null && attacker.IsPaused) return;

        behavior.Tick();
    }

    public void ChangeState(WolfState newState)
    {
        CurrentState = newState;
        behavior.OnStateEntered(newState);

        if (identity.IsAlpha && newState == WolfState.Retreat)
        {
            foreach (var member in identity.PackMembers)
            {
                member.GetComponent<WolfBehaviorController>()?.ChangeState(WolfState.Retreat);
            }
        }
    }

    public void StunAndRetreat()
    {
        ChangeState(WolfState.Retreat);
        Debug.Log($"{name} is retreating after stun!");
    }

    private void OnDrawGizmos()
    {
        var id = GetComponent<WolfIdentity>();
        bool isAlpha = id != null && id.IsAlpha;

        Gizmos.color = isAlpha ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        if (!isAlpha)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, formationRadius);
        }

        if (CurrentState == WolfState.Chase && behavior != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, behavior.DebugTarget);
            Gizmos.DrawSphere(behavior.DebugTarget, 0.2f);
        }
    }
}
