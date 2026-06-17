using System.Collections;
using UnityEngine;

public class Boss1AI : EntityAI
{
    [Header("Boss Settings")]
    [SerializeField] private float orbitHeight = 3f;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float minAttackCooldown = 3f;
    [SerializeField] private float maxAttackCooldown = 5f;

    [Header("Contact Damage")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactDamageCooldown = 1f;

    [Header("Attacks")]
    [SerializeField] private BossAttack_Charge chargeAttack;
    [SerializeField] private BossAttack_Arc arcAttack;
    [SerializeField] private BossAttack_Tornado tornadoAttack;

    private BossHealthBar _healthBar;
    private float _attackTimer;
    private bool _isAttacking;
    private float _floatTime;
    private int _lastAttack = -1;
    private int _lastAttackCount = 0;
    private int _totalAttackCount = 0;
    private float _contactDamageTimer;

    protected override void Start()
    {
        base.Start();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _attackTimer = Random.Range(minAttackCooldown, maxAttackCooldown);

        _healthBar = FindAnyObjectByType<BossHealthBar>();
        if (_healthBar != null)
            _healthBar.Initialize(stats);

        stats.OnDeath += OnBossDeath;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var playerStats = playerObj.GetComponent<PlayerStats>();
            if (playerStats != null)
                playerStats.OnHealthChanged += OnPlayerHealthChanged;
        }
    }

    void OnDestroy()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var playerStats = playerObj.GetComponent<PlayerStats>();
            if (playerStats != null)
                playerStats.OnHealthChanged -= OnPlayerHealthChanged;
        }

        if (_healthBar != null)
            _healthBar.Hide();
    }

    protected override void UpdateState()
    {
        if (_isAttacking) return;
        State = EntityState.Chase;
    }

    protected override void Tick()
    {
        if (_isAttacking) return;

        _floatTime += Time.deltaTime;
        OrbitPlayer();

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
            StartCoroutine(DoAttack());

        if (_contactDamageTimer > 0f)
            _contactDamageTimer -= Time.deltaTime;
    }

    private void OrbitPlayer()
    {
        if (player == null) return;

        float floatOffset = Mathf.Sin(_floatTime * 1.5f) * 0.4f;
        Vector2 orbitTarget = new Vector2(
            player.position.x,
            player.position.y + orbitHeight + floatOffset);

        Vector2 dir = (orbitTarget - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, orbitTarget);
        float speed = Mathf.Min(moveSpeed, dist / Time.deltaTime);
        rb.linearVelocity = dir * speed;

        FlipTowards(player.position.x - transform.position.x);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (_contactDamageTimer > 0f) return;
        var ps = other.GetComponent<PlayerStats>();
        if (ps == null) return;
        ps.TakeDamage(contactDamage, transform.position);
        _contactDamageTimer = contactDamageCooldown;
    }

    private int PickAttack()
    {
        _totalAttackCount++;

        if (_totalAttackCount % 4 == 0)
            return 2;

        int attack;
        int maxTries = 10;

        do
        {
            attack = Random.Range(0, 2);
            maxTries--;
        }
        while (attack == _lastAttack && _lastAttackCount >= 2 && maxTries > 0);

        if (attack == _lastAttack)
            _lastAttackCount++;
        else
        {
            _lastAttack = attack;
            _lastAttackCount = 1;
        }

        return attack;
    }

    private IEnumerator DoAttack()
    {
        _isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        int attack = PickAttack();

        if (attack == 0 && chargeAttack != null && player != null)
            yield return StartCoroutine(chargeAttack.Execute(transform, player, rb));
        else if (attack == 1 && arcAttack != null && player != null)
            yield return StartCoroutine(arcAttack.Execute(transform, player, rb));
        else if (attack == 2 && tornadoAttack != null && player != null)
            yield return StartCoroutine(tornadoAttack.Execute(transform, player, rb));

        _attackTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
        _isAttacking = false;
    }

    private void OnBossDeath()
    {
        if (_healthBar != null)
            _healthBar.Hide();
        StopAllCoroutines();
    }

    private void OnPlayerHealthChanged(int current, int max)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        if (current <= 0)
        {
            StopAllCoroutines();
            _isAttacking = false;
            rb.linearVelocity = Vector2.zero;
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        rb.gravityScale = 0f;
    }
}