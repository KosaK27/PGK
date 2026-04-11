using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 10;
    public float speed = 8f;
    public float lifetime = 4f;
    public bool hitsPlayer = true;
    public bool hitsEntities = false;

    private Vector2 _direction;
    private float _spawnTime;
    private Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 direction, int dmg, float spd,
                     bool hitsPlayer, bool hitsEntities,
                     GameObject shooter = null)
    {
        _direction = direction.normalized;
        damage = dmg;
        speed = spd;
        this.hitsPlayer = hitsPlayer;
        this.hitsEntities = hitsEntities;
        _spawnTime = Time.time;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (shooter != null)
        {
            var shooterCol = shooter.GetComponent<Collider2D>();
            var myCol = GetComponent<Collider2D>();
            if (shooterCol != null && myCol != null)
                Physics2D.IgnoreCollision(myCol, shooterCol);
        }

        _rb.linearVelocity = _direction * speed;
    }

    void Update()
    {
        if (Time.time - _spawnTime > lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Pocisk trafi³: {other.gameObject.name}");

        if (other.GetComponent<Projectile>() != null) return;

        if (hitsPlayer)
        {
            var player = other.GetComponent<PlayerStats>();
            if (player != null)
            {
                player.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
        }

        if (hitsEntities)
        {
            var entity = other.GetComponent<EntityStats>();
            if (entity != null)
            {
                entity.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
        }

        if (other.isTrigger) return;
        Destroy(gameObject);
    }
}