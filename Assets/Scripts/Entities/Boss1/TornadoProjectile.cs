using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class TornadoProjectile : MonoBehaviour
{
    [SerializeField] private float descendSpeed = 3f;
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float maxHorizontalDistance = 8f;

    [SerializeField] private float growDuration = 2f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 3f;

    [SerializeField] private int damage = 8;
    [SerializeField] private float damageCooldown = 0.5f;

    [SerializeField] private float hitboxWidth = 1f;
    [SerializeField] private float hitboxHeight = 2f;

    private float _direction;
    private float _targetY;

    private float _elapsed;
    private float _currentScale;
    private float _damageTimer;

    private Rigidbody2D _rb;
    private BoxCollider2D _col;

    public void Init(float direction, float targetY)
    {
        _direction = direction;
        _targetY = targetY;

        _currentScale = minScale;
        _damageTimer = 0f;

        StartCoroutine(TornadoRoutine());
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();

        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        _col.isTrigger = true;
    }

    private IEnumerator TornadoRoutine()
    {
        transform.localScale = Vector3.one * minScale;

        while (transform.position.y > _targetY)
        {
            _rb.linearVelocity = new Vector2(0f, -descendSpeed);
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;

        float distanceTraveled = 0f;

        while (distanceTraveled < maxHorizontalDistance)
        {
            _elapsed += Time.deltaTime;
            distanceTraveled += horizontalSpeed * Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / growDuration);
            _currentScale = Mathf.Lerp(minScale, maxScale, t);

            transform.localScale = Vector3.one * _currentScale;

            _rb.linearVelocity = new Vector2(_direction * horizontalSpeed, 0f);

            yield return null;
        }

        float fadeTime = 0.5f;
        float fadeElapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (fadeElapsed < fadeTime)
        {
            fadeElapsed += Time.deltaTime;
            float t = 1f - fadeElapsed / fadeTime;
            transform.localScale = startScale * t;
            yield return null;
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (_damageTimer > 0f)
            _damageTimer -= Time.deltaTime;

        _col.size = new Vector2(
            hitboxWidth * _currentScale,
            hitboxHeight * _currentScale
        );
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (_damageTimer > 0f) return;

        var player = other.GetComponent<PlayerStats>();
        if (player != null)
        {
            player.TakeDamage(damage, transform.position);
            _damageTimer = damageCooldown;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);

        Vector2 size = new Vector2(
            hitboxWidth * _currentScale,
            hitboxHeight * _currentScale
        );

        Gizmos.DrawWireCube(transform.position, size);
    }
}