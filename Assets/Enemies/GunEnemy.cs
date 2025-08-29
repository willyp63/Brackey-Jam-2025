using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunEnemy : Enemy
{
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

    [Header("Misc")]
    [SerializeField]
    private GameObject mainGun;

    [SerializeField]
    private GameObject offGun;

    [SerializeField]
    private float maxWeaponRotationAngle = 80f;

    private float lastFireTime = 0f;

    protected override void Attack()
    {
        if (projectilePrefab == null)
            return;

        if (Time.time < lastFireTime + fireRate)
            return;

        lastFireTime = Time.time;

        // Calculate spawn position (use projectileSpawnPoint if assigned, otherwise use enemy position)
        Vector3 spawnPosition =
            projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;

        // Calculate direction from enemy to player
        Vector2 direction = (player.position - spawnPosition).normalized;

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

    protected override void FacePlayer()
    {
        base.FacePlayer();

        // Rotate weapons to face player
        RotateWeaponsToPlayer();
    }

    void RotateWeaponsToPlayer()
    {
        if (mainGun == null && offGun == null)
            return;

        // Calculate direction from enemy to player
        Vector2 directionToPlayer = (player.position - transform.position).normalized;

        // Calculate the angle to the player
        float angleToPlayer = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        // Clamp the angle based on enemy facing direction
        float clampedAngle = ClampWeaponAngle(angleToPlayer);

        // Apply rotation to weapons
        if (mainGun != null)
        {
            mainGun.transform.rotation = Quaternion.Euler(0, 0, clampedAngle);
        }

        if (offGun != null)
        {
            offGun.transform.rotation = Quaternion.Euler(0, 0, clampedAngle);
        }
    }

    float ClampWeaponAngle(float targetAngle)
    {
        // Get enemy facing direction (1 for right, -1 for left)
        float enemyFacingDirection = transform.localScale.x;

        // Adjust angle based on enemy facing direction
        if (enemyFacingDirection > 0)
        {
            // Enemy facing left, flip the angle
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
}
