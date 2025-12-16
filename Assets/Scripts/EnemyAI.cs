using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    // Define possible states for the enemy
    public enum State { Idle, Patrol, Chase, Attack }

    // Current state of the enemy
    public State currentState = State.Idle;

    [Header("References")]
    // NavMeshAgent for movement
    public NavMeshAgent agent;
    public Transform player;
    // Patrol waypoints
    public Transform[] patrolPoints;

    [Header("Stats")]
    // How far the enemy can see the player
    public float sightRange = 12f;
    // Range within which the enemy can attack
    public float attackRange = 2f;
    // Enemy field of view in degrees
    public float fieldOfView = 120f;
    // Minimum time between attacks
    public float attackCooldown = 1.5f;
    // Tracks the last attack time
    private float lastAttackTime;

    // Current index of patrol point
    private int patrolIndex = 0;

    void Start()
    {
        // Ensure agent is assigned
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Find the player by tag if not assigned
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;

        // If patrol points exist, start in Patrol state
        if (patrolPoints.Length > 0) currentState = State.Patrol;
    }

    void Update()
    {
        // Execute behavior based on current state
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
        // If the enemy can see the player, start chasing
        if (CanSeePlayer()) currentState = State.Chase;
    }

    // ------------------------
    //  STATE: PATROL
    // ------------------------
    void Patrol()
    {
        // If no patrol points, switch to idle
        if (patrolPoints.Length == 0)
        {
            currentState = State.Idle;
            return;
        }

        // Move toward current patrol point
        agent.SetDestination(patrolPoints[patrolIndex].position);

        // If reached the current point, go to the next
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        }

        // If the enemy sees the player, start chasing
        if (CanSeePlayer()) currentState = State.Chase;
    }

    // ------------------------
    //  STATE: CHASE
    // ------------------------
    void Chase()
    {
        // If player is lost, return to patrol
        if (!CanSeePlayer())
        {
            currentState = State.Patrol;
            return;
        }

        // Move toward the player
        agent.SetDestination(player.position);

        // If close enough, switch to attack
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
        // Stop moving
        agent.SetDestination(transform.position);

        // Face the player
        transform.LookAt(player);

        // If player moved out of range, switch back to chase
        if (Vector3.Distance(transform.position, player.position) > attackRange)
        {
            currentState = State.Chase;
            return;
        }

        // Check if cooldown has passed to attack again
        if (Time.time > lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Debug.Log("Enemy attacks player!");

            // TO DO: Insert player damage code here
        }
    }

    // ------------------------
    //  DETECTION LOGIC
    // ------------------------
    bool CanSeePlayer()
    {
        // Direction vector from enemy to player
        Vector3 dirToPlayer = player.position - transform.position;

        // If player is too far, cannot see
        if (dirToPlayer.magnitude > sightRange) return false;

        // Check if player is within field of view
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > fieldOfView / 2f) return false;

        // Raycast to see if there is a clear line of sight
        if (Physics.Raycast(transform.position + Vector3.up, dirToPlayer.normalized, out RaycastHit hit, sightRange))
        {
            // Only true if the ray hits the player
            if (hit.transform == player) return true;
        }

        // Otherwise, player is not visible
        return false;
    }
}
