using UnityEngine;

public class WalkingEnemyAI : EntityAI
{
    private enum State { Patrol, Chase, Attack }
    private State _state = State.Patrol;

    private float _patrolTimer;
    private float _patrolDir = 1f;
    private bool _isGrounded;

    private float _attackCooldown;
    private const float ATTACK_CD = 1.2f;

    private float _stuckTimer;
    private Vector2 _lastPosition;
    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private float _stuckCheckTimer;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_JUMP_DELAY = 3f;
    [SerializeField] private float jumpForce = 7f;

    protected override void Start()
    {
        base.Start();
        _lastPosition = transform.position;
    }

    protected override void Tick()
    {
        var d = stats.data;
        float dist = distanceToPlayer;

        if (d.isHostile && dist < d.detectionRange)
            _state = dist < d.attackRange ? State.Attack : State.Chase;
        else
            _state = State.Patrol;

        switch (_state)
        {
            case State.Patrol: DoPatrol(d); break;
            case State.Chase: DoChase(d); break;
            case State.Attack: DoAttack(d); break;
        }

        _attackCooldown -= Time.deltaTime;

        if (_state != State.Attack)
            CheckStuck();
    }

    private void CheckStuck()
    {
        _stuckCheckTimer -= Time.deltaTime;
        if (_stuckCheckTimer > 0f) return;
        _stuckCheckTimer = STUCK_CHECK_INTERVAL;

        float movedDistance = Vector2.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        bool shouldBeMoving = Mathf.Abs(rb.linearVelocity.x) > 0.05f || _patrolDir != 0f;
        if (shouldBeMoving && movedDistance < STUCK_THRESHOLD)
        {
            _stuckTimer += STUCK_CHECK_INTERVAL;
            if (_stuckTimer >= STUCK_JUMP_DELAY && _isGrounded)
            {
                Jump();
                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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

    private void DoAttack(EntityData d)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (_attackCooldown <= 0f)
            _attackCooldown = ATTACK_CD;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        foreach (var c in col.contacts)
            if (c.normal.y > 0.5f) { _isGrounded = true; return; }
        _isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col) => _isGrounded = false;
}