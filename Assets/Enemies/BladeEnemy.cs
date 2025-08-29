using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeEnemy : Enemy
{
    [Header("Attack Settings")]
    [SerializeField]
    private float chargeForce = 15f;

    [SerializeField]
    private float attackPushForce = 5f;

    [SerializeField]
    private float attackCooldown = 1f;

    private float lastAttackTime = 0f;

    protected override void Attack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Calculate direction to player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Apply a strong charge force towards the player
        rb.AddForce(directionToPlayer * chargeForce, ForceMode2D.Impulse);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (Health <= 0)
            return;

        Player player = other.gameObject.GetComponent<Player>();
        if (player != null)
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            player.TakeDamage(1);
            player.Push(direction * attackPushForce);
            Push(-direction * attackPushForce);
        }
    }
}
