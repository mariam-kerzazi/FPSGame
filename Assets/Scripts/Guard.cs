using UnityEngine;
using UnityEngine.AI;

public enum GuardState { Patrol, Chase, Flee, Dead }

// Guard AI: a small state machine (Patrol / Chase / Flee / Dead) driven by a NavMeshAgent.
// Attach to each guard capsule (which needs a NavMeshAgent and a Capsule Collider).
[RequireComponent(typeof(NavMeshAgent))]
public class Guard : MonoBehaviour
{
    [Header("What this guard protects")]
    public TreasureBox treasure;
    public float patrolRadius = 5f;       // random patrol around the guard's start position

    [Header("Speeds")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float fleeSpeed = 5.5f;

    [Header("Combat")]
    public int health = 3;
    public GameObject bulletPrefab;
    public float shootRange = 14f;
    public float shootInterval = 1.5f;
    public float bulletSpeed = 18f;
    public float aimError = 0.6f;         // bigger = less accurate
    public float reactionDelay = 0.7f;    // pause before the first shot of a chase
    public float chaseStopDistance = 5f;

    public bool IsDead { get { return state == GuardState.Dead; } }

    private GuardState state = GuardState.Patrol;
    private NavMeshAgent agent;
    private Transform player;
    private Vector3 patrolCenter;
    private float waitTimer = 0f;
    private float nextShootTime;
    private float nextPathTime;
    private float nextFleeTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameManager.Instance.player.transform;
        patrolCenter = transform.position;
        EnterState(GuardState.Patrol);
    }

    void Update()
    {
        if (state == GuardState.Dead) return;

        // 1. Decide which state we should be in.
        GuardState desired = DecideState();
        if (desired != state)
        {
            EnterState(desired);
        }

        // 2. Run that state's behaviour.
        switch (state)
        {
            case GuardState.Patrol: UpdatePatrol(); break;
            case GuardState.Chase: UpdateChase(); break;
            case GuardState.Flee: UpdateFlee(); break;
        }
    }

    // ---------- State decision ----------
    private GuardState DecideState()
    {
        // After all treasures are stolen the guards run away from the player.
        if (GameManager.Instance.AllTreasuresCollected) return GuardState.Flee;

        // Chase if our treasure was stolen (anywhere!) or the player is inside the protection zone.
        if (treasure != null && (treasure.IsCollected || PlayerInProtectionZone()))
        {
            return GuardState.Chase;
        }

        return GuardState.Patrol;
    }

    // The protection zone is an invisible circle around the treasure box (ignoring height).
    private bool PlayerInProtectionZone()
    {
        Vector3 a = treasure.transform.position;
        Vector3 b = player.position;
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b) <= treasure.zoneRadius;
    }

    private void EnterState(GuardState newState)
    {
        state = newState;
        agent.isStopped = false;

        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0f;
                waitTimer = 0f;
                PickPatrolPoint();
                break;
            case GuardState.Chase:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = chaseStopDistance;
                nextShootTime = Time.time + reactionDelay;
                nextPathTime = 0f;
                break;
            case GuardState.Flee:
                agent.speed = fleeSpeed;
                agent.stoppingDistance = 0f;
                nextFleeTime = 0f;
                break;
        }
    }

    // ---------- Patrol ----------
    private void UpdatePatrol()
    {
        if (agent.pathPending) return;

        // Arrived? Wait a moment, then pick a new random point.
        if (agent.remainingDistance <= agent.stoppingDistance + 0.3f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                PickPatrolPoint();
                waitTimer = Random.Range(1f, 3f);
            }
        }
    }

    private void PickPatrolPoint()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = patrolCenter + new Vector3(r.x, 0f, r.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                // Make sure the point is actually reachable (not, say, on top of a wall).
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetPath(path);
                    return;
                }
            }
        }
    }

    // ---------- Chase + shoot ----------
    private void UpdateChase()
    {
        // Re-path a few times per second rather than every frame.
        if (Time.time >= nextPathTime)
        {
            agent.SetDestination(player.position);
            nextPathTime = Time.time + 0.2f;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= shootRange && HasLineOfSight())
        {
            FaceTarget();
            if (Time.time >= nextShootTime)
            {
                Shoot();
                nextShootTime = Time.time + shootInterval;
            }
        }
    }

    private bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = player.position - origin;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude + 0.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PlayerController>() != null;
        }
        return false;
    }

    private void FaceTarget()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        }
    }

    private void Shoot()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f + transform.forward * 0.8f;
        Vector3 target = player.position + Random.insideUnitSphere * aimError;
        Vector3 dir = (target - origin).normalized;

        Instantiate(bulletPrefab, origin, Quaternion.LookRotation(dir)).GetComponent<GuardBullet>().speed = bulletSpeed;
        AudioManager.Instance.Play(AudioManager.Instance.guardShot, origin);
    }

    // ---------- Flee ----------
    private void UpdateFlee()
    {
        if (Time.time < nextFleeTime) return;
        nextFleeTime = Time.time + 0.5f;

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        away = away.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, away) * Random.Range(-0.5f, 0.5f);
        Vector3 target = transform.position + (away + side).normalized * 10f;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 6f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // ---------- Damage ----------
    // Called by the player's gun (1 damage per hit).
    public void TakeDamage(int damage)
    {
        if (state == GuardState.Dead) return;

        health -= damage;
        AudioManager.Instance.Play(AudioManager.Instance.guardHit, transform.position);
        Debug.Log(name + " hit! Health left: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    // Called by a grenade explosion: instant kill.
    public void Kill()
    {
        if (state == GuardState.Dead) return;
        health = 0;
        AudioManager.Instance.Play(AudioManager.Instance.guardHit, transform.position);
        Die();
    }

    private void Die()
    {
        state = GuardState.Dead;
        agent.enabled = false;

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // Tip over so it's obvious the guard is dead, then remove it.
        transform.Rotate(90f, 0f, 0f);
        transform.position -= Vector3.up * 0.5f;
        Destroy(gameObject, 2f);

        GameManager.Instance.GuardKilled();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 c = Application.isPlaying ? patrolCenter : transform.position;
        Gizmos.DrawWireSphere(c, patrolRadius);
    }
}
