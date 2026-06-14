using UnityEngine;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    public int maxHP = 20;
    public int currentHP;
    [SerializeField] private float iframeDuration = 0.6f;
    private float _iframeTimer;
    private HitEffect _hitEffect;
    private GameObject _respawnContainer;
    private TextMeshProUGUI _respawnTimerText;
    public bool IsInIframes => _iframeTimer > 0f;
    public event System.Action<int, int> OnHealthChanged;

    void Start()
    {
        currentHP = maxHP;
        _hitEffect = GetComponent<HitEffect>();

        _respawnContainer = GameObject.Find("Respawn");
        if (_respawnContainer != null)
        {
            Transform timerTransform = _respawnContainer.transform.Find("RespawnTimer");
            if (timerTransform != null)
                _respawnTimerText = timerTransform.GetComponent<TextMeshProUGUI>();
            _respawnContainer.SetActive(false);
        }
    }

    void Update()
    {
        if (_iframeTimer > 0f)
        {
            _iframeTimer -= Time.deltaTime;
            if (_iframeTimer <= 0f)
            {
                _hitEffect?.StopIframes();
                int playerLayer = gameObject.layer;
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            }
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void AddMaxHP(int amount)
    {
        maxHP += amount;
        currentHP += amount;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int damage, Vector2? sourcePosition = null, float knockbackForce = 10f)
    {
        if (_iframeTimer > 0f) return;
        int defense = ArmorSystem.Instance != null ? ArmorSystem.Instance.TotalDefense : 0;
        int mitigated = Mathf.Max(1, damage - defense);
        currentHP -= mitigated;
        currentHP = Mathf.Max(0, currentHP);
        PlayerAudioManager.Instance?.PlayPlayerHit();
        _iframeTimer = iframeDuration;
        if (_hitEffect != null)
        {
            _hitEffect.TriggerHit(sourcePosition ?? (Vector2)transform.position);
            _hitEffect.StartIframes();
        }
        ParticleManager.Instance.EmitHit(transform.position);
        if (sourcePosition.HasValue && knockbackForce > 0f)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 horizontal = new Vector2(transform.position.x - sourcePosition.Value.x, 0f);
                if (horizontal.sqrMagnitude < 0.001f) horizontal = Vector2.right;
                else horizontal.Normalize();
                Vector2 force = new Vector2(horizontal.x * knockbackForce, knockbackForce);
                rb.linearVelocity = Vector2.zero;
                rb.AddForce(force, ForceMode2D.Impulse);
                GetComponent<PlayerMovement>()?.ApplyKnockback();
            }
        }
        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        if (currentHP <= 0) Die();
    }

    public void Die() => StartCoroutine(Respawn());

    System.Collections.IEnumerator Respawn()
    {
        _hitEffect?.StopIframesKeepHidden();
        SetChildrenActive(false);
        GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<PlayerMovement>().enabled = false;

        if (_respawnContainer != null)
            _respawnContainer.SetActive(true);

        float timer = 3f;
        while (timer > 0f)
        {
            if (_respawnTimerText != null)
                _respawnTimerText.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
            timer -= Time.deltaTime;
        }

        if (_respawnContainer != null)
            _respawnContainer.SetActive(false);

        transform.position = PlayerSpawner.Instance.GetSpawnPosition();
        currentHP = maxHP;
        SetChildrenActive(true);
        GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<PlayerMovement>().enabled = true;
        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void SetChildrenActive(bool active)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(active);

        if (active)
            GetComponent<PlayerAnimation>()?.RefreshArmorCache();
    }
}