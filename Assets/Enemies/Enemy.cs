using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [SerializeField]
    private LayerMask wallLayer;

    [SerializeField]
    private float visionRange = 12f;

    [SerializeField]
    private float attackRange = 3f;

    [SerializeField]
    private float wanderSpeed = 5f;

    [SerializeField]
    private float chaseSpeed = 5f;

    [SerializeField]
    private float wanderChangeDirectionTime = 3f;

    [SerializeField]
    private float wallDetectionDistance = 0.5f;

    [SerializeField]
    private bool stopWhenInAttackRange = true;

    [Header("Health")]
    [SerializeField]
    private int initialHealth = 100;

    [Header("Misc")]
    [SerializeField]
    private List<SpriteRenderer> spriteRenderers;

    [SerializeField]
    private Color hurtColor = Color.red;

    protected Rigidbody2D rb;
    protected Transform player;
    private Animator animator;
    private Vector2 currentDirection;
    private float lastDirectionChangeTime;
    private bool canSeePlayer;
    private bool isWithinAttackRange;
    private Color originalColor;

    private int health = 0;
    private int maxHealth = 1;
    public int Health => health;
    public int MaxHealth => maxHealth;

    public System.Action onDeath;
    public System.Action onHealthChanged;

    protected virtual void FacePlayer()
    {
        // face the player
        transform.localScale = new Vector3(player.position.x > transform.position.x ? -1 : 1, 1, 1);
    }

    protected abstract void Attack();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null)
        {
            Debug.LogWarning("Player not found! Make sure the player has the 'Player' tag.");
        }

        // Initialize health
        if (spriteRenderers[0] != null)
        {
            originalColor = spriteRenderers[0].color;
        }

        maxHealth = initialHealth;
        health = initialHealth;
        onHealthChanged?.Invoke();

        // Initialize random direction for wandering
        ChangeWanderDirection();
    }

    public void Push(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        onHealthChanged?.Invoke();

        bool isDead = health <= 0;
        StartCoroutine(HurtAnimation(isDead));

        if (isDead)
        {
            rb.gravityScale = 2f;
            onDeath?.Invoke();
            Destroy(gameObject, 0.5f);
        }
    }

    IEnumerator HurtAnimation(bool isDead)
    {
        animator.SetTrigger(isDead ? "Death" : "Hurt");
        foreach (var spriteRenderer in spriteRenderers)
            spriteRenderer.color = hurtColor;

        yield return new WaitForSeconds(0.2f);

        if (!isDead)
            animator.SetTrigger("EndHurt");
        foreach (var spriteRenderer in spriteRenderers)
            spriteRenderer.color = originalColor;
    }

    void Update()
    {
        if (player == null || health <= 0)
            return;

        FacePlayer();

        // Check if we can see the player
        canSeePlayer = CanSeePlayer();

        // Check if we're within attack range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        isWithinAttackRange = distanceToPlayer <= attackRange;

        // Debug current state
        Debug.Log(
            $"[{gameObject.name}] State: canSeePlayer={canSeePlayer}, isWithinAttackRange={isWithinAttackRange}, distanceToPlayer={distanceToPlayer:F2}"
        );

        // Handle movement based on current state
        if (canSeePlayer && !isWithinAttackRange)
        {
            // Chase the player
            Debug.Log($"[{gameObject.name}] Chasing player!");
            ChasePlayer();
        }
        else if (!canSeePlayer)
        {
            // Wander randomly
            Debug.Log($"[{gameObject.name}] Wandering...");
            Wander();
        }
        else
        {
            // Stop moving when within attack range
            Debug.Log($"[{gameObject.name}] Within attack range, stopping!");
            Attack();
            if (stopWhenInAttackRange)
                StopMoving();
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if player is within vision range
        if (distanceToPlayer > visionRange)
        {
            Debug.Log($"[{gameObject.name}] Player too far: {distanceToPlayer:F2} > {visionRange}");
            return false;
        }

        // Cast ray to check line of sight
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            directionToPlayer,
            distanceToPlayer,
            wallLayer
        );

        // If we hit a wall before reaching the player, we can't see them
        if (hit.collider != null)
        {
            Debug.Log(
                $"[{gameObject.name}] Wall blocking vision: {hit.collider.name} at distance {hit.distance:F2}"
            );
            return false;
        }

        Debug.Log($"[{gameObject.name}] Can see player! Distance: {distanceToPlayer:F2}");
        return true;
    }

    private void ChasePlayer()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Check if there's a wall in the way
        if (!IsWallInDirection(directionToPlayer))
        {
            // Move towards player
            Vector2 force = directionToPlayer * chaseSpeed;
            rb.AddForce(force);
            Debug.Log($"[{gameObject.name}] Chasing with force: {force}, velocity: {rb.velocity}");
        }
        else
        {
            // Try to find an alternative path or stop
            Debug.Log($"[{gameObject.name}] Wall in way while chasing, stopping!");
            StopMoving();
        }
    }

    private void Wander()
    {
        // Change direction periodically
        if (Time.time - lastDirectionChangeTime > wanderChangeDirectionTime)
        {
            Debug.Log($"[{gameObject.name}] Time to change wander direction!");
            ChangeWanderDirection();
        }

        // Check if there's a wall ahead
        if (!IsWallInDirection(currentDirection))
        {
            // Move in current direction
            Vector2 force = currentDirection * wanderSpeed;
            rb.AddForce(force);
            Debug.Log(
                $"[{gameObject.name}] Wandering in direction: {currentDirection}, force: {force}, velocity: {rb.velocity}"
            );
        }
        else
        {
            // Wall detected, change direction immediately
            Debug.Log($"[{gameObject.name}] Wall detected while wandering, changing direction!");
            ChangeWanderDirection();
        }
    }

    private void ChangeWanderDirection()
    {
        // Generate a random direction
        float randomAngle = Random.Range(0f, 360f);
        currentDirection = new Vector2(
            Mathf.Cos(randomAngle * Mathf.Deg2Rad),
            Mathf.Sin(randomAngle * Mathf.Deg2Rad)
        );

        lastDirectionChangeTime = Time.time;
        Debug.Log(
            $"[{gameObject.name}] New wander direction: {currentDirection} (angle: {randomAngle:F1}°)"
        );
    }

    private bool IsWallInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            wallDetectionDistance,
            wallLayer
        );

        if (hit.collider != null)
        {
            Debug.Log(
                $"[{gameObject.name}] Wall detected in direction {direction}: {hit.collider.name} at distance {hit.distance:F2}"
            );
        }

        return hit.collider != null;
    }

    private void StopMoving()
    {
        // Apply damping to slow down movement
        rb.velocity *= 0.9f;

        // If moving very slowly, stop completely
        if (rb.velocity.magnitude < 0.1f)
        {
            rb.velocity = Vector2.zero;
            Debug.Log($"[{gameObject.name}] Completely stopped");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Slowing down, current velocity: {rb.velocity}");
        }
    }

    // Optional: Visualize the vision range and detection in the editor
    private void OnDrawGizmosSelected()
    {
        // Vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Current movement direction
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, currentDirection * 2f);
        }
    }
}
