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

    [Header("Curve-Driven Movement (opsiyonel)")]
    [SerializeField] private bool useCurveDrivenMovement = false;
    [SerializeField] private MovementCurveProfile curveProfile;
    private CurveMovementSolver curveSolver;
    private bool curveProfileWarningLogged;

    private Rigidbody rb;
    private Vector3 lastFixedPosition;

    private Vector3 smoothedVelocity;
    private Vector3 velocitySmoothRef;
    private float turnSmoothVelocity;

    private Vector3 pendingDirection;
    private float pendingSpeed;
    private bool hasPendingMove;

    public float CurrentSpeed { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastFixedPosition = transform.position;

        if (curveProfile != null)
            curveSolver = new CurveMovementSolver(curveProfile);
    }

    // rb.MovePosition() fizik motorunun collision resolve'unu doğru yapabilmesi için
    // FixedUpdate'de, fixedDeltaTime ile çağrılmalı. MoveTo/MoveAwayFrom Update-zinciri
    // üzerinden (behavior.Tick()) çağrıldığı için burada sadece niyeti (yön/hız) kaydediyoruz,
    // gerçek hareketi burada, fizik adımıyla senkron şekilde uyguluyoruz - aksi halde
    // (frame rate fizik adımıyla senkron olmadığında) hızlı hareket eden kurtlar terrain
    // collider'ını "atlayıp" (tunneling) altına düşebiliyordu.
    private void FixedUpdate()
    {
        CurrentSpeed = (transform.position - lastFixedPosition).magnitude / Time.fixedDeltaTime;
        lastFixedPosition = transform.position;

        if (hasPendingMove)
        {
            ApplyMovement(pendingDirection, pendingSpeed);
            hasPendingMove = false;
        }
    }

    public void MoveTo(Vector3 target, float moveSpeed)
    {
        pendingDirection = (target - transform.position).normalized;
        pendingSpeed = moveSpeed;
        hasPendingMove = true;
    }

    public void MoveAwayFrom(Vector3 target, float moveSpeed)
    {
        pendingDirection = (transform.position - target).normalized;
        pendingSpeed = moveSpeed;
        hasPendingMove = true;
    }

    private void ApplyMovement(Vector3 direction, float maxSpeed)
    {
        Vector3 velocity;
        if (useCurveDrivenMovement && curveSolver != null)
        {
            velocity = curveSolver.Evaluate(direction, maxSpeed, hasInput: true, Time.fixedTime);
        }
        else
        {
            if (useCurveDrivenMovement && !curveProfileWarningLogged)
            {
                Debug.LogWarning($"{name}: useCurveDrivenMovement açık ama curveProfile atanmamış, SmoothDamp yoluna düşülüyor.", this);
                curveProfileWarningLogged = true;
            }
            smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity, direction * maxSpeed, ref velocitySmoothRef, moveSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            velocity = smoothedVelocity;
        }
        rb.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
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
