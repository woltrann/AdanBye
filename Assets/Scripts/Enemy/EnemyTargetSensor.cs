using UnityEngine;

// Tek iş: hedefi (oyuncuyu) tutmak ve ona olan mesafeyi ölçmek.
// Hareket, saldırı gibi hiçbir davranış bilgisi burada yok.
public class EnemyTargetSensor : MonoBehaviour, ITargetProvider, IDistanceProvider
{
    [SerializeField] private Transform target;

    public Transform Target => target;
    public bool HasTarget => target != null;
    public float DistanceToTarget { get; private set; }

    private void Update()
    {
        DistanceToTarget = HasTarget
            ? Vector3.Distance(transform.position, target.position)
            : float.PositiveInfinity;
    }
}
