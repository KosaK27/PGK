using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float fuseTime = 3f;
    [SerializeField] private int playerDamage = 40;
    [SerializeField] private int entityDamage = 60;
    [SerializeField] private float knockbackForce = 15f;

    private Rigidbody2D _rb;
    private float _timer;
    private bool _exploded;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 1.5f;
    }

    void Update()
    {
        if (_exploded) return;
        _timer += Time.deltaTime;
        if (_timer >= fuseTime)
            Explode();
    }

    public void Launch(Vector2 velocity)
    {
        _rb.linearVelocity = velocity;
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        BombExplosionSystem.Instance.Explode(
            transform.position,
            explosionRadius,
            playerDamage,
            entityDamage,
            knockbackForce);

        Destroy(gameObject);
    }
}