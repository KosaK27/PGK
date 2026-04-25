using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    private int _damage;
    private Transform _playerTransform;
    private Color _hitboxColor = new Color(1f, 0.1f, 0.1f, 0.35f);

    public void Init(int damage, Transform playerTransform = null)
    {
        _damage = damage;
        _playerTransform = playerTransform;
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _hitboxColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        var entity = other.GetComponent<EntityStats>();
        if (entity != null)
        {
            Vector2 source = _playerTransform != null
                ? (Vector2)_playerTransform.position
                : (Vector2)transform.position;
            entity.TakeDamage(_damage, source);
        }
    }
}