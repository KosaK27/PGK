using System.Collections.Generic;
using UnityEngine;

public class GravityBlockSystem : MonoBehaviour
{
    public static GravityBlockSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float checkInterval = 0.1f;

    private float _timer;
    private HashSet<Vector2Int> _toCheck = new();
    private List<FallingBlock> _falling = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = checkInterval;
            ProcessPending();
        }
    }

    public void NotifyNeighbors(int worldX, int worldY)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = 0; dy <= 1; dy++)
                _toCheck.Add(new Vector2Int(worldX + dx, worldY + dy));
    }

    public void Schedule(int worldX, int worldY)
    {
        _toCheck.Add(new Vector2Int(worldX, worldY));
    }

    void ProcessPending()
    {
        var snapshot = new List<Vector2Int>(_toCheck);
        _toCheck.Clear();

        foreach (var pos in snapshot)
        {
            var block = WorldManager.Instance.GetBlock(pos.x, pos.y);
            if (!IsGravityBlock(block)) continue;

            var below = WorldManager.Instance.GetBlock(pos.x, pos.y - 1);
            if (below != BlockType.Air) continue;

            WorldManager.Instance.DestroyBlock(pos.x, pos.y);
            SpawnFallingBlock(pos.x, pos.y, block);
        }
    }

    void SpawnFallingBlock(int worldX, int worldY, BlockType type)
    {
        var go = new GameObject($"FallingBlock_{type}");
        var fb = go.AddComponent<FallingBlock>();
        var rb = go.AddComponent<Rigidbody2D>();
        var col = go.AddComponent<BoxCollider2D>();
        var sr = go.AddComponent<SpriteRenderer>();

        go.transform.position = new Vector3(worldX + 0.5f, worldY + 0.5f, 0);

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        col.size = Vector2.one * 0.9f;
        sr.sortingOrder = 50;
        sr.color = GetFallbackColor(type);

        fb.Init(type);
        _falling.Add(fb);
    }

    Color GetFallbackColor(BlockType type) => type switch
    {
        BlockType.Sand => new Color(0.87f, 0.78f, 0.45f),
        _ => Color.white
    };

    public bool IsGravityBlock(BlockType type) => type == BlockType.Sand;

    public void RemoveFalling(FallingBlock fb) => _falling.Remove(fb);
}