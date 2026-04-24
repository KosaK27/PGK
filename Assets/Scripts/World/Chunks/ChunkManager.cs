using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private Transform chunkParent;
    [SerializeField] private int viewDistanceX = 4;
    [SerializeField] private int viewDistanceY = 3;
    [SerializeField] private WallRegistry wallRegistry;

    private Dictionary<Vector2Int, Chunk> _loadedChunks = new();
    private Transform _playerTransform;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void LateUpdate()
    {
        if (_playerTransform == null) return;
        UpdateChunks();
        FlushDirtyChunks();
    }

    public void SetPlayer(Transform player)
    {
        _playerTransform = player;
        UpdateChunks();
        FlushDirtyChunks();
    }

    public void RefreshBlock(int lx, int ly, int offsetX, int offsetY, TileBase tile, bool isSolid = true)
    {
        var chunkPos = GetChunkPos(lx, ly);
        if (_loadedChunks.TryGetValue(chunkPos, out var chunk))
            chunk.RefreshTile(lx, ly, offsetX, offsetY, tile, isSolid);
    }

    private void UpdateChunks()
    {
        var world = WorldManager.Instance;
        var playerChunk = WorldToChunkPos(_playerTransform.position, world.OffsetX, world.OffsetY);

        var toLoad = new HashSet<Vector2Int>();

        for (int cy = playerChunk.y - viewDistanceY; cy <= playerChunk.y + viewDistanceY; cy++)
        for (int cx = playerChunk.x - viewDistanceX; cx <= playerChunk.x + viewDistanceX; cx++)
        {
            var cp = new Vector2Int(cx, cy);
            if (!IsValidChunk(cp, world)) continue;
            toLoad.Add(cp);
            if (!_loadedChunks.ContainsKey(cp))
                LoadChunk(cp, world);
        }

        var toUnload = new List<Vector2Int>();
        foreach (var cp in _loadedChunks.Keys)
            if (!toLoad.Contains(cp))
                toUnload.Add(cp);

        foreach (var cp in toUnload)
            UnloadChunk(cp);
    }

    private void LoadChunk(Vector2Int chunkPos, WorldManager world)
    {
        var go = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        var chunk = go.AddComponent<Chunk>();
        chunk.Initialize(chunkPos, blockRegistry, wallRegistry, chunkParent);
        chunk.RenderAll(world.Data, world.OffsetX, world.OffsetY);
        _loadedChunks[chunkPos] = chunk;
    }

    private void UnloadChunk(Vector2Int chunkPos)
    {
        if (_loadedChunks.TryGetValue(chunkPos, out var chunk))
        {
            chunk.Clear();
            Destroy(chunk.gameObject);
            _loadedChunks.Remove(chunkPos);
        }
    }

    private void FlushDirtyChunks()
    {
        foreach (var chunk in _loadedChunks.Values)
            chunk.FlushIfDirty();
    }

    private Vector2Int GetChunkPos(int lx, int ly)
        => new Vector2Int(lx / Chunk.SIZE, ly / Chunk.SIZE);

    private Vector2Int WorldToChunkPos(Vector3 worldPos, int offsetX, int offsetY)
    {
        int lx = Mathf.FloorToInt(worldPos.x) - offsetX;
        int ly = Mathf.FloorToInt(worldPos.y) - offsetY;
        return new Vector2Int(
            Mathf.FloorToInt((float)lx / Chunk.SIZE),
            Mathf.FloorToInt((float)ly / Chunk.SIZE)
        );
    }

    private bool IsValidChunk(Vector2Int cp, WorldManager world)
    {
        int maxX = world.Data.Width / Chunk.SIZE;
        int maxY = world.Data.Height / Chunk.SIZE;
        return cp.x >= 0 && cp.x < maxX && cp.y >= 0 && cp.y < maxY;
    }

    public bool IsChunkLoaded(Vector2 worldPos)
    {
        var world = WorldManager.Instance;
        int lx = Mathf.FloorToInt(worldPos.x) - world.OffsetX;
        int ly = Mathf.FloorToInt(worldPos.y) - world.OffsetY;
        var chunkPos = GetChunkPos(lx, ly);
        return _loadedChunks.ContainsKey(chunkPos);
    }

    public void RefreshWall(int lx, int ly, int offsetX, int offsetY, TileBase tile)
    {
        var chunkPos = GetChunkPos(lx, ly);
        if (_loadedChunks.TryGetValue(chunkPos, out var chunk))
            chunk.RefreshWallTile(lx, ly, offsetX, offsetY, tile);
    }

    public void RebuildAll(int offsetX, int offsetY)
    {
        var world = WorldManager.Instance;
        if (world == null) return;

        foreach (var chunk in _loadedChunks.Values)
            chunk.RenderAll(world.Data, offsetX, offsetY);
    }

    void OnDrawGizmos()
    {
        if (_loadedChunks == null) return;

        Gizmos.color = Color.green;
        foreach (var kvp in _loadedChunks)
        {
            var world = WorldManager.Instance;
            if (world == null) return;

            float x = kvp.Key.x * Chunk.SIZE + world.OffsetX;
            float y = kvp.Key.y * Chunk.SIZE + world.OffsetY;
            float size = Chunk.SIZE;

            Gizmos.DrawWireCube(
                new Vector3(x + size / 2f, y + size / 2f, 0),
                new Vector3(size, size, 0)
            );
        }
    }
}