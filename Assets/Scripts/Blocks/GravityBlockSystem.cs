using System.Collections.Generic;
using UnityEngine;

public class GravityBlockSystem : MonoBehaviour
{
    public static GravityBlockSystem Instance { get; private set; }

    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private BlockSpriteMap blockSpriteMap;

    private float _timer;
    private readonly HashSet<Vector2Int> _toCheck = new();
    private readonly List<FallingBlock> _fallingBlocks = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = checkInterval;
        ProcessPending();
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

    public bool IsGravityBlock(BlockType type) => type == BlockType.Sand;

    public void RemoveFalling(FallingBlock fallingBlock) => _fallingBlocks.Remove(fallingBlock);

    public void CaptureToSave(WorldSaveData save)
    {
        save.fallingBlocks.Clear();
        foreach (var fallingBlock in _fallingBlocks)
        {
            if (fallingBlock == null) continue;
            save.fallingBlocks.Add(new FallingBlockSave
            {
                blockType = (int)fallingBlock.BlockType,
                x = fallingBlock.transform.position.x,
                y = fallingBlock.transform.position.y
            });
        }
    }

    public void RestoreFromSave(WorldSaveData save)
    {
        if (save.fallingBlocks == null) return;
        foreach (var saved in save.fallingBlocks)
            SpawnFallingBlock(saved.x, saved.y, (BlockType)saved.blockType);
    }

    void ProcessPending()
    {
        var snapshot = new List<Vector2Int>(_toCheck);
        _toCheck.Clear();

        foreach (var pos in snapshot)
        {
            var block = WorldManager.Instance.GetBlock(pos.x, pos.y);
            if (!IsGravityBlock(block)) continue;

            if (MultitileObjectSystem.Instance.IsSupporting(pos))
                continue;

            var below = WorldManager.Instance.GetBlock(pos.x, pos.y - 1);
            if (below != BlockType.Air) continue;

            WorldManager.Instance.DestroyBlock(pos.x, pos.y);
            SpawnFallingBlock(pos.x + 0.5f, pos.y + 0.5f, block);
        }
    }

    void SpawnFallingBlock(float worldX, float worldY, BlockType type)
    {
        var go = new GameObject($"FallingBlock_{type}");
        go.transform.position = new Vector3(worldX, worldY, 0f);
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<SpriteRenderer>();
        var fallingBlock = go.AddComponent<FallingBlock>();
        fallingBlock.Init(type, blockSpriteMap);
        _fallingBlocks.Add(fallingBlock);
    }
}