using System;
using UnityEngine;

// Tek iş: saldırı cooldown'u, saldırı sonrası duraklama ve hasar verme.
// Menzil kontrolü burada yok - "saldırmaya hazır mıyım" (IsReady/IsPaused) ve
// "şimdi saldır" (hasar + event) sorularına cevap verir; menzile ne zaman saldırılacağına
// davranış sınıfları (AlphaWolfBehavior/BetaWolfBehavior) karar verir.
[RequireComponent(typeof(WolfIdentity))]
public class WolfAttackController : MonoBehaviour, IWolfAttacker
{
    [Header("Attack")]
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackDamage = 20f; // Saldırı hasarı
    [SerializeField] private float attackCooldown = 2f;
    [Tooltip("Saldırı sonrası hareketin duracağı süre")]
    [SerializeField] private float attackPauseDuration = 0.5f;
    [SerializeField] private BodyParts damagePart;

    private WolfIdentity identity;
    private float lastAttackTime = float.NegativeInfinity;
    private float pauseTimer;

    public float AttackDistance => attackDistance;
    public bool IsPaused => pauseTimer > 0f;
    public bool IsReady => !IsPaused && Time.time >= lastAttackTime + attackCooldown;
    public event Action OnAttack;

    private void Awake()
    {
        identity = GetComponent<WolfIdentity>();
    }

    private void Update()
    {
        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
        }
    }

    public void Attack()
    {
        lastAttackTime = Time.time;
        pauseTimer = attackPauseDuration;
        OnAttack?.Invoke();

        if (identity.CachedPlayerManager != null)
        {
            identity.CachedPlayerManager.mainCharacter.DamagePart(damagePart, attackDamage);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
