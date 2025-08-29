using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveForce = 10f;

    [SerializeField]
    private float jumpForce = 12f;

    [SerializeField]
    private float jumpHoldForce = 10f;

    [SerializeField]
    private float jumpMaxVelocity = 10f;

    [SerializeField]
    private float jumpHoldGravityScale = 0.25f;

    [SerializeField]
    private float jumpMinTime = 0.1f;

    [SerializeField]
    private float jumpHoldMaxTime = 0.5f;

    [SerializeField]
    private float maxSpeed = 8f;

    [SerializeField]
    private float deceleration = 0.1f;

    [SerializeField]
    private float friction = 1f;

    [Header("Ground Check")]
    [SerializeField]
    private List<Transform> groundChecks;

    [SerializeField]
    private float groundCheckRadius = 0.2f;

    [SerializeField]
    private LayerMask groundLayerMask = 1;

    [Header("Weapons")]
    [SerializeField]
    private Transform mainWeapon;

    [SerializeField]
    private Transform offWeapon;

    [SerializeField]
    private float maxWeaponRotationAngle = 45f;

    [Header("Projectiles")]
    [SerializeField]
    private GameObject projectilePrefab;

    [SerializeField]
    private Transform projectileSpawnPoint;

    [SerializeField]
    private float projectileSpeed = 15f;

    [SerializeField]
    private float projectileLifetime = 5f;

    [SerializeField]
    private int projectileDamage = 25;

    [SerializeField]
    private float fireRate = 0.2f;

    [SerializeField]
    private bool canFireContinuously = true;

    [SerializeField]
    private float projectileEnergyCost = 0.1f;

    [Header("Loot")]
    [SerializeField]
    private float pickupRadius = 1f;
    public float PickupRadius => pickupRadius;

    [SerializeField]
    private float pickupForce = 4f;
    public float PickupForce => pickupForce;

    [Header("Energy")]
    [SerializeField]
    private int initialEnergy = 3;

    [SerializeField]
    private float energyRegenRate = 0.1f;

    [SerializeField]
    private float energyDrainRate = 0.1f;

    [Header("Health")]
    [SerializeField]
    private int initialHealth = 3;

    [Header("Misc")]
    [SerializeField]
    private GameObject jumpGlow;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Color hurtColor = Color.red;

    private Rigidbody2D rb;
    private Animator animator;
    private bool isGrounded;
    private float moveInput;
    private bool isJumping = false;
    private float lastJumpTime = 0;
    private float originalGravityScale;
    private float lastFireTime = 0f;
    private float originalDrag;
    private Color originalColor;
    private ShakeBehavior damageShakeEffect;
    private bool isInLava = false;
    private float lastLavaDamageTime = 0f;

    private int health = 0;
    private int maxHealth = 1;
    public int Health => health;
    public int MaxHealth => maxHealth;

    private float energy = 0;
    private int maxEnergy = 1;
    public float Energy => energy;
    public int MaxEnergy => maxEnergy;

    private int gold = 0;
    public int Gold => gold;

    public System.Action onDeath;
    public System.Action onHealthChanged;
    public System.Action onGoldChanged;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalGravityScale = rb.gravityScale;
        originalDrag = rb.drag;
        originalColor = spriteRenderer.color;
        jumpGlow.SetActive(false);
        damageShakeEffect = Camera.main.GetComponent<ShakeBehavior>();

        maxEnergy = initialEnergy;
        energy = initialEnergy;

        maxHealth = initialHealth;
        health = initialHealth;
        onHealthChanged?.Invoke();

        gold = 0;
        onGoldChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        onGoldChanged?.Invoke();
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        onHealthChanged?.Invoke();

        bool isDead = health <= 0;
        StartCoroutine(HurtAnimation(isDead));

        if (isDead)
        {
            rb.gravityScale = originalGravityScale * 2f;
            onDeath?.Invoke();
        }
    }

    IEnumerator HurtAnimation(bool isDead)
    {
        Time.timeScale = 0.2f;
        animator.SetTrigger(isDead ? "Death" : "Hurt");
        spriteRenderer.color = hurtColor;
        damageShakeEffect.Shake();

        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        if (!isDead)
            animator.SetTrigger("EndHurt");
        spriteRenderer.color = originalColor;
    }

    void Update()
    {
        if (health <= 0)
            return;

        // Get input
        moveInput = Input.GetAxisRaw("Horizontal");

        // Jump input
        // if (Input.GetButtonDown("Jump") && isGrounded)
        // {
        //     Jump();
        // }

        if (isInLava && Time.time > lastLavaDamageTime + 0.5f)
        {
            TakeDamage(1);
            lastLavaDamageTime = Time.time;
        }

        if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W)) && energy > 0.1f)
        {
            rb.gravityScale = originalGravityScale * jumpHoldGravityScale;

            float maxVelocityFactor = Mathf.Clamp01(
                Mathf.Abs(jumpMaxVelocity - rb.velocity.y) / jumpMaxVelocity
            );
            rb.AddForce(
                new Vector2(0, jumpHoldForce * maxVelocityFactor * Time.deltaTime),
                ForceMode2D.Force
            );

            energy -= Time.deltaTime * energyDrainRate;
            energy = Mathf.Clamp(energy, 0, maxEnergy);

            Debug.Log("Energy: " + energy);

            jumpGlow.SetActive(true);
        }
        else
        {
            rb.gravityScale = originalGravityScale;
            jumpGlow.SetActive(false);

            if (Time.time > lastFireTime + fireRate + 0.2f)
            {
                energy += Time.deltaTime * energyRegenRate;
                energy = Mathf.Clamp(energy, 0, maxEnergy);
            }
        }

        // Fire projectile input
        if ((canFireContinuously && Input.GetMouseButton(0)) || Input.GetMouseButtonDown(0))
        {
            FireProjectile();
        }

        // Check if grounded
        CheckGrounded();

        // Rotate weapons to face mouse
        RotateWeaponsToMouse();

        // Land animation
        if (isJumping && isGrounded && Time.time - lastJumpTime > 0.1f)
        {
            isJumping = false;
        }

        // Update animation
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsRunning", Mathf.Abs(moveInput) > 0.1f);

        // Make player face mouse direction instead of movement direction
        FaceMouseDirection();
    }

    void FixedUpdate()
    {
        // Apply horizontal movement force
        Move();

        // Limit maximum speed
        LimitSpeed();
    }

    void Move()
    {
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            float desiredSpeed = maxSpeed * Mathf.Sign(moveInput);
            float maxSpeedFactor = Mathf.Clamp01(
                Mathf.Abs(desiredSpeed - rb.velocity.x) / maxSpeed
            );
            float frictionFactor = isGrounded ? friction * 0.66f + 0.33f : 0.33f;
            float force = moveForce * maxSpeedFactor * frictionFactor * Mathf.Sign(moveInput);
            rb.AddForce(new Vector2(force, 0), ForceMode2D.Force);
        }
        else if (isGrounded)
        {
            rb.AddForce(
                new Vector2(-rb.velocity.x * deceleration * friction, 0),
                ForceMode2D.Force
            );
        }
    }

    void Jump()
    {
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);

        isJumping = true;
        lastJumpTime = Time.time;
    }

    void CheckGrounded()
    {
        isGrounded = false;
        friction = 1f;

        foreach (Transform groundCheck in groundChecks)
        {
            Collider2D hitCollider = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayerMask
            );

            if (hitCollider != null)
            {
                isGrounded = true;

                BlockManager blockManager = hitCollider.GetComponent<BlockManager>();
                if (blockManager != null)
                {
                    friction = blockManager.GetFriction(groundCheck.position);
                }

                break;
            }
        }
    }

    void LimitSpeed()
    {
        // Clamp horizontal velocity to max speed
        Vector2 velocity = rb.velocity;
        velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
        rb.velocity = velocity;
    }

    void FaceMouseDirection()
    {
        // Get mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Calculate horizontal direction from player to mouse
        float horizontalDirection = Mathf.Sign(mouseWorldPos.x - transform.position.x);

        // Only update facing direction if mouse is not directly above/below player
        if (Mathf.Abs(mouseWorldPos.x - transform.position.x) > 0.1f)
        {
            transform.localScale = new Vector3(horizontalDirection, 1, 1);
        }
    }

    void RotateWeaponsToMouse()
    {
        if (mainWeapon == null && offWeapon == null)
            return;

        // Get mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Calculate direction from player to mouse
        Vector2 directionToMouse = (mouseWorldPos - transform.position).normalized;

        // Calculate the angle to the mouse
        float angleToMouse = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

        // Clamp the angle based on player facing direction
        float clampedAngle = ClampWeaponAngle(angleToMouse);

        // Apply rotation to weapons
        if (mainWeapon != null)
        {
            mainWeapon.rotation = Quaternion.Euler(0, 0, clampedAngle);
        }

        if (offWeapon != null)
        {
            offWeapon.rotation = Quaternion.Euler(0, 0, clampedAngle);
        }
    }

    float ClampWeaponAngle(float targetAngle)
    {
        // Get player facing direction (1 for right, -1 for left)
        float playerFacingDirection = transform.localScale.x;

        // Adjust angle based on player facing direction
        if (playerFacingDirection < 0)
        {
            // Player facing left, flip the angle
            targetAngle = -FlipAngle(targetAngle);
        }

        // Clamp the angle between -maxWeaponRotationAngle and +maxWeaponRotationAngle
        float clampedAngle = Mathf.Clamp(
            targetAngle,
            -maxWeaponRotationAngle,
            maxWeaponRotationAngle
        );

        return clampedAngle;
    }

    float FlipAngle(float angle)
    {
        // Convert angle to radians and create unit vector
        float radians = angle * Mathf.Deg2Rad;
        Vector2 unitVector = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        // Flip about y-axis (negate x component)
        unitVector.x = -unitVector.x;

        // Convert back to angle
        return Mathf.Atan2(unitVector.y, unitVector.x) * Mathf.Rad2Deg;
    }

    void FireProjectile()
    {
        if (projectilePrefab == null)
            return;

        if (energy < projectileEnergyCost)
            return;

        if (Time.time < lastFireTime + fireRate)
            return;

        energy -= projectileEnergyCost;
        lastFireTime = Time.time;

        // Get mouse position in world space
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // Calculate spawn position (use projectileSpawnPoint if assigned, otherwise use player position)
        Vector3 spawnPosition =
            projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        // Calculate direction from spawn position to mouse
        Vector2 direction = (mouseWorldPos - spawnPosition).normalized;

        // Spawn the projectile
        GameObject projectileObj = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            // Initialize the projectile with the calculated direction and custom values
            projectile.Initialize(direction, projectileSpeed, projectileLifetime, projectileDamage);
        }
    }

    // Visualize ground check in editor
    void OnDrawGizmosSelected()
    {
        foreach (Transform groundCheck in groundChecks)
        {
            if (groundCheck != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Lava>() != null)
        {
            Debug.Log("Player entered lava");
            rb.drag = originalDrag * 50f;
            isInLava = true;
            lastLavaDamageTime = 0f;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Lava>() != null)
        {
            Debug.Log("Player left lava");
            rb.drag = originalDrag;
            isInLava = false;
        }
    }
}
