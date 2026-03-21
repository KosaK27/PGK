using UnityEngine;

[RequireComponent(typeof(EntityStats))]
public class ContactDamage : MonoBehaviour
{
    private EntityStats _stats;
    private float _damageCooldown;
    private const float DAMAGE_INTERVAL = 0.8f;

    void Awake() => _stats = GetComponent<EntityStats>();

    void Update() => _damageCooldown -= Time.deltaTime;

    void OnCollisionStay2D(Collision2D col)
    {
        if (_stats.IsDead) return;
        if (_damageCooldown > 0f) return;

        var playerStats = col.gameObject.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.TakeDamage(_stats.data.contactDamage, transform.position);
            _damageCooldown = DAMAGE_INTERVAL;
            return;
        }

        var entityStats = col.gameObject.GetComponent<EntityStats>();
        if (entityStats != null && entityStats != _stats)
        {
            entityStats.TakeDamage(_stats.data.contactDamage, transform.position);
            _damageCooldown = DAMAGE_INTERVAL;
        }
    }
}