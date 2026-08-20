using System;
using UnityEngine;

// Tek iş: attackDistance + cooldown kontrolü ve OnAttack event'ini fırlatmak.
// Gerçek hasar/animasyon mantığı bu event'e abone olunarak eklenir (OCP) -
// bu sınıfın kendisini değiştirmeye gerek kalmaz.
[RequireComponent(typeof(EnemyTargetSensor))]
public class EnemyAttackController : MonoBehaviour, IEnemyAttacker
{
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackCooldown = 2f;

    private IDistanceProvider distanceProvider;
    private float lastAttackTime = float.NegativeInfinity;

    public bool IsAttackReady => Time.time >= lastAttackTime + attackCooldown;
    public event Action OnAttack;

    private void Awake()
    {
        distanceProvider = GetComponent<IDistanceProvider>();
    }

    private void Update()
    {
        if (distanceProvider.DistanceToTarget > attackDistance) return;
        if (!IsAttackReady) return;

        Attack();
    }

    public void Attack()
    {
        lastAttackTime = Time.time;
        Debug.Log("Attacking player!");
        OnAttack?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
