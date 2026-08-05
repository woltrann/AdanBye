using UnityEngine;
using System.Collections.Generic;

public class WolfPackAI : MonoBehaviour
{
    public bool isAlpha = false; // Lider mi?
    public Transform playerTransform;
    public WolfPackAI alphaWolf; // Beta'lar için lider referansý
    public List<WolfPackAI> packMembers; // Alpha'nýn ekibi

    [Header("Alpha Attack Settings")]
    public float attackDistance = 1.5f;
    public float attackDamage = 20f; // Saldýrý hasarý
    [Header("Attack Pause")]
    public float attackPauseDuration = 0.5f; // Time to pause after attacking
    public BodyParts damagePart;

    [Header("Movement")]
    public float speed = 3f;
    public float chaseDistance = 8f;
    public float attackCooldown = 2f;
    public float formationRadius = 3f; // kovalamada oyuncunun etrafýnda durma mesafesi

    [Header("Follow Alpha Settings")]
    public float minFollowDistance = 2f;
    public float maxFollowDistance = 5f;

    [Header("Retreat")]
    public float retreatDuration = 3f;
    private float retreatTimer;

    [Header("Howl")]
    public float idleHowlChance = 0.15f;      // Idle'da yeni wander hedefi seçilince howl atma ihtimali
    public float howlCooldown = 8f;
    private float lastHowlTime = -999f;

    [Header("Animation")]
    public float animSpeedSmoothing = 8f;     // Speed parametresine geçiþi yumuþatýr

    private Rigidbody rb;
    private float lastAttackTime;
    public enum State { Idle, Chase, Search, Retreat }
    public State currentState = State.Idle;

    private Vector3 lastKnownPlayerPos;
    private Vector3 homePosition; // Idle wander'ýn etrafýnda döneceði sabit nokta

    // Wander deðiþkenleri (artýk merkeze göre LOCAL offset olarak tutuluyor)
    private Vector3 wanderOffset;
    private float wanderTimer;
    private Vector3 lastWanderCenter;

    // Chase optimization
    private Vector3 currentTarget;
    private Vector3 lastPosition;
    private Vector3 lastFixedPosition; // FixedUpdate tabanlý hýz ölçümü için
    private float measuredSpeed;       // FixedUpdate'te hesaplanan, Update()'in okuduðu ham hýz
    private float attackPauseTimer = 0f;    // Timer to track the pause

    [SerializeField] private Animator animator;
    private PlayerManager cachedPlayerManager;
    private float currentAnimSpeed; // yumuþatýlmýþ hýz, animator'a bunu yolluyoruz

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int HowlHash = Animator.StringToHash("Howl");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        lastPosition = transform.position;
        lastFixedPosition = transform.position;
        homePosition = transform.position;

        if (playerTransform != null)
            cachedPlayerManager = playerTransform.GetComponent<PlayerManager>();

        // Baþlangýçta kýsa bir rastgele bekleme, hemen hedef seçilmesini engeller
        wanderTimer = Random.Range(0f, 2f);
        wanderOffset = Vector3.zero;
    }

    void Update()
    {
        // Handle attack pause
        if (attackPauseTimer > 0)
        {
            attackPauseTimer -= Time.deltaTime;
            UpdateAnimator(0f); // saldýrý duraklamasýnda hýz 0'a insin
            return; // Skip behavior updates while paused
        }

        if (isAlpha)
        {
            AlphaBehavior();
        }
        else
        {
            BetaBehavior();
        }

        UpdateAnimator(measuredSpeed);
    }

    void FixedUpdate()
    {
        // rb.MovePosition() ile yapýlan hareket transform.position'a ancak fizik
        // adýmýnda yansýyor; bu yüzden hýz ölçümünü Update() yerine burada,
        // Time.fixedDeltaTime ile yapýyoruz. Update() içinde ölçmek çoðu karede
        // 0 delta (hareket henüz uygulanmamýþ) görmenize yol açar.
        measuredSpeed = (transform.position - lastFixedPosition).magnitude / Time.fixedDeltaTime;
        lastFixedPosition = transform.position;
    }

    // ------------------- ANIMASYON -------------------

    void UpdateAnimator(float targetSpeed)
    {
        if (animator == null) return;
        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, Time.deltaTime * animSpeedSmoothing);
        animator.SetFloat(SpeedHash, currentAnimSpeed);
    }

    void TryHowl()
    {
        if (animator == null) return;
        if (Time.time < lastHowlTime + howlCooldown) return;
        lastHowlTime = Time.time;
        animator.SetTrigger(HowlHash);
    }

    // ------------------- ALPHA -------------------
    void AlphaBehavior()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case State.Idle:
                WanderAround(homePosition, 6f, speed * 0.5f);
                if (distance <= chaseDistance)
                {
                    TryHowl();
                    ChangeState(State.Chase);
                }
                break;

            case State.Chase:
                // Always face and move toward the player
                currentTarget = playerTransform.position;
                LookAt(currentTarget);

                // If we're not at attack distance or can't attack yet, keep moving
                if (distance > attackDistance || Time.time < lastAttackTime + attackCooldown)
                {
                    MoveTo(currentTarget, speed);
                }

                // Attack if within attack distance and cooldown is ready
                if (distance <= attackDistance && Time.time >= lastAttackTime + attackCooldown)
                {
                    Attack();
                }

                // Transition to Search state if the player moves too far away
                if (distance > chaseDistance * 1.5f)
                {
                    lastKnownPlayerPos = playerTransform.position;
                    ChangeState(State.Search);
                }
                break;

            case State.Search:
                WanderAround(lastKnownPlayerPos, 6f, speed * 0.5f);
                if (distance <= chaseDistance) ChangeState(State.Chase);
                break;

            case State.Retreat:
                retreatTimer -= Time.deltaTime;
                MoveAwayFrom(playerTransform.position, speed * 1.5f);
                if (retreatTimer <= 0) ChangeState(State.Idle);
                break;
        }
    }

    // ------------------- BETA -------------------
    void BetaBehavior()
    {
        if (alphaWolf == null) return;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float distToAlpha = Vector3.Distance(transform.position, alphaWolf.transform.position);

        // Beta alerts alpha if it sees the player
        if (distToPlayer <= chaseDistance && alphaWolf.currentState != State.Chase)
        {
            alphaWolf.ChangeState(State.Chase);
        }

        if (alphaWolf.currentState == State.Retreat)
        {
            MoveAwayFrom(playerTransform.position, speed * 1.5f);
            return;
        }

        if (alphaWolf.currentState == State.Chase)
        {
            // Follow the player in formation
            int index = alphaWolf.packMembers.IndexOf(this);
            float angle = (index + 1) * (180f / (alphaWolf.packMembers.Count + 1));
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.forward * formationRadius);
            Vector3 targetPos = playerTransform.position + offset;

            currentTarget = targetPos;
            float distToTarget = Vector3.Distance(transform.position, targetPos);

            // Always move toward formation position unless very close
            if (distToTarget > 0.5f)
            {
                MoveTo(targetPos, speed * 0.9f);
                LookAt(playerTransform.position);
            }

            // Attack if close enough to player
            if (distToPlayer <= attackDistance && Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            // Follow alpha wolf when not chasing
            if (distToAlpha > maxFollowDistance)
            {
                MoveTo(alphaWolf.transform.position, speed * 0.8f);
            }
            else if (distToAlpha < minFollowDistance)
            {
                MoveAwayFrom(alphaWolf.transform.position, speed * 0.8f);
            }
            else
            {
                // Alpha'nýn etrafýnda dolaþ — merkez (alpha) her karede hareket ediyor,
                // bu yüzden offset-tabanlý WanderAround kullanýyoruz (bkz. WanderAround yorumu)
                WanderAround(alphaWolf.transform.position, maxFollowDistance, speed * 0.4f);
            }
        }
    }

    // ------------------- Ortak Fonksiyonlar -------------------
    Vector3 GetAvoidanceDirection(Vector3 target)
    {
        // Try to go around obstacles by adding a perpendicular offset
        Vector3 toTarget = (target - transform.position).normalized;
        Vector3 right = Vector3.Cross(toTarget, Vector3.up);

        // Randomly choose left or right
        return Random.value > 0.5f ? right : -right;
    }

    void ChangeState(State newState)
    {
        currentState = newState;
        if (newState == State.Retreat)
            retreatTimer = retreatDuration;

        if (isAlpha && newState == State.Retreat)
        {
            foreach (var wolf in packMembers)
                wolf.ChangeState(State.Retreat);
        }
    }

    void LookAt(Vector3 target)
    {
        Vector3 lookPos = new Vector3(target.x, transform.position.y, target.z);
        Vector3 direction = (lookPos - transform.position).normalized;
        if (direction.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void MoveTo(Vector3 target, float moveSpeed)
    {
        Vector3 dir = (target - transform.position).normalized;
        Vector3 newPos = transform.position + dir * moveSpeed * Time.deltaTime;
        rb.MovePosition(newPos);
    }

    void MoveAwayFrom(Vector3 target, float moveSpeed)
    {
        Vector3 dir = (transform.position - target).normalized;
        Vector3 newPos = transform.position + dir * moveSpeed * Time.deltaTime;
        rb.MovePosition(newPos);
    }

    void Attack()
    {
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        // Attack logic: Deal damage to the player if within range
        if (cachedPlayerManager != null)
        {
            cachedPlayerManager.mainCharacter.DamagePart(damagePart, attackDamage);
        }

        // Pause movement after attacking
        attackPauseTimer = attackPauseDuration;
    }

    void WanderAround(Vector3 center, float radius, float moveSpeed)
    {
        wanderTimer -= Time.deltaTime;

        Vector3 target = center + wanderOffset;
        bool reachedTarget = Vector3.Distance(transform.position, target) < 0.5f;

        if (wanderTimer <= 0f || reachedTarget)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            wanderOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            wanderTimer = Random.Range(2f, 5f);

            if (isAlpha && Random.value < idleHowlChance)
                TryHowl();

            target = center + wanderOffset;
        }

        // Zemin yüksekliðini raycast ile bul
        RaycastHit hit;
        float groundY = transform.position.y;
        if (Physics.Raycast(new Vector3(target.x, 500f, target.z), Vector3.down, out hit, 1000f))
        {
            groundY = hit.point.y;
        }
        Vector3 groundedTarget = new Vector3(target.x, groundY, target.z);

        Vector3 flatTarget = new Vector3(groundedTarget.x, transform.position.y, groundedTarget.z);
        float dist = (flatTarget - transform.position).magnitude;

        if (dist > 0.05f)
        {
            Vector3 newPosition = Vector3.MoveTowards(transform.position, groundedTarget, moveSpeed * Time.deltaTime);
            rb.MovePosition(newPosition);
            LookAt(groundedTarget);
        }
    }

    public void StunAndRetreat()
    {
        ChangeState(State.Retreat);
        Debug.Log($"{name} is retreating after stun!");
    }

    // ------------------- GÝZMOS -------------------
    void OnDrawGizmos()
    {
        Gizmos.color = isAlpha ? Color.red : Color.blue;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        if (!isAlpha)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, formationRadius);
        }

        if (currentState == State.Chase)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget);
            Gizmos.DrawSphere(currentTarget, 0.2f);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position + wanderOffset, 0.12f);
    }
}