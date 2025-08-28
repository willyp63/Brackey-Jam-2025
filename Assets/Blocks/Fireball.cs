using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    [Header("Fireball Hit Effect")]
    public GameObject fireballHitEffectPrefab;

    void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(1);

            GameObject fireballHitEffect = Instantiate(
                fireballHitEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            fireballHitEffect.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            Destroy(fireballHitEffect, 0.3f);

            Destroy(gameObject);
        }
    }
}
