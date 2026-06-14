using UnityEngine;

public class WalkingEnemyAI : EntityAI
{
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float speedMultiplier = 1.4f;

    [Header("Water Settings")]
    [SerializeField] private float swimUpSpeed = 2.5f;
    [SerializeField] private float waterGravityScale = 0.4f;
    [SerializeField] private float waterMoveSpeedMultiplier = 0.6f;
    [SerializeField] private float waterLeapForce = 11f;

    private float _patrolTimer;
    private float _patrolDir = 1f;
    private bool _isGrounded;
    private float _attackCooldown;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector2 _lastPosition;
    private float _defaultGravityScale;
    private bool _isInWater;

    private const float ATTACK_CD = 1.2f;
    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_JUMP_DELAY = 3f;

    protected override void Start()
    {
        base.Start();
        _lastPosition = transform.position;
        _defaultGravityScale = rb.gravityScale;
    }

    protected override void UpdateState()
    {
        var d = stats.data;
        if (d.isHostile && currentTarget != null && distanceToTarget < d.detectionRange)
        {
            State = distanceToTarget < d.attackRange ? EntityState.Attack : EntityState.Chase;
        }
        else
        {
            State = EntityState.Patrol;
        }
    }

    protected override void Tick()
    {
        HandleWaterAndSwim();

        _attackCooldown -= Time.deltaTime;
        var d = stats.data;

        switch (State)
        {
            case EntityState.Patrol:
                DoPatrol(d);
                CheckStuck();
                break;
            case EntityState.Chase:
                DoChase(d);
                CheckStuck();
                break;
            case EntityState.Attack:
                DoAttack();
                break;
        }
    }

    private void HandleWaterAndSwim()
    {
        Vector3Int feetCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 0.1f, 0));
        Vector3Int bodyCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 0.8f, 0));

        BlockType feetBlock = WorldManager.Instance.GetBlock(feetCell.x, feetCell.y);
        BlockType bodyBlock = WorldManager.Instance.GetBlock(bodyCell.x, bodyCell.y);

        _isInWater = (feetBlock == BlockType.Water || bodyBlock == BlockType.Water);

        if (_isInWater)
        {
            rb.gravityScale = waterGravityScale;
            Vector3Int cellAbove = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 1.5f, 0));
            BlockType blockAbove = WorldManager.Instance.GetBlock(cellAbove.x, cellAbove.y);

            if (blockAbove == BlockType.Air)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, waterLeapForce);
                _isInWater = false;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimUpSpeed);
            }
        }
        else
        {
            rb.gravityScale = _defaultGravityScale;
        }
    }

    private void DoPatrol(EntityData d)
    {
        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
        {
            _patrolDir = Random.value > 0.5f ? 1f : -1f;
            _patrolTimer = d.patrolChangeInterval * 1.3f;
        }

        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(_patrolDir * currentSpeed * 0.8f, rb.linearVelocity.y);
        FlipTowards(_patrolDir);
    }

    private void DoChase(EntityData d)
    {
        if (currentTarget == null) return;

        float distanceX = Mathf.Abs(currentTarget.position.x - transform.position.x);
        if (distanceX < d.attackRange * 0.7f)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(dir * currentSpeed, rb.linearVelocity.y);
        FlipTowards(dir);
    }

    private void DoAttack()
    {
        if (currentTarget != null)
        {
            float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
            float distanceX = Mathf.Abs(currentTarget.position.x - transform.position.x);

            if (distanceX < 0.6f)
            {
                var d = stats.data;
                float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
                rb.linearVelocity = new Vector2(-dir * currentSpeed * 0.4f, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            FlipTowards(dir);
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        if (_attackCooldown <= 0f) _attackCooldown = ATTACK_CD;
    }

    private void CheckStuck()
    {
        if (_isInWater) return;

        _stuckCheckTimer -= Time.deltaTime;
        if (_stuckCheckTimer > 0f) return;
        _stuckCheckTimer = STUCK_CHECK_INTERVAL;

        float moved = Vector2.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        float intendedDir = 0f;
        if (State == EntityState.Patrol) intendedDir = _patrolDir;
        else if (State == EntityState.Chase && currentTarget != null) intendedDir = Mathf.Sign(currentTarget.position.x - transform.position.x);

        bool isTryingToMove = intendedDir != 0f;

        if (isTryingToMove && moved < STUCK_THRESHOLD)
        {
            Vector3Int frontCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(intendedDir * 0.6f, 0.2f, 0));
            BlockType blockInFront = WorldManager.Instance.GetBlock(frontCell.x, frontCell.y);
            bool hittingSolidBlock = (blockInFront != BlockType.Air && blockInFront != BlockType.Water);

            _stuckTimer += STUCK_CHECK_INTERVAL;

            float requiredDelay = hittingSolidBlock ? 0.5f : STUCK_JUMP_DELAY;

            if (_stuckTimer >= requiredDelay && _isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                _stuckTimer = 0f;
            }
        }
        else
        {
            _stuckTimer = 0f;
        }
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (_isInWater) return;
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f) { _isGrounded = true; return; }
        }
        _isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col) => _isGrounded = false;
}