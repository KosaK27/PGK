using UnityEngine;

public class FriendlyNPCAI : EntityAI
{
    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 35f;
    [SerializeField] private float teleportDistance = 70f;
    [SerializeField] private float moveInterval = 1.5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float speedMultiplier = 1.8f;

    [Header("Water Settings")]
    [SerializeField] private float swimUpSpeed = 4f;
    [SerializeField] private float waterGravityScale = 0.4f;
    [SerializeField] private float waterMoveSpeedMultiplier = 0.8f;
    [SerializeField] private float waterLeapForce = 4.5f;

    private Vector2 _homePosition;
    private float _timer;
    private float _moveDir;
    private bool _isGrounded;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector2 _lastPosition;
    private float _defaultGravityScale;
    private bool _isInWater;

    private const float STUCK_CHECK_INTERVAL = 0.5f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_JUMP_DELAY = 3f;

    public void SetHome(Vector2 homePos)
    {
        _homePosition = homePos;
    }

    protected override void Start()
    {
        base.Start();
        State = EntityState.Patrol;
        _lastPosition = transform.position;
        _defaultGravityScale = rb.gravityScale;
    }

    protected override void UpdateState()
    {
        if (currentTarget != null)
        {
            if (distanceToTarget <= stats.data.attackRange)
                State = EntityState.Attack;
            else
                State = EntityState.Chase;
        }
        else
        {
            State = EntityState.Patrol;
        }
    }

    protected override void Tick()
    {
        HandleWaterAndSwim();

        float distFromHome = Vector2.Distance(transform.position, _homePosition);
        if (distFromHome > teleportDistance && !ChunkManager.Instance.IsChunkLoaded(transform.position))
        {
            transform.position = _homePosition;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        switch (State)
        {
            case EntityState.Patrol:
                DoWander(distFromHome);
                CheckStuck();
                break;
            case EntityState.Chase:
                DoChase();
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
        Vector3Int bodyCell = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 0.5f, 0));

        BlockType feetBlock = WorldManager.Instance.GetBlock(feetCell.x, feetCell.y);
        BlockType bodyBlock = WorldManager.Instance.GetBlock(bodyCell.x, bodyCell.y);

        _isInWater = (feetBlock == BlockType.Water || bodyBlock == BlockType.Water);

        if (_isInWater)
        {
            rb.gravityScale = waterGravityScale;
            Vector3Int cellAbove = WorldManager.Instance.WorldToCell(transform.position + new Vector3(0, 1.0f, 0));
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

    private void DoWander(float distFromHome)
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _moveDir = Random.value > 0.1f ? (Random.value > 0.5f ? 1f : -1f) : 0f;
            _timer = moveInterval + Random.Range(-0.3f, 0.3f);
            if (distFromHome > wanderRadius)
            {
                _moveDir = transform.position.x < _homePosition.x ? 1f : -1f;
            }
        }

        var d = stats.data;
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(_moveDir * currentSpeed * 0.7f, rb.linearVelocity.y);
        FlipTowards(-_moveDir);
    }

    private void DoChase()
    {
        if (currentTarget == null) return;

        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        var d = stats.data;
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(dir * currentSpeed, rb.linearVelocity.y);
        FlipTowards(-dir);
    }

    private void DoAttack()
    {
        if (currentTarget == null) return;

        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        var d = stats.data;
        float currentSpeed = _isInWater ? d.moveSpeed * speedMultiplier * waterMoveSpeedMultiplier : d.moveSpeed * speedMultiplier;
        rb.linearVelocity = new Vector2(dir * currentSpeed, rb.linearVelocity.y);
        FlipTowards(-dir);
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
        if (State == EntityState.Patrol) intendedDir = _moveDir;
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