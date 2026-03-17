using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private int worldWidth = 200;
    [SerializeField] private int worldHeight = 100;
    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private Tilemap foregroundTilemap;

    public WorldData Data { get; private set; }
    public int OffsetX => -worldWidth  / 2;
    public int OffsetY => -worldHeight / 2;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        blockRegistry.Initialize();
        Data = new WorldData(worldWidth, worldHeight);
        worldGenerator.Generate(Data);
    }

    void Start()
    {
        RenderAll();
    }

    public BlockType GetBlock(int x, int y)
        => Data.GetBlock(x - OffsetX, y - OffsetY);

    public void PlaceBlock(int x, int y, BlockType type)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (!Data.SetBlock(lx, ly, type)) return;

        var data = blockRegistry.Get(type);
        foregroundTilemap.SetTile(new Vector3Int(x, y, 0), data?.tile);
        ChunkManager.Instance.RefreshBlock(lx, ly, OffsetX, OffsetY, data?.tile);
    }

    public void DestroyBlock(int x, int y)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (Data.GetBlock(lx, ly) == BlockType.Air) return;
        Data.SetBlock(lx, ly, BlockType.Air);
        foregroundTilemap.SetTile(new Vector3Int(x, y, 0), null);
        ChunkManager.Instance.RefreshBlock(lx, ly, OffsetX, OffsetY, null);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);
        return new Vector3Int(x, y, 0);
    }

    public Vector3 CellToWorld(int x, int y)
        => new Vector3(x, y, 0);

    public int GetSurfaceWorldY(int worldX)
    {
        int lx = worldX - OffsetX;
        for (int ly = Data.Height - 1; ly >= 0; ly--)
        {
            if (Data.GetBlock(lx, ly) != BlockType.Air &&
                Data.GetBlock(lx, ly + 1) == BlockType.Air)
                return ly + OffsetY;
        }
        return OffsetY;
    }

    private void RenderAll()
    {
        var positions = new Vector3Int[worldWidth * worldHeight];
        var tiles = new TileBase[worldWidth * worldHeight];

        for (int ly = 0; ly < worldHeight; ly++)
        for (int lx = 0; lx < worldWidth; lx++)
        {
            int i = ly * worldWidth + lx;
            positions[i] = new Vector3Int(lx + OffsetX, ly + OffsetY, 0);
            var block = Data.GetBlock(lx, ly);
            var data = blockRegistry.Get(block);
            tiles[i] = data?.tile;
        }

        foregroundTilemap.SetTiles(positions, tiles);
    }
}