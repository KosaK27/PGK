using UnityEngine;

public enum EntityState { Patrol, Chase, Attack, Dead }

[RequireComponent(typeof(EntityStats))]
public abstract class EntityAI : MonoBehaviour
{
    protected EntityStats stats;
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Transform currentTarget;

    public EntityState State { get; protected set; } = EntityState.Patrol;
    protected float knockbackTimer;
    private const float KNOCKBACK_DURATION = 0.3f;

    protected float distanceToPlayer =>
        player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    protected float distanceToTarget =>
        currentTarget != null ? Vector2.Distance(transform.position, currentTarget.position) : float.MaxValue;

    protected virtual void Awake()
    {
        stats = GetComponent<EntityStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        stats.OnDeath += OnDeath;
    }

    protected virtual void Update()
    {
        if (State == EntityState.Dead) return;
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            return;
        }
        UpdateTargetPriority();
        UpdateState();
        Tick();
    }

    protected virtual void UpdateTargetPriority()
    {
        float range = stats.data != null ? stats.data.detectionRange : 8f;
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
        else
        {
            Transform foundEnemy = null;
            float closestEnemyDist = float.MaxValue;

            foreach (var c in cols)
            {
                if (c.CompareTag("Enemy"))
                {
                    float d = Vector2.Distance(transform.position, c.transform.position);
                    if (d < closestEnemyDist)
                    {
                        closestEnemyDist = d;
                        foundEnemy = c.transform;
                    }
                }
            }
            currentTarget = foundEnemy;
        }
    }

    public void ApplyKnockback()
    {
        knockbackTimer = KNOCKBACK_DURATION;
    }

    protected abstract void UpdateState();
    protected abstract void Tick();

    protected virtual void OnDeath()
    {
        State = EntityState.Dead;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    protected void FlipTowards(float directionX)
    {
        if (Mathf.Abs(directionX) > 0.01f)
        {
            if (directionX > 0f)
                transform.localScale = new Vector3(1f, 1f, 1f);
            else
                transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }
}