using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }
    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private int worldWidth = 200;
    [SerializeField] private int worldHeight = 100;
    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private WallRegistry wallRegistry;

    private readonly Dictionary<(ConnectedTile, int), Tile> _tileCache = new();
    public WorldData Data { get; private set; }
    public WorldData OriginalData { get; private set; }
    public int OffsetX => Data != null ? -Data.Width / 2 : 0;
    public int OffsetY => Data != null ? -Data.Height / 2 : 0;

    private static readonly Vector2Int[] _neighbors = {
        new(1,0), new(-1,0), new(0,1), new(0,-1),
        new(1,1), new(-1,1), new(1,-1), new(-1,-1)
    };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        blockRegistry.Initialize();
        wallRegistry.Initialize();

        if (WorldDataTransfer.Data != null)
        {
            Data = WorldDataTransfer.Data;
            OriginalData = WorldDataTransfer.OriginalData;
            WorldDataTransfer.Data = null;
            WorldDataTransfer.OriginalData = null;
        }
        else
        {
            Data = new WorldData(worldWidth, worldHeight);
            worldGenerator.Generate(Data);
            OriginalData = Data.Clone();
        }
    }

    void Start()
    {
        if (LiquidManager.Instance != null && Data != null) InitializeLiquids();
    }

    private void InitializeLiquids()
    {
        for (int x = 0; x < Data.Width; x++)
            for (int y = 0; y < Data.Height; y++)
                if (Data.GetBlock(x, y) == BlockType.Water)
                    LiquidManager.Instance.AddActiveWater(x + OffsetX, y + OffsetY);
    }

    public BlockType GetBlock(int x, int y) => Data.GetBlock(x - OffsetX, y - OffsetY);
    public WallType GetWall(int x, int y) => Data.GetWall(x - OffsetX, y - OffsetY);

    public BlockData GetBlockData(int worldX, int worldY)
    {
        return blockRegistry.Get(GetBlock(worldX, worldY));
    }

    public TileBase GetConnectedTileBase(int wx, int wy, ConnectedTile connectedTile)
    {
        bool up = IsSolidBlock(wx, wy + 1);
        bool down = IsSolidBlock(wx, wy - 1);
        bool left = IsSolidBlock(wx - 1, wy);
        bool right = IsSolidBlock(wx + 1, wy);
        int index = (up ? 1 : 0) | (down ? 2 : 0) | (left ? 4 : 0) | (right ? 8 : 0);
        var key = (connectedTile, index);
        if (!_tileCache.TryGetValue(key, out var tile))
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = connectedTile.sprites[index];
            _tileCache[key] = tile;
        }
        return tile;
    }

    private bool IsSolidBlock(int wx, int wy)
    {
        var block = GetBlock(wx, wy);
        return block != BlockType.Air && block != BlockType.Water;
    }

    public void PlaceBlock(int x, int y, BlockType type)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (!Data.SetBlock(lx, ly, type)) return;
        RefreshBlockAndNeighbors(x, y);
        GravityBlockSystem.Instance?.NotifyNeighbors(x, y);
        if (LiquidManager.Instance != null && !LiquidManager.Instance.isSimulating)
            LiquidManager.Instance.NotifyBlockChanged(x, y);
        LightingSystem.Instance?.RebuildLightMapAt(x, y);
    }

    public void DestroyBlock(int x, int y)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (Data.GetBlock(lx, ly) == BlockType.Air) return;
        Data.SetBlock(lx, ly, BlockType.Air);
        RefreshBlockAndNeighbors(x, y);
        GravityBlockSystem.Instance?.NotifyNeighbors(x, y);
        if (LiquidManager.Instance != null && !LiquidManager.Instance.isSimulating)
            LiquidManager.Instance.NotifyBlockChanged(x, y);
        LightingSystem.Instance?.RebuildLightMapAt(x, y);
    }

    public void PlaceWall(int x, int y, WallType type)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (!Data.SetWall(lx, ly, type)) return;
        var data = wallRegistry.Get(type);
        ChunkManager.Instance.RefreshWall(lx, ly, OffsetX, OffsetY, data?.tile);
        LightingSystem.Instance?.RebuildLightMapAt(x, y);
    }

    public void DestroyWall(int x, int y)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (Data.GetWall(lx, ly) == WallType.None) return;
        Data.SetWall(lx, ly, WallType.None);
        ChunkManager.Instance.RefreshWall(lx, ly, OffsetX, OffsetY, null);
        LightingSystem.Instance?.RebuildLightMapAt(x, y);
    }

    public void RefreshBlockAndNeighbors(int wx, int wy)
    {
        RefreshSingleBlock(wx, wy);
        foreach (var n in _neighbors)
            RefreshSingleBlock(wx + n.x, wy + n.y);
    }

    private void RefreshSingleBlock(int wx, int wy)
    {
        int lx = wx - OffsetX;
        int ly = wy - OffsetY;
        var data = blockRegistry.Get(GetBlock(wx, wy));
        ChunkManager.Instance.RefreshBlock(lx, ly, OffsetX, OffsetY, data);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return new Vector3Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y), 0);
    }

    public Vector3 CellToWorld(int x, int y) => new Vector3(x, y, 0);

    public int GetSurfaceWorldY(int worldX)
    {
        int lx = worldX - OffsetX;
        for (int ly = Data.Height - 2; ly >= 0; ly--)
        {
            if (Data.GetBlock(lx, ly) != BlockType.Air && Data.GetBlock(lx, ly + 1) == BlockType.Air)
                return ly + OffsetY;
        }
        return OffsetY;
    }

    void OnDestroy()
    {
        foreach (var tile in _tileCache.Values)
            if (tile != null) DestroyImmediate(tile);
        _tileCache.Clear();
    }
}