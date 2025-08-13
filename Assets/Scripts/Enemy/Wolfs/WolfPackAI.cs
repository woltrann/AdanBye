using UnityEngine;
using System.Collections.Generic;

public class WolfPackAI : MonoBehaviour
{
    public bool isAlpha = false; // Lider mi?
    public Transform playerTransform;
    public WolfPackAI alphaWolf; // Beta'lar için lider referansý
    public List<WolfPackAI> packMembers; // Alpha'nýn ekibi

    [Header("Movement")]
    public float speed = 3f;
    public float chaseDistance = 8f;
    public float attackDistance = 1.5f;
    public float attackCooldown = 2f;
    public float formationRadius = 3f; // kovalamada oyuncunun etrafýnda durma mesafesi

    [Header("Follow Alpha Settings")]
    public float minFollowDistance = 2f;
    public float maxFollowDistance = 5f;

    [Header("Retreat")]
    public float retreatDuration = 3f;
    private float retreatTimer;

    private Rigidbody rb;
    private float lastAttackTime;
    public enum State { Idle, Chase, Search, Retreat }
    public State currentState = State.Idle;

    private Vector3 lastKnownPlayerPos;

    // Wander deðiþkenleri
    private Vector3 wanderTarget;
    private float wanderTimer;

    // Chase optimization
    private Vector3 currentTarget;
    private Vector3 lastPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }

    void Update()
    {

        if (isAlpha)
        {
            AlphaBehavior();
        }
        else
        {
            BetaBehavior();
        }
    }



    // ------------------- ALPHA -------------------
    void AlphaBehavior()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        switch (currentState)
        {
            case State.Idle:
                WanderAround(transform.position, 6f, speed * 0.5f);
                if (distance <= chaseDistance) ChangeState(State.Chase);
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
                // Wander around alpha wolf
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
        Debug.Log($"{name} attacks player!");
    }

    void WanderAround(Vector3 center, float radius, float moveSpeed)
    {
        // Timer to control when to pick a new target
        wanderTimer -= Time.deltaTime;

        // If the timer runs out, pick a new random target within the radius
        if (wanderTimer <= 0)
        {
            // Generate a random point within a circle
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            wanderTarget = center + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Reset the timer with a random interval
            wanderTimer = Random.Range(3f, 6f);
        }

        // Smoothly move toward the wander target
        Vector3 direction = (wanderTarget - transform.position).normalized;
        Vector3 newPosition = Vector3.MoveTowards(transform.position, wanderTarget, moveSpeed * Time.deltaTime);

        // Update position and rotation
        rb.MovePosition(newPosition);
        if (direction.magnitude > 0.1f)
        {
            LookAt(wanderTarget);
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

        // Draw current target
        if (currentState == State.Chase)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget);
            Gizmos.DrawSphere(currentTarget, 0.2f);
        }
    }
}