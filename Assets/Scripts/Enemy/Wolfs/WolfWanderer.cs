using UnityEngine;

// Tek iş: bir merkez etrafında rastgele, zemine oturan noktalar arasında dolaşmak.
// Idle/Search state'leri ve Beta'nın alpha etrafında dolaşması bu servisi paylaşır -
// merkez, yarıçap ve hız her çağrıda dışarıdan verilir, kendi state'i bilmez.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WolfMotor))]
public class WolfWanderer : MonoBehaviour
{
    private Rigidbody rb;
    private IWolfMover mover;

    private Vector3 wanderOffset;
    private float wanderTimer;

    private Vector3 pendingPosition;
    private bool hasPendingMove;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mover = GetComponent<IWolfMover>();

        // Başlangıçta kısa bir rastgele bekleme, hemen hedef seçilmesini engeller
        wanderTimer = Random.Range(0f, 2f);
    }

    // rb.MovePosition()'ı Tick() (Update-zinciri) içinden değil, fizik adımıyla senkron
    // FixedUpdate'ten uyguluyoruz - aynı gerekçe için bkz. WolfMotor.FixedUpdate().
    private void FixedUpdate()
    {
        if (hasPendingMove)
        {
            rb.MovePosition(pendingPosition);
            hasPendingMove = false;
        }
    }

    // center etrafında dolaşır; bu çağrıda yeni bir hedef seçtiyse true döner
    // (çağıran taraf bunu howl şansı gibi kararlar için kullanabilir).
    public bool Tick(Vector3 center, float radius, float moveSpeed)
    {
        wanderTimer -= Time.deltaTime;

        Vector3 target = center + wanderOffset;
        bool reachedTarget = Vector3.Distance(transform.position, target) < 0.5f;
        bool pickedNewTarget = false;

        if (wanderTimer <= 0f || reachedTarget)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            wanderOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            wanderTimer = Random.Range(2f, 5f);
            pickedNewTarget = true;

            target = center + wanderOffset;
        }

        // Zemin yüksekliğini raycast ile bul
        float groundY = transform.position.y;
        if (Physics.Raycast(new Vector3(target.x, 500f, target.z), Vector3.down, out RaycastHit hit, 1000f))
        {
            groundY = hit.point.y;
        }
        Vector3 groundedTarget = new Vector3(target.x, groundY, target.z);

        Vector3 flatTarget = new Vector3(groundedTarget.x, transform.position.y, groundedTarget.z);
        float dist = (flatTarget - transform.position).magnitude;

        if (dist > 0.05f)
        {
            pendingPosition = Vector3.MoveTowards(transform.position, groundedTarget, moveSpeed * Time.deltaTime);
            hasPendingMove = true;
            mover.LookAt(groundedTarget);
        }

        return pickedNewTarget;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position + wanderOffset, 0.12f);
    }
}
