using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseDistance = 5f;

    [Header("Target")]
    [SerializeField] private Transform playerTransform;

    [Header("Attack Settings")]
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float attackCooldown = 2f;

    private Rigidbody rb;
    private float lastAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= chaseDistance)
        {
            ChasePlayer();
        }

        if (distanceToPlayer <= attackDistance && Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    private void ChasePlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);
        transform.LookAt(new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z));
    }


    private void AttackPlayer()
    {
        lastAttackTime = Time.time;
        Debug.Log("Attacking player!");
        // Add actual attack logic here (e.g., damage player, play animation, etc.)
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize chase and attack distances in the Unity Editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
