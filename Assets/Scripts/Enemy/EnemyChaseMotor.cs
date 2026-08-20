using UnityEngine;

// Tek iş: chaseDistance içindeyken hedefe doğru Rigidbody ile yürümek ve ona bakmak.
// Menzil/saldırı kararı vermez - sadece IDistanceProvider'ı okuyup hareket eder.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyTargetSensor))]
public class EnemyChaseMotor : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float chaseDistance = 5f;

    private Rigidbody rb;
    private ITargetProvider targetProvider;
    private IDistanceProvider distanceProvider;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        targetProvider = GetComponent<ITargetProvider>();
        distanceProvider = GetComponent<IDistanceProvider>();
    }

    private void Update()
    {
        if (!targetProvider.HasTarget) return;
        if (distanceProvider.DistanceToTarget > chaseDistance) return;

        Vector3 targetPos = targetProvider.Target.position;
        Vector3 direction = (targetPos - transform.position).normalized;
        rb.MovePosition(transform.position + direction * speed * Time.deltaTime);
        transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
    }
}
