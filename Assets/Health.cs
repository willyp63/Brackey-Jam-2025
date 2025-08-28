using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField]
    private float maxHealth = 100f;

    public System.Action onDamage;
    public System.Action onHeal;
    public System.Action onDeath;
    public System.Action onHealthChanged;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;

    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public bool TakeDamage(float damage, bool ignoreInvulnerability = false)
    {
        // Don't take damage if dead
        if (IsDead)
        {
            return false;
        }

        // Reduce health
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        // Trigger events
        onDamage?.Invoke();
        onHealthChanged?.Invoke();

        // Check if dead
        if (IsDead)
        {
            onDeath?.Invoke();
        }

        return true;
    }

    public bool Heal(float healAmount)
    {
        if (IsDead || currentHealth >= maxHealth)
        {
            return false;
        }

        // Increase health
        float oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        // Only trigger events if health actually increased
        if (currentHealth > oldHealth)
        {
            onHeal?.Invoke();
            onHealthChanged?.Invoke();
            return true;
        }

        return false;
    }
}
