using UnityEngine;

public class FriendlyNPCAI : EntityAI
{
    [Header("Wandering")]
    [SerializeField] private float wanderRadius = 15f;
    [SerializeField] private float teleportDistance = 30f;
    [SerializeField] private float moveInterval = 3f;

    private Vector2 _homePosition;
    private float _timer;
    private float _moveDir;

    public void SetHome(Vector2 homePos)
    {
        _homePosition = homePos;
    }

    protected override void Start()
    {
        base.Start();
        State = EntityState.Patrol;
    }

    protected override void UpdateState()
    {
        State = EntityState.Patrol;
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

        DoWander(distFromHome);
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
}