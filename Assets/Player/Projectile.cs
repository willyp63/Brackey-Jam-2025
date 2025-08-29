using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private GameObject particleEffectPrefab;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private float rotationSpeed = 100f;

    [SerializeField]
    private float pushForce = 10f;

    [SerializeField]
    private bool isPlayerProjectile = true;

    private Vector2 direction;
    private float speed = 10f; // Default speed
    private float lifetime = 5f; // Default lifetime
    private int damage = 10; // Default damage
    private float timer = 0f;
    private Vector3 spawnPosition; // Added to store the spawn position

    private bool isDestroyed = false;

    public void Initialize(
        Vector2 direction,
        float customSpeed = -1f,
        float customLifetime = -1f,
        int customDamage = -1
    )
    {
        this.direction = direction.normalized;
        timer = 0f;
        spawnPosition = transform.position; // Store the spawn position

        // Use custom values if provided, otherwise use default values
        if (customSpeed > 0)
            speed = customSpeed;
        if (customLifetime > 0)
            lifetime = customLifetime;
        if (customDamage > 0)
            damage = customDamage;

        // Move projectile out a bit so it doesn't look like it's coming from the player center
        transform.Translate(direction * speed * 0.015f);
    }

    void Update()
    {
        // Move projectile only if not stopped
        if (!isDestroyed)
        {
            transform.Translate(direction * speed * Time.deltaTime);
            spriteRenderer.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        // Update lifetime timer
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            DestroyProjectile();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (isPlayerProjectile)
        {
            // Try to deal damage to enemies
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                enemy.Push(direction * pushForce);
            }
        }
        else
        {
            // Try to deal damage to player
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
                player.Push(direction * pushForce);
            }
        }

        // Try to deal damage to falling blocks
        FallingBlock fallingBlock = other.GetComponent<FallingBlock>();
        if (fallingBlock != null)
        {
            fallingBlock.TakeDamage(damage);
        }

        // Try to deal damage to blocks
        BlockManager blockManager = other.GetComponent<BlockManager>();
        if (blockManager != null)
        {
            Vector3Int blockGridPosition = blockManager.DamageBlock(transform.position, damage);

            // Move projectile to be on the perimeter of the block
            Vector3 perimeterPosition = blockManager.GetProjectilePerimeterPosition(
                transform.position,
                direction,
                spawnPosition,
                blockGridPosition
            );
            transform.position = perimeterPosition;
        }

        // Destroy the projectile
        DestroyProjectile();
    }

    void DestroyProjectile()
    {
        spriteRenderer.enabled = false;

        GameObject particleEffect = Instantiate(
            particleEffectPrefab,
            transform.position,
            Quaternion.identity
        );
        particleEffect.transform.SetParent(transform);
        particleEffect.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        // Start coroutine to wait and destroy
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        // Wait 0.3 seconds
        yield return new WaitForSeconds(0.3f);

        // Destroy the projectile
        Destroy(gameObject);
    }
}
