using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBlock : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer damageSpriteRenderer;
    public ShakeBehavior shakeBehavior;

    private BlockManager blockManager;
    private BlockData blockData;
    private Health health;
    private Rigidbody2D rb;

    public void Initialize(Block block, BlockManager blockManager)
    {
        this.blockManager = blockManager;
        blockData = block.blockData;

        spriteRenderer.sprite = blockData.sprite;

        health = GetComponent<Health>();
        health.SetMaxHealth(blockData.maxHealth);
        health.SetHealth(block.health);

        health.onHealthChanged += UpdateDamageSprite;
        health.onDeath += DestroyBlock;

        UpdateDamageSprite();

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        StartCoroutine(FallAfterDelay());
    }

    IEnumerator FallAfterDelay()
    {
        shakeBehavior.Shake();
        yield return new WaitForSeconds(0.3f);
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    void UpdateDamageSprite()
    {
        if (health.CurrentHealth <= 0 || health.CurrentHealth >= health.MaxHealth)
        {
            damageSpriteRenderer.sprite = null;
            return;
        }

        damageSpriteRenderer.sprite = blockManager.GetDamageSprite(
            health.CurrentHealth,
            health.MaxHealth
        );
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
        blockManager.SpawnBlockDestroyEffect(transform.position, blockData);
        Destroy(gameObject);
    }
}
