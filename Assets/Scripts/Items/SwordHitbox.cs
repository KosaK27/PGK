using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [SerializeField] private int   damage      = 20;
    [SerializeField] private Color hitboxColor = new Color(1f, 0.1f, 0.1f, 0.35f);

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = hitboxColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        var entity = other.GetComponent<EntityStats>();
        if (entity != null)
            entity.TakeDamage(damage, transform.position);
    }
}