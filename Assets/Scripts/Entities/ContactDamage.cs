using UnityEngine;

[RequireComponent(typeof(EntityStats))]
public class ContactDamage : MonoBehaviour
{
    private EntityStats _stats;
    private float _damageCooldown;
    private const float DAMAGE_INTERVAL = 0.8f;

    [Header("Knockback Settings")]
    [SerializeField] private float horizontalKnockbackForce = 12f;
    [SerializeField] private float verticalKnockbackForce = 0.5f;

    void Awake() => _stats = GetComponent<EntityStats>();

    void Update() => _damageCooldown -= Time.deltaTime;

    void OnTriggerStay2D(Collider2D other)
    {
        if (_stats.IsDead) return;
        if (_damageCooldown > 0f) return;

        var playerStats = other.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            if (playerStats.IsInIframes) return;

            playerStats.TakeDamage(_stats.data.contactDamage, transform.position);

            _damageCooldown = DAMAGE_INTERVAL;
            return;
        }

        var entityStats = other.GetComponent<EntityStats>();
        if (entityStats != null && entityStats != _stats)
        {
            entityStats.TakeDamage(_stats.data.contactDamage, transform.position);

            ApplyHorizontalPush(transform, other.transform);

            _damageCooldown = DAMAGE_INTERVAL;
        }
    }

    private void ApplyHorizontalPush(Transform attacker, Transform victim)
    {
        Rigidbody2D attackerRb = attacker.GetComponent<Rigidbody2D>();
        Rigidbody2D victimRb = victim.GetComponent<Rigidbody2D>();

        float pushDir = Mathf.Sign(victim.position.x - attacker.position.x);
        if (pushDir == 0) pushDir = 1f;

        if (victimRb != null)
        {
            victimRb.linearVelocity = new Vector2(pushDir * horizontalKnockbackForce, verticalKnockbackForce);

            var victimAI = victim.GetComponent<EntityAI>();
            if (victimAI != null) victimAI.ApplyKnockback();
        }

        if (attackerRb != null)
        {
            attackerRb.linearVelocity = new Vector2(-pushDir * (horizontalKnockbackForce * 0.6f), verticalKnockbackForce);

            var attackerAI = attacker.GetComponent<EntityAI>();
            if (attackerAI != null) attackerAI.ApplyKnockback();
        }
    }
}