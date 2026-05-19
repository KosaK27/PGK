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

    public void RefreshBlock(int lx, int ly, int offsetX, int offsetY, BlockData data)
    {
        var chunkPos = GetChunkPos(lx, ly);
        if (_loadedChunks.TryGetValue(chunkPos, out var chunk))
            chunk.RefreshTile(lx, ly, offsetX, offsetY, data);
    }

    private void UpdateChunks()
    {
        var world = WorldManager.Instance;
        var playerChunk = WorldToChunkPos(_playerTransform.position, world.OffsetX, world.OffsetY);
        var toLoad = new HashSet<Vector2Int>();

        int minCX = playerChunk.x - viewDistanceX;
        int maxCX = playerChunk.x + viewDistanceX;
        int minCY = playerChunk.y - viewDistanceY;
        int maxCY = playerChunk.y + viewDistanceY;

        for (int cy = minCY; cy <= maxCY; cy++)
        for (int cx = minCX; cx <= maxCX; cx++)
        {
            var cp = new Vector2Int(cx, cy);
            if (!IsValidChunk(cp, world)) continue;
            toLoad.Add(cp);

            if (SaveManager.Instance != null)
                SaveManager.Instance.DiscoverChunk(cp.x, cp.y);

            if (!_loadedChunks.ContainsKey(cp))
                LoadChunk(cp, world);
        }

        var toUnload = new List<Vector2Int>();
        foreach (var cp in _loadedChunks.Keys)
            if (!toLoad.Contains(cp))
                toUnload.Add(cp);
        foreach (var cp in toUnload)
            UnloadChunk(cp);

        UpdateLightingWindow(world, minCX, maxCX, minCY, maxCY);
    }

    private void UpdateLightingWindow(WorldManager world, int minCX, int maxCX, int minCY, int maxCY)
    {
        if (LightingSystem.Instance == null) return;

        int clampedMinCX = Mathf.Max(0, minCX);
        int clampedMinCY = Mathf.Max(0, minCY);
        int clampedMaxCX = Mathf.Min(world.Data.Width / Chunk.SIZE - 1, maxCX);
        int clampedMaxCY = Mathf.Min(world.Data.Height / Chunk.SIZE - 1, maxCY);

        int originWorldX = clampedMinCX * Chunk.SIZE + world.OffsetX;
        int originWorldY = clampedMinCY * Chunk.SIZE + world.OffsetY;
        int widthTiles = (clampedMaxCX - clampedMinCX + 1) * Chunk.SIZE;
        int heightTiles = (clampedMaxCY - clampedMinCY + 1) * Chunk.SIZE;

        LightingSystem.Instance.SetWindow(originWorldX, originWorldY, widthTiles, heightTiles);
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
    {
        return new Vector2Int(
            Mathf.FloorToInt((float)lx / Chunk.SIZE),
            Mathf.FloorToInt((float)ly / Chunk.SIZE)
        );
    }

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
        return _loadedChunks.ContainsKey(GetChunkPos(lx, ly));
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
            Gizmos.DrawWireCube(
                new Vector3(x + Chunk.SIZE / 2f, y + Chunk.SIZE / 2f, 0),
                new Vector3(Chunk.SIZE, Chunk.SIZE, 0)
            );
        }
    }
}