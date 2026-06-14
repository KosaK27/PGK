using UnityEngine;

public class ShootingEnemyAI : EntityAI
{
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootRange = 14f;
    [SerializeField] private float safeDistance = 6f;

    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float speedMultiplier = 1.4f;

    [Header("Water Settings")]
    [SerializeField] private float swimUpSpeed = 2.5f;
    [SerializeField] private float waterGravityScale = 0.4f;
    [SerializeField] private float waterMoveSpeedMultiplier = 0.6f;
    [SerializeField] private float waterLeapForce = 11f;

    private ProjectileLauncher _launcher;
    private float _patrolTimer;
    private float _patrolDir = 1f;

    private bool _isGrounded;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector2 _lastPosition;
    private float _defaultGravityScale;
    private bool _isInWater;

    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_JUMP_DELAY = 3f;

    protected override void Start()
    {
        base.Start();
        _launcher = GetComponent<ProjectileLauncher>();
        _lastPosition = transform.position;
        _defaultGravityScale = rb.gravityScale;
    }

    protected override void UpdateTargetPriority()
    {
        float range = (stats.data != null ? stats.data.detectionRange : 8f) * 2f;
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, range);

        if (stats.data != null && stats.data.isHostile)
        {
            Transform foundPlayer = null;
            Transform foundNPC = null;
            float closestNPCDist = float.MaxValue;

            foreach (var c in cols)
            {
                if (c.CompareTag("Player"))
                {
                    foundPlayer = c.transform;
                }
                else if (c.CompareTag("NPC"))
                {
                    float d = Vector2.Distance(transform.position, c.transform.position);
                    if (d < closestNPCDist)
                    {
                        closestNPCDist = d;
                        foundNPC = c.transform;
                    }
                }
            }

            if (foundPlayer != null) currentTarget = foundPlayer;
            else if (foundNPC != null) currentTarget = foundNPC;
            else currentTarget = null;
        }
    }

    protected override void UpdateState()
    {
        var d = stats.data;
        float doubleDetection = (d != null ? d.detectionRange : 8f) * 2f;

        if (!d.isHostile || currentTarget == null || distanceToTarget >= doubleDetection)
            State = EntityState.Patrol;
        else if (distanceToTarget < safeDistance)
            State = EntityState.Chase;
        else if (distanceToTarget < shootRange)
            State = EntityState.Attack;
        else
            State = EntityState.Chase;
    }

    protected override void Tick()
    {
        HandleWaterAndSwim();

        switch (State)
        {
            case EntityState.Patrol:
                DoPatrol();
                CheckStuck();
                break;
            case EntityState.Chase:
                DoChase();
                CheckStuck();
                break;
            case EntityState.Attack:
                DoShoot();
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

    void DoPatrol()
    {
        var d = stats.data;
        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
        {
            _patrolDir = Random.value > 0.5f ? 1f : -1f;
            _patrolTimer = d.patrolChangeInterval * 1.3f;
        }
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(_patrolDir * currentSpeed * 0.5f, rb.linearVelocity.y);
        FlipTowards(_patrolDir);
    }

    void DoChase()
    {
        if (currentTarget == null) return;

        var d = stats.data;
        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(dir * currentSpeed, rb.linearVelocity.y);
        FlipTowards(dir);
    }

    void DoShoot()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (currentTarget == null || _launcher == null) return;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir = (currentTarget.position - transform.position).normalized;

        FlipTowards(dir.x);
        _launcher.Shoot(origin, dir);
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
            Vector3Int frontCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(intendedDir * frontDistMultiplier(), 0.2f, 0));
            float frontDistMultiplier() => 0.6f;
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