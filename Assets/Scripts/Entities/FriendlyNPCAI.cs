using UnityEngine;

public class FriendlyNPCAI : EntityAI
{
    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float teleportDistance = 30f;
    [SerializeField] private float moveInterval = 3f;
    [SerializeField] private float jumpForce = 7f;

    private Vector2 _homePosition;
    private float _timer;
    private float _moveDir;

    private bool _isGrounded;
    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector2 _lastPosition;

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

    private void DoWander(float distFromHome)
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _moveDir = Random.Range(-1, 2);
            _timer = moveInterval + Random.Range(-1f, 1f);

            if (distFromHome > wanderRadius)
            {
                _moveDir = transform.position.x < _homePosition.x ? 1f : -1f;
            }
        }

        var d = stats.data;
        rb.linearVelocity = new Vector2(_moveDir * d.moveSpeed * 0.4f, rb.linearVelocity.y);
        FlipTowards(-_moveDir);
    }

    private void DoChase()
    {
        if (currentTarget == null) return;
        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * stats.data.moveSpeed, rb.linearVelocity.y);
        FlipTowards(-dir);
    }

    private void DoAttack()
    {
        if (currentTarget == null) return;
        float dir = Mathf.Sign(currentTarget.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * stats.data.moveSpeed, rb.linearVelocity.y);
        FlipTowards(-dir);
    }

    private void CheckStuck()
    {
        _stuckCheckTimer -= Time.deltaTime;
        if (_stuckCheckTimer > 0f) return;
        _stuckCheckTimer = STUCK_CHECK_INTERVAL;

        float moved = Vector2.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        bool shouldMove = Mathf.Abs(rb.linearVelocity.x) > 0.05f || _moveDir != 0f || State == EntityState.Chase;

        if (shouldMove && moved < STUCK_THRESHOLD)
        {
            _stuckTimer += STUCK_CHECK_INTERVAL;
            if (_stuckTimer >= STUCK_JUMP_DELAY && _isGrounded)
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
        foreach (var c in col.contacts)
            if (c.normal.y > 0.5f) { _isGrounded = true; return; }
        _isGrounded = false;
    }

    void OnCollisionExit2D(Collision2D col) => _isGrounded = false;
}