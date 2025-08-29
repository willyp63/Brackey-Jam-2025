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

    [Header("Health")]
    [SerializeField]
    private int initialHealth = 100;

    [Header("Loot")]
    [SerializeField]
    private int minGoldDrop = 1;

    [SerializeField]
    private int maxGoldDrop = 2;

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
    private BlockManager blockManager;
    private bool isDead = false;

    private int health = 0;
    private int maxHealth = 1;
    public int Health => health;
    public int MaxHealth => maxHealth;

    public System.Action onDeath;
    public System.Action onHealthChanged;

    public void Initialize(BlockManager blockManager)
    {
        this.blockManager = blockManager;
    }

    protected virtual void FacePlayer(bool canSeePlayer)
    {
        // face the player
        if (canSeePlayer)
            transform.localScale = new Vector3(
                player.position.x > transform.position.x ? -1 : 1,
                1,
                1
            );
        else
            transform.localScale = new Vector3(rb.velocity.x > 0 ? -1 : 1, 1, 1);
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
        if (isDead)
            return;

        health -= damage;
        onHealthChanged?.Invoke();

        isDead = health <= 0;
        StartCoroutine(HurtAnimation(isDead));

        if (isDead)
        {
            rb.gravityScale = 2f;

            blockManager.SpawnLoot(transform.position, minGoldDrop, maxGoldDrop);

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

        // Check if we can see the player
        canSeePlayer = CanSeePlayer();

        FacePlayer(canSeePlayer);

        // Check if we're within attack range
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        isWithinAttackRange = distanceToPlayer <= attackRange;

        // Handle movement based on current state
        if (canSeePlayer && !isWithinAttackRange)
        {
            // Chase the player
            ChasePlayer();
        }
        else if (!canSeePlayer)
        {
            // Wander randomly
            Wander();
        }
        else
        {
            // Stop moving when within attack range
            Attack();
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
            return false;
        }

        return true;
    }

    private void ChasePlayer()
    {
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Move towards player
        Vector2 force = directionToPlayer * chaseSpeed;
        rb.AddForce(force);
    }

    private void Wander()
    {
        // Change direction periodically
        if (Time.time - lastDirectionChangeTime > wanderChangeDirectionTime)
        {
            ChangeWanderDirection();
        }

        // Check if there's a wall ahead
        if (!IsWallInDirection(currentDirection))
        {
            // Move in current direction
            Vector2 force = currentDirection * wanderSpeed;
            rb.AddForce(force);
        }
        else
        {
            // Wall detected, change direction immediately
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
    }

    private bool IsWallInDirection(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            wallDetectionDistance,
            wallLayer
        );

        return hit.collider != null;
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
