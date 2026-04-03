using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    private int _damage;
    private Color _hitboxColor = new Color(1f, 0.1f, 0.1f, 0.35f);

    public void Init(int damage)
    {
        _damage = damage;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _hitboxColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        var entity = other.GetComponent<EntityStats>();
        if (entity != null)
            entity.TakeDamage(_damage, transform.position);
    }
}