using UnityEngine;

public class FlyingEnemyAI : EntityAI
{
    [Header("Flying")]
    [SerializeField] private float glideDownSpeed = 0.8f;
    [SerializeField] private float chaseAmplitude = 0.6f;
    [SerializeField] private float chaseFrequency = 2f;
    [SerializeField] private float idleFloatAmp = 0.3f;
    [SerializeField] private float idleFloatFreq = 1.2f;

    private float _time;

    protected override void Start()
    {
        base.Start();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    protected override void UpdateState()
    {
        var d = stats.data;
        State = d.isHostile && distanceToPlayer < d.detectionRange
            ? EntityState.Chase
            : EntityState.Patrol;
    }

    protected override void Tick()
    {
        _time += Time.deltaTime;

        if (State == EntityState.Chase)
            ChasePlayer();
        else
            IdleFloat();
    }

    void ChasePlayer()
    {
        if (player == null) return;
        var d = stats.data;

        Vector2 dir = (player.position - transform.position).normalized;
        dir.y -= glideDownSpeed * Time.deltaTime;

        float wave = Mathf.Sin(_time * chaseFrequency) * chaseAmplitude;
        rb.linearVelocity = dir * d.moveSpeed + new Vector2(0, wave);
        FlipTowards(dir.x);
    }

    void IdleFloat()
    {
        float wave = Mathf.Sin(_time * idleFloatFreq) * idleFloatAmp;
        rb.linearVelocity = new Vector2(0, wave);
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        rb.gravityScale = 2f;
    }
}