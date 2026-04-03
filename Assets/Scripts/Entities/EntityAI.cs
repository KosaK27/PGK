using UnityEngine;

public enum EntityState { Patrol, Chase, Attack, Dead }

[RequireComponent(typeof(EntityStats))]
public abstract class EntityAI : MonoBehaviour
{
    protected EntityStats stats;
    protected Transform player;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;

    public EntityState State { get; protected set; } = EntityState.Patrol;

    protected float distanceToPlayer =>
        player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    protected virtual void Awake()
    {
        stats = GetComponent<EntityStats>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        stats.OnDeath += OnDeath;
    }

    protected virtual void Update()
    {
        if (State == EntityState.Dead) return;
        UpdateState();
        Tick();
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
        if (Mathf.Abs(directionX) > 0.01f && sr != null)
            sr.flipX = directionX < 0;
    }
}