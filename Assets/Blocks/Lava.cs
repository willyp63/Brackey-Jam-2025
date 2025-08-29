using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 2f; // Speed of upward movement

    [SerializeField]
    private GameObject fireballPrefab;

    [SerializeField]
    private float fireballLaunchMinX = 5f;

    [SerializeField]
    private float fireballLaunchMaxX = 10f;

    [SerializeField]
    private float fireballLaunchInterval = 1f;

    [SerializeField]
    private float fireballLaunchMinXForce = 100f;

    [SerializeField]
    private float fireballLaunchMaxXForce = 200f;

    [SerializeField]
    private float fireballLaunchMinYForce = 100f;

    [SerializeField]
    private float fireballLaunchMaxYForce = 200f;

    private float lastFireballLaunchTime = 0f;
    private List<GameObject> activeFireballs = new List<GameObject>();
    private float originalYPosition;

    private float currentMoveSpeed = 0f;

    // Start is called before the first frame update
    void Start()
    {
        originalYPosition = transform.position.y;
        currentMoveSpeed = moveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        // Move the lava upward at a constant speed
        transform.Translate(Vector3.up * currentMoveSpeed * Time.deltaTime);

        // Launch fireballs at regular intervals
        if (Time.time - lastFireballLaunchTime >= fireballLaunchInterval)
        {
            LaunchFireball();
            lastFireballLaunchTime = Time.time;
        }

        // Check and cleanup fireballs that have returned to original Y position
        CleanupFireballs();
    }

    public void SetMoveSpeed(float moveSpeed)
    {
        currentMoveSpeed = moveSpeed;
    }

    void LaunchFireball()
    {
        if (fireballPrefab == null)
            return;

        bool isWithinOuterRange = Random.Range(0, 2) == 0;

        // Choose random X coordinate between min and max
        float randomSign = Random.Range(0, 2) == 0 ? -1f : 1f;
        float randomX = isWithinOuterRange
            ? Random.Range(fireballLaunchMinX, fireballLaunchMaxX) * randomSign
            : Random.Range(-fireballLaunchMinX, fireballLaunchMinX);
        Vector3 spawnPosition = new Vector3(randomX, transform.position.y, 0f);

        // Spawn the fireball
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        activeFireballs.Add(fireball);

        // Get the Rigidbody2D component
        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Apply random force towards the center of the lava
            float randomXForce =
                Random.Range(fireballLaunchMinXForce, fireballLaunchMaxXForce)
                * (isWithinOuterRange ? Mathf.Sign(randomX) : randomSign)
                * -1f;
            float randomYForce = Random.Range(fireballLaunchMinYForce, fireballLaunchMaxYForce);

            // Apply impulse force
            Vector2 force = new Vector2(randomXForce, randomYForce);
            rb.AddForce(force, ForceMode2D.Impulse);
        }
    }

    void CleanupFireballs()
    {
        for (int i = activeFireballs.Count - 1; i >= 0; i--)
        {
            if (activeFireballs[i] == null)
            {
                activeFireballs.RemoveAt(i);
                continue;
            }

            // Check if fireball has returned to original Y position or lower
            if (activeFireballs[i].transform.position.y < originalYPosition)
            {
                Destroy(activeFireballs[i]);
                activeFireballs.RemoveAt(i);
            }
        }
    }
}
