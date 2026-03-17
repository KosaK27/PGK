using UnityEngine;
using UnityEngine.Tilemaps;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;

    public Vector2Int ChunkPos { get; private set; }

    private Tilemap _tilemap;
    private TilemapCollider2D _tilemapCollider;
    private CompositeCollider2D _compositeCollider;
    private Rigidbody2D _rb;
    private BlockRegistry _blockRegistry;
    private bool _dirty = false;

    public void Initialize(Vector2Int chunkPos, BlockRegistry blockRegistry, Transform parent)
    {
        ChunkPos = chunkPos;
        _blockRegistry = blockRegistry;
        transform.SetParent(parent);

        _tilemap = gameObject.AddComponent<Tilemap>();
        
        var renderer = gameObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -999;

        _tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
        _tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Static;

        _compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
        _compositeCollider.generationType = CompositeCollider2D.GenerationType.Synchronous;
    }

    public void RenderAll(WorldData world, int offsetX, int offsetY)
    {
        int startX = ChunkPos.x * SIZE;
        int startY = ChunkPos.y * SIZE;

        var positions = new Vector3Int[SIZE * SIZE];
        var tiles = new TileBase[SIZE * SIZE];

        for (int ly = 0; ly < SIZE; ly++)
        for (int lx = 0; lx < SIZE; lx++)
        {
            int wx = startX + lx;
            int wy = startY + ly;
            int i = ly * SIZE + lx;

            positions[i] = new Vector3Int(wx + offsetX, wy + offsetY, 0);

            var block = world.GetBlock(wx, wy);
            var data = _blockRegistry.Get(block);
            tiles[i] = data?.tile;
        }

        _tilemap.SetTiles(positions, tiles);
        _dirty = false;
    }

    public void RefreshTile(int wx, int wy, int offsetX, int offsetY, TileBase tile)
    {
        var cell = new Vector3Int(wx + offsetX, wy + offsetY, 0);
        _tilemap.SetTile(cell, tile);
        _dirty = false;
    }

    public void FlushIfDirty()
    {
        if (!_dirty) return;
        _dirty = false;
    }

    public void Clear()
    {
        _tilemap.ClearAllTiles();
    }
}