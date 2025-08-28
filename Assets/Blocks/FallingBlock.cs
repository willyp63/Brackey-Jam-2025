using System.Collections;
using UnityEngine;

public class FallingBlock : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer damageSpriteRenderer;
    public ShakeBehavior shakeBehavior;

    private BlockManager blockManager;
    private BlockData blockData;
    private Rigidbody2D rb;

    private int health;

    public void Initialize(Block block, BlockManager blockManager)
    {
        this.blockManager = blockManager;
        blockData = block.blockData;

        spriteRenderer.sprite = blockData.sprite;

        health = block.health;
        UpdateDamageSprite();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        StartCoroutine(FallAfterDelay());
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        UpdateDamageSprite();

        if (health <= 0)
        {
            DestroyBlock();
        }
    }

    IEnumerator FallAfterDelay()
    {
        shakeBehavior.Shake();
        yield return new WaitForSeconds(0.3f);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    void UpdateDamageSprite()
    {
        if (health <= 0 || health >= blockData.maxHealth)
        {
            damageSpriteRenderer.sprite = null;
            return;
        }

        damageSpriteRenderer.sprite = blockManager.GetDamageSprite(health, blockData.maxHealth);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check for collision with BlockManager
        if (collision.gameObject.GetComponent<BlockManager>() == blockManager)
        {
            transform.position = blockManager.GetNearestGridPosition(transform.position);
            DestroyBlock();
            return;
        }

        // Check for collision with Player
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            // Check if the block is above the player and player is hitting the bottom of the block
            if (IsBlockAbovePlayer(player, collision))
            {
                // Destroy the block and damage the player
                player.TakeDamage(1);
                DestroyBlock();
            }
        }
    }

    private bool IsBlockAbovePlayer(Player player, Collision2D collision)
    {
        Debug.Log("IsBlockAbovePlayer");

        // Get the colliders
        BoxCollider2D blockCollider = GetComponent<BoxCollider2D>();
        CapsuleCollider2D playerCollider = player.GetComponent<CapsuleCollider2D>();

        if (blockCollider == null || playerCollider == null)
            return false;

        // Calculate the bottom of the block
        float blockBottom =
            transform.position.y - (blockCollider.size.y * transform.localScale.y * 0.5f);

        // Calculate the top of the player
        float playerTop =
            player.transform.position.y
            + (playerCollider.size.y * player.transform.localScale.y * 0.5f);

        Debug.Log($"Block bottom: {blockBottom}, Player top: {playerTop}");

        // Check if block is above player
        if (blockBottom + 0.1f <= playerTop)
            return false;

        return true;
    }

    // 0.03125
    // 0.0625
    // 0.09375
    // 0.125
    // 0.15625
    // 0.1875
    // 0.21875
    // 0.25
    // 0.28125
    // 0.3125
    // 0.34375

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<Lava>() != null)
        {
            DestroyBlock();
        }
    }

    void DestroyBlock()
    {
        if (blockManager.IsBlockOnScreen(transform.position))
        {
            blockManager.SpawnBlockDestroyEffect(transform.position, blockData);
            blockManager.SpawnLoot(transform.position, blockData);
        }

        Destroy(gameObject);
    }
}
