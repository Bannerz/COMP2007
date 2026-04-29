using UnityEngine;
using UnityEngine.AI;

public class SimpleEnemyAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Roaming,
        Chasing,
        Attacking
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);

    [Header("Detection")]
    [SerializeField] private float proximityDetectionRadius = 6f;
    [SerializeField] private float lineOfSightRadius = 14f;
    [SerializeField, Range(1f, 360f)] private float fieldOfViewAngle = 110f;
    [SerializeField] private LayerMask lineOfSightBlockers = ~0;
    [SerializeField] private bool rememberTargetAfterDetection = true;
    [SerializeField] private float loseTargetAfterSeconds = 3f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float stoppingDistance = 1.6f;
    [SerializeField] private bool keepMovementFlat = true;

    [Header("Roaming")]
    [SerializeField] private bool roamWhenIdle = true;
    [SerializeField] private float roamRadius = 10f;
    [SerializeField] private float roamSpeed = 1.5f;
    [SerializeField] private float roamStoppingDistance = 0.75f;
    [SerializeField] private float minRoamWaitTime = 1.5f;
    [SerializeField] private float maxRoamWaitTime = 4f;
    [SerializeField] private int roamPointAttempts = 12;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.25f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string movingParameter = "IsMoving";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private float animatorDampTime = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private NavMeshAgent navMeshAgent;
    private CharacterController characterController;
    private IDamageable targetDamageable;
    private EnemyState state;
    private Vector3 roamOrigin;
    private Vector3 roamDestination;
    private float lastTimeTargetSeen;
    private float nextAttackTime;
    private float nextRoamTime;
    private bool hasRoamDestination;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        characterController = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.angularSpeed = turnSpeed;
        }
    }

    private void Start()
    {
        roamOrigin = transform.position;
        FindTargetIfNeeded();
        CacheTargetDamageable();
        ScheduleNextRoam();
    }

    private void Update()
    {
        FindTargetIfNeeded();

        if (target == null)
        {
            RoamOrIdle();
            return;
        }

        CacheTargetDamageable();

        bool canDetectTarget = CanDetectTarget();
        if (canDetectTarget)
        {
            lastTimeTargetSeen = Time.time;
        }

        bool shouldChase = canDetectTarget || (rememberTargetAfterDetection && Time.time - lastTimeTargetSeen <= loseTargetAfterSeconds);
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        if (!shouldChase)
        {
            RoamOrIdle();
            return;
        }

        hasRoamDestination = false;
        FaceTarget();

        if (distanceToTarget <= attackRange)
        {
            StopMoving();
            SetMovementAnimation(false, 0f);
            state = EnemyState.Attacking;
            TryAttack();
            return;
        }

        state = EnemyState.Chasing;
        SetMovementAnimation(true, 1f);
        MoveTowardTarget();
    }

    private void FindTargetIfNeeded()
    {
        if (target != null)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            return;
        }

        target = player.transform;
        CacheTargetDamageable();
    }

    private void CacheTargetDamageable()
    {
        if (target == null || targetDamageable != null)
        {
            return;
        }

        targetDamageable = target.GetComponent<IDamageable>()
            ?? target.GetComponentInParent<IDamageable>()
            ?? target.GetComponentInChildren<IDamageable>();
    }

    private bool CanDetectTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= proximityDetectionRadius)
        {
            return true;
        }

        if (distance > lineOfSightRadius)
        {
            return false;
        }

        Vector3 flatDirection = FlattenDirection(toTarget).normalized;
        if (Vector3.Angle(transform.forward, flatDirection) > fieldOfViewAngle * 0.5f)
        {
            return false;
        }

        Vector3 rayStart = transform.position + eyeOffset;
        Vector3 rayEnd = target.position + targetOffset;
        Vector3 rayDirection = rayEnd - rayStart;

        RaycastHit[] hits = Physics.RaycastAll(rayStart, rayDirection.normalized, rayDirection.magnitude, lineOfSightBlockers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    private void MoveTowardTarget()
    {
        Vector3 destination = target.position;

        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(destination);
            return;
        }

        Vector3 direction = destination - transform.position;
        direction = FlattenDirection(direction);

        if (direction.magnitude <= stoppingDistance)
        {
            return;
        }

        Vector3 movement = direction.normalized * moveSpeed * Time.deltaTime;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    private void RoamOrIdle()
    {
        if (!roamWhenIdle)
        {
            StopMoving();
            SetMovementAnimation(false, 0f);
            state = EnemyState.Idle;
            return;
        }

        if (!hasRoamDestination && Time.time >= nextRoamTime)
        {
            hasRoamDestination = TryChooseRoamDestination(out roamDestination);
            if (!hasRoamDestination)
            {
                ScheduleNextRoam();
            }
        }

        if (!hasRoamDestination)
        {
            StopMoving();
            SetMovementAnimation(false, 0f);
            state = EnemyState.Idle;
            return;
        }

        state = EnemyState.Roaming;
        SetMovementAnimation(true, 0.35f);
        MoveTowardRoamDestination();
    }

    private bool TryChooseRoamDestination(out Vector3 destination)
    {
        for (int i = 0; i < roamPointAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = roamOrigin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
                {
                    destination = hit.position;
                    return true;
                }
            }
            else
            {
                destination = candidate;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    private void MoveTowardRoamDestination()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.speed = roamSpeed;
            navMeshAgent.stoppingDistance = roamStoppingDistance;
            navMeshAgent.SetDestination(roamDestination);

            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= roamStoppingDistance)
            {
                FinishRoamStep();
            }

            return;
        }

        Vector3 direction = FlattenDirection(roamDestination - transform.position);
        if (direction.magnitude <= roamStoppingDistance)
        {
            FinishRoamStep();
            return;
        }

        FaceDirection(direction);
        Vector3 movement = direction.normalized * roamSpeed * Time.deltaTime;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += movement;
        }
    }

    private void FinishRoamStep()
    {
        hasRoamDestination = false;
        ScheduleNextRoam();
        StopMoving();
        SetMovementAnimation(false, 0f);
        state = EnemyState.Idle;
    }

    private void ScheduleNextRoam()
    {
        float waitTime = Random.Range(minRoamWaitTime, maxRoamWaitTime);
        nextRoamTime = Time.time + waitTime;
    }

    private void FaceTarget()
    {
        Vector3 direction = FlattenDirection(target.position - transform.position);
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction = FlattenDirection(direction);
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        animator?.SetTrigger(attackTrigger);

        if (targetDamageable == null)
        {
            CacheTargetDamageable();
        }

        targetDamageable?.TakeDamage(attackDamage);
    }

    private void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;
        }
    }

    public void StopEnemy()
    {
        StopMoving();
        SetMovementAnimation(false, 0f);
        enabled = false;
    }

    private void SetMovementAnimation(bool isMoving, float normalizedSpeed)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(movingParameter, isMoving);
        animator.SetFloat(speedParameter, normalizedSpeed, animatorDampTime, Time.deltaTime);
    }

    private Vector3 FlattenDirection(Vector3 direction)
    {
        if (keepMovementFlat)
        {
            direction.y = 0f;
        }

        return direction;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityDetectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, lineOfSightRadius);

        Gizmos.color = state == EnemyState.Attacking ? Color.red : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.green;
        Vector3 origin = Application.isPlaying ? roamOrigin : transform.position;
        Gizmos.DrawWireSphere(origin, roamRadius);

        if (Application.isPlaying && hasRoamDestination)
        {
            Gizmos.DrawLine(transform.position, roamDestination);
            Gizmos.DrawWireSphere(roamDestination, 0.35f);
        }

        Vector3 leftRay = Quaternion.Euler(0f, -fieldOfViewAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0f, fieldOfViewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + eyeOffset, leftRay * lineOfSightRadius);
        Gizmos.DrawRay(transform.position + eyeOffset, rightRay * lineOfSightRadius);
    }
}
