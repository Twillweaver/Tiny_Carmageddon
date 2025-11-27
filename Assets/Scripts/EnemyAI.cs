using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }
    public State currentState = State.Idle;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] patrolPoints;

    [Header("Stats")]
    public float sightRange = 12f;
    public float attackRange = 2f;
    public float fieldOfView = 120f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    private int patrolIndex = 0;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;
        if (patrolPoints.Length > 0) currentState = State.Patrol;
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                Idle();
                break;
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                Attack();
                break;
        }
    }

    // ------------------------
    //  STATE: IDLE
    // ------------------------
    void Idle()
    {
        if (CanSeePlayer()) currentState = State.Chase;
    }

    // ------------------------
    //  STATE: PATROL
    // ------------------------
    void Patrol()
    {
        if (patrolPoints.Length == 0)
        {
            currentState = State.Idle;
            return;
        }

        agent.SetDestination(patrolPoints[patrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        if (CanSeePlayer()) currentState = State.Chase;
    }

    // ------------------------
    //  STATE: CHASE
    // ------------------------
    void Chase()
    {
        if (!CanSeePlayer())
        {
            currentState = State.Patrol;
            return;
        }

        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) < attackRange)
        {
            currentState = State.Attack;
        }
    }

    // ------------------------
    //  STATE: ATTACK
    // ------------------------
    void Attack()
    {
        agent.SetDestination(transform.position); // Stop moving
        transform.LookAt(player);

        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            currentState = State.Chase;
            return;
        }

        if (Time.time > lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Debug.Log("Enemy attacks player!");

            // Insert player damage code here
        }
    }

    // ------------------------
    //  DETECTION LOGIC
    // ------------------------
    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = player.position - transform.position;

        if (dirToPlayer.magnitude > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView / 2f) return false;

        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer.normalized, out RaycastHit hit, sightRange))
        {
            if (hit.transform == player) return true;
        }

        return false;
    }
}
