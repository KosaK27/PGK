using UnityEngine;

public class WalkingEnemyAI : EntityAI
{
    [SerializeField] private float jumpForce = 7f;

    private float _patrolTimer;
    private float _patrolDir = 1f;
    private bool _isGrounded;
    private float _attackCooldown;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector2 _lastPosition;

    private const float ATTACK_CD = 1.2f;
    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_JUMP_DELAY = 3f;

    protected override void Start()
    {
        base.Start();
        _lastPosition = transform.position;
    }

    protected override void UpdateState()
    {
        var d = stats.data;
        float dist = distanceToPlayer;

        if (d.isHostile && dist < d.detectionRange)
            State = dist < d.attackRange ? EntityState.Attack : EntityState.Chase;
        else
            State = EntityState.Patrol;
    }

    protected override void Tick()
    {
        var d = stats.data;
        _attackCooldown -= Time.deltaTime;

        switch (State)
        {
            case EntityState.Patrol: DoPatrol(d); CheckStuck(); break;
            case EntityState.Chase: DoChase(d); CheckStuck(); break;
            case EntityState.Attack: DoAttack(); break;
        }
    }

    private void DoPatrol(EntityData d)
    {
        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
        {
            _patrolDir = Random.value > 0.5f ? 1f : -1f;
            if (Random.value < 0.3f) _patrolDir = 0f;
            _patrolTimer = d.patrolChangeInterval;
        }
        rb.linearVelocity = new Vector2(_patrolDir * d.moveSpeed * 0.5f, rb.linearVelocity.y);
        FlipTowards(_patrolDir);
    }

    private void DoChase(EntityData d)
    {
        if (player == null) return;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * d.moveSpeed, rb.linearVelocity.y);
        FlipTowards(dir);
    }

    private void DoAttack()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (_attackCooldown <= 0f) _attackCooldown = ATTACK_CD;
    }

    private void CheckStuck()
    {
        _stuckCheckTimer -= Time.deltaTime;
        if (_stuckCheckTimer > 0f) return;
        _stuckCheckTimer = STUCK_CHECK_INTERVAL;

        float moved = Vector2.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        bool shouldMove = Mathf.Abs(rb.linearVelocity.x) > 0.05f || _patrolDir != 0f;
        if (shouldMove && moved < STUCK_THRESHOLD)
        {
            _stuckTimer += STUCK_CHECK_INTERVAL;
            if (_stuckTimer >= STUCK_JUMP_DELAY && _isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                _stuckTimer = 0f;
            }
        }
        else _stuckTimer = 0f;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        foreach (var c in col.contacts)
            if (c.normal.y > 0.5f) { _isGrounded = true; return; }
        _isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col) => _isGrounded = false;
}