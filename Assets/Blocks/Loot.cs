using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Loot : MonoBehaviour
{
    private Player player;
    private Rigidbody2D rb;
    private float originalGravityScale;

    public void Initialize(Player player)
    {
        this.player = player;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < 0.2f)
            {
                // TODO: give player loot
                Destroy(gameObject);
            }
            else if (distance < player.PickupRadius)
            {
                Vector2 direction = (player.transform.position - transform.position).normalized;
                float normalizedDistance = distance / player.PickupRadius;
                float distanceFactor = 1f / (normalizedDistance * normalizedDistance);
                rb.AddForce(direction * player.PickupForce * distanceFactor);
                rb.gravityScale = originalGravityScale * 0.5f;
            }
            else
            {
                rb.gravityScale = originalGravityScale;
            }
        }
    }
}
