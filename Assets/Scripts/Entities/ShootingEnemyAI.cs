using UnityEngine;

public class ShootingEnemyAI : EntityAI
{
    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootRange = 7f;
    [SerializeField] private float safeDistance = 3f;

    private ProjectileLauncher _launcher;
    private float _patrolTimer;
    private float _patrolDir = 1f;

    protected override void Start()
    {
        base.Start();
        _launcher = GetComponent<ProjectileLauncher>();
    }

    protected override void UpdateState()
    {
        var d = stats.data;
        float dist = distanceToPlayer;

        if (!d.isHostile || dist >= d.detectionRange)
            State = EntityState.Patrol;
        else if (dist < safeDistance)
            State = EntityState.Chase;
        else if (dist < shootRange)
            State = EntityState.Attack;
        else
            State = EntityState.Chase;
    }

    protected override void Tick()
    {
        switch (State)
        {
            case EntityState.Patrol: DoPatrol(); break;
            case EntityState.Chase: DoChase(); break;
            case EntityState.Attack: DoShoot(); break;
        }
    }

    void DoPatrol()
    {
        var d = stats.data;
        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
        {
            _patrolDir = Random.value > 0.5f ? 1f : -1f;
            _patrolTimer = d.patrolChangeInterval;
        }
        rb.linearVelocity = new Vector2(_patrolDir * d.moveSpeed * 0.5f, rb.linearVelocity.y);
        FlipTowards(_patrolDir);
    }

    void DoChase()
    {
        if (player == null) return;
        var d = stats.data;
        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * d.moveSpeed, rb.linearVelocity.y);
        FlipTowards(dir);
    }

    void DoShoot()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        if (player == null || _launcher == null) return;

        Vector2 origin = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;

        Vector2 dir = (player.position - transform.position).normalized;
        FlipTowards(dir.x);
        _launcher.Shoot(origin, dir);
    }
}