using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 20;
    public int currentHP;
    [SerializeField] private float iframeDuration = 0.6f;

    private float _iframeTimer;
    private HitEffect _hitEffect;

    public event System.Action<int, int> OnHealthChanged;

    void Start()
    {
        currentHP = maxHP;
        _hitEffect = GetComponent<HitEffect>();
    }

    void Update()
    {
        if (_iframeTimer > 0f)
        {
            _iframeTimer -= Time.deltaTime;
            if (_iframeTimer <= 0f)
                _hitEffect?.StopIframes();
        }
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        if (_iframeTimer > 0f) return;

        currentHP -= damage;
        currentHP = Mathf.Max(0, currentHP);

        _iframeTimer = iframeDuration;
        if (_hitEffect != null)
        {
            _hitEffect.TriggerHit(sourcePosition ?? (Vector2)transform.position);
            _hitEffect.StartIframes();
        }

        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0) Die();
    }

    public void Die()
    {
        StartCoroutine(Respawn());
    }

    System.Collections.IEnumerator Respawn()
    {
        SetRenderersEnabled(false);
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<PlayerMovement>().enabled = false;

        yield return new WaitForSeconds(3f);

        transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        currentHP = maxHP;

        SetRenderersEnabled(true);
        GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<PlayerMovement>().enabled = true;

        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void SetRenderersEnabled(bool enabled)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.enabled = enabled;
    }
}