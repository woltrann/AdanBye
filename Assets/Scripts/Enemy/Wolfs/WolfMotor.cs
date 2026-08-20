using UnityEngine;

// Tek iş: Rigidbody üzerinden hareket/bakış primitifleri ve FixedUpdate tabanlı gerçek
// hız ölçümü. rb.MovePosition() ile yapılan hareket transform.position'a ancak fizik
// adımında yansır; bu yüzden hız ölçümünü Update() yerine burada, Time.fixedDeltaTime
// ile yapıyoruz - Update() içinde ölçmek çoğu karede 0 delta görmenize yol açar.
//
// Hareket ve dönüş SmoothDamp/SmoothDampAngle ile yumuşatılıyor (PlayerMotor'daki
// moveSmoothTime/rotationSmoothTime deseniyle aynı) - hedef nokta karede bir değişse
// bile (örn. beta'nın takip ettiği alpha yön değiştirdiğinde) kurt anında zıplayıp
// dönmek yerine yumuşakça yönelir; bu, hedefin sık değiştiği durumlarda görülen
// yerinde titreme (jitter) hissini ortadan kaldırır.
[RequireComponent(typeof(Rigidbody))]
public class WolfMotor : MonoBehaviour, IWolfMover
{
    [Header("Smoothing")]
    [SerializeField] private float moveSmoothTime = 0.15f;
    [SerializeField] private float rotationSmoothTime = 0.15f;

    private Rigidbody rb;
    private Vector3 lastFixedPosition;

    private Vector3 smoothedVelocity;
    private Vector3 velocitySmoothRef;
    private float turnSmoothVelocity;

    public float CurrentSpeed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastFixedPosition = transform.position;
    }

    private void FixedUpdate()
    {
        CurrentSpeed = (transform.position - lastFixedPosition).magnitude / Time.fixedDeltaTime;
        lastFixedPosition = transform.position;
    }

    public void MoveTo(Vector3 target, float moveSpeed)
    {
        Vector3 dir = (target - transform.position).normalized;
        ApplySmoothedMovement(dir * moveSpeed);
    }

    public void MoveAwayFrom(Vector3 target, float moveSpeed)
    {
        Vector3 dir = (transform.position - target).normalized;
        ApplySmoothedMovement(dir * moveSpeed);
    }

    private void ApplySmoothedMovement(Vector3 targetVelocity)
    {
        smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity, targetVelocity, ref velocitySmoothRef, moveSmoothTime);
        rb.MovePosition(transform.position + smoothedVelocity * Time.deltaTime);
    }

    public void LookAt(Vector3 target)
    {
        Vector3 lookPos = new Vector3(target.x, transform.position.y, target.z);
        Vector3 direction = lookPos - transform.position;
        if (direction.sqrMagnitude < 0.0001f) return; // hedef tam üzerimizde, dönecek bir şey yok

        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }
}
