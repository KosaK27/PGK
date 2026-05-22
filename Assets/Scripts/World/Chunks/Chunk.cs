using UnityEngine;
using UnityEngine.Tilemaps;

public class Chunk : MonoBehaviour
{
    public const int SIZE = 16;
    public Vector2Int ChunkPos { get; private set; }

    private Tilemap _tilemap;
    private Tilemap _wallTilemap;
    private Tilemap _nonSolidTilemap;
    private Tilemap _liquidTilemap;
    private TilemapCollider2D _tilemapCollider;
    private CompositeCollider2D _compositeCollider;
    private Rigidbody2D _rb;
    private BlockRegistry _blockRegistry;
    private WallRegistry _wallRegistry;

    private TilemapRenderer _solidRenderer;
    private TilemapRenderer _wallRenderer;
    private TilemapRenderer _nonSolidRenderer;
    private TilemapRenderer _liquidRenderer;

    public void Initialize(Vector2Int chunkPos, BlockRegistry blockRegistry, WallRegistry wallRegistry, Transform parent)
    {
        gameObject.layer = LayerMask.NameToLayer("ChunkColliders");
        ChunkPos = chunkPos;
        _blockRegistry = blockRegistry;
        _wallRegistry = wallRegistry;
        transform.SetParent(parent);

        var wallGo = new GameObject("WallTilemap");
        wallGo.transform.SetParent(transform);
        _wallTilemap = wallGo.AddComponent<Tilemap>();
        _wallRenderer = wallGo.AddComponent<TilemapRenderer>();
        _wallRenderer.sortingOrder = -50;

        var nonSolidGo = new GameObject("NonSolidTilemap");
        nonSolidGo.transform.SetParent(transform);
        _nonSolidTilemap = nonSolidGo.AddComponent<Tilemap>();
        _nonSolidRenderer = nonSolidGo.AddComponent<TilemapRenderer>();
        _nonSolidRenderer.sortingOrder = -1;

        var liquidGo = new GameObject("LiquidTilemap");
        liquidGo.transform.SetParent(transform);
        _liquidTilemap = liquidGo.AddComponent<Tilemap>();
        _liquidRenderer = liquidGo.AddComponent<TilemapRenderer>();
        _liquidRenderer.sortingOrder = 50;

        _tilemap = gameObject.AddComponent<Tilemap>();
        _solidRenderer = gameObject.AddComponent<TilemapRenderer>();
        _solidRenderer.sortingOrder = 100;

        _tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
        _tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Static;
        _compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
        _compositeCollider.generationType = CompositeCollider2D.GenerationType.Synchronous;

        RegisterRenderers();
    }

    private void RegisterRenderers()
    {
        var ctrl = LightingMaterialController.Instance;
        if (ctrl == null) return;
        ctrl.RegisterRenderer(_solidRenderer);
        ctrl.RegisterRenderer(_wallRenderer);
        ctrl.RegisterRenderer(_nonSolidRenderer);
        ctrl.RegisterRenderer(_liquidRenderer);
    }

    private void UnregisterRenderers()
    {
        var ctrl = LightingMaterialController.Instance;
        if (ctrl == null) return;
        ctrl.UnregisterRenderer(_solidRenderer);
        ctrl.UnregisterRenderer(_wallRenderer);
        ctrl.UnregisterRenderer(_nonSolidRenderer);
        ctrl.UnregisterRenderer(_liquidRenderer);
    }

    private TileBase ResolveTile(int wx, int wy, BlockData data)
    {
        if (data.connectedTile != null)
            return WorldManager.Instance.GetConnectedTileBase(wx, wy, data.connectedTile);
        return data.tile;
    }

    private TileBase ResolveLiquidTile(WorldData world, int lx, int ly)
    {
        byte level = world.GetLiquid(lx, ly);
        if (level == 0) return null;
        bool belowEmpty = !world.InBounds(lx, ly - 1) || world.GetBlock(lx, ly - 1) == BlockType.Air && world.GetLiquid(lx, ly - 1) == 0;
        return LiquidTileSet.Instance?.Resolve(level, belowEmpty);
    }

    public void RenderAll(WorldData world, int offsetX, int offsetY)
    {
        int startX = ChunkPos.x * SIZE;
        int startY = ChunkPos.y * SIZE;
        var positions = new Vector3Int[SIZE * SIZE];
        var solidTiles = new TileBase[SIZE * SIZE];
        var nonSolidTiles = new TileBase[SIZE * SIZE];
        var wallTiles = new TileBase[SIZE * SIZE];
        var liquidTiles = new TileBase[SIZE * SIZE];

        for (int ly = 0; ly < SIZE; ly++)
        for (int lx = 0; lx < SIZE; lx++)
        {
            int wx = startX + lx + offsetX;
            int wy = startY + ly + offsetY;
            int i = ly * SIZE + lx;
            positions[i] = new Vector3Int(wx, wy, 0);

            var block = world.GetBlock(startX + lx, startY + ly);
            if (block != BlockType.Water)
            {
                var data = _blockRegistry.Get(block);
                if (data != null && !data.isSolid)
                    nonSolidTiles[i] = ResolveTile(wx, wy, data);
                else if (data != null)
                    solidTiles[i] = ResolveTile(wx, wy, data);
            }

            var wall = world.GetWall(startX + lx, startY + ly);
            wallTiles[i] = _wallRegistry.Get(wall)?.tile;

            liquidTiles[i] = ResolveLiquidTile(world, startX + lx, startY + ly);
        }

        _tilemap.SetTiles(positions, solidTiles);
        _nonSolidTilemap.SetTiles(positions, nonSolidTiles);
        _wallTilemap.SetTiles(positions, wallTiles);
        _liquidTilemap.SetTiles(positions, liquidTiles);
    }

    public void RefreshTile(int wx, int wy, int offsetX, int offsetY, BlockData data)
    {
        var pos = new Vector3Int(wx + offsetX, wy + offsetY, 0);
        if (data != null && data.blockType == BlockType.Water) return;
        TileBase tile = data != null ? ResolveTile(wx + offsetX, wy + offsetY, data) : null;
        if (data != null && !data.isSolid)
        {
            _nonSolidTilemap.SetTile(pos, tile);
            _tilemap.SetTile(pos, null);
        }
        else
        {
            _tilemap.SetTile(pos, tile);
            _nonSolidTilemap.SetTile(pos, null);
        }
    }

    public void RefreshLiquidTile(int wx, int wy, int offsetX, int offsetY, byte level)
    {
        int lx = wx;
        int ly = wy;
        var world = WorldManager.Instance.Data;
        bool belowEmpty = !world.InBounds(lx, ly - 1) || world.GetBlock(lx, ly - 1) == BlockType.Air && world.GetLiquid(lx, ly - 1) == 0;
        _liquidTilemap.SetTile(new Vector3Int(wx + offsetX, wy + offsetY, 0), LiquidTileSet.Instance?.Resolve(level, belowEmpty));
    }

    public void RefreshWallTile(int wx, int wy, int offsetX, int offsetY, TileBase tile)
    {
        _wallTilemap.SetTile(new Vector3Int(wx + offsetX, wy + offsetY, 0), tile);
    }

    public void FlushIfDirty() { }

    public void Clear()
    {
        UnregisterRenderers();
        _tilemap.ClearAllTiles();
        _nonSolidTilemap.ClearAllTiles();
        _wallTilemap.ClearAllTiles();
        _liquidTilemap.ClearAllTiles();
    }
}