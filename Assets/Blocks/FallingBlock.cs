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
        if (collision.gameObject.GetComponent<BlockManager>() == blockManager)
        {
            transform.position = blockManager.GetNearestGridPosition(transform.position);
            DestroyBlock();
        }
    }

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
