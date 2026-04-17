using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [SerializeField] private WorldGenerator worldGenerator;
    [SerializeField] private int worldWidth = 200;
    [SerializeField] private int worldHeight = 100;
    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private WallRegistry wallRegistry;

    public WorldData Data { get; private set; }
    public int OffsetX => -Data.Width / 2;
    public int OffsetY => -Data.Height / 2;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        blockRegistry.Initialize();
        wallRegistry.Initialize();

        var sm = SaveManager.Instance;
        var worldSave = sm?.SelectedWorld;

        if (worldSave != null && worldSave.blocks != null && worldSave.blocks.Count > 0)
        {
            Data = new WorldData(worldSave.width, worldSave.height);
        }
        else if (worldSave != null)
        {
            Data = new WorldData(worldSave.width, worldSave.height);
            worldGenerator.randomSeed = false;
            worldGenerator.seed = worldSave.seed;
            worldGenerator.Generate(Data);
        }
        else
        {
            Data = new WorldData(worldWidth, worldHeight);
            worldGenerator.Generate(Data);
        }
    }

    public BlockType GetBlock(int x, int y) => Data.GetBlock(x - OffsetX, y - OffsetY);
    public WallType GetWall(int x, int y) => Data.GetWall(x - OffsetX, y - OffsetY);

    public void PlaceBlock(int x, int y, BlockType type)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (!Data.SetBlock(lx, ly, type)) return;
        var data = blockRegistry.Get(type);
        ChunkManager.Instance.RefreshBlock(lx, ly, OffsetX, OffsetY, data?.tile);
    }

    public void DestroyBlock(int x, int y)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (Data.GetBlock(lx, ly) == BlockType.Air) return;
        Data.SetBlock(lx, ly, BlockType.Air);
        ChunkManager.Instance.RefreshBlock(lx, ly, OffsetX, OffsetY, null);
    }

    public void PlaceWall(int x, int y, WallType type)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (!Data.SetWall(lx, ly, type)) return;
        var data = wallRegistry.Get(type);
        ChunkManager.Instance.RefreshWall(lx, ly, OffsetX, OffsetY, data?.tile);
    }

    public void DestroyWall(int x, int y)
    {
        int lx = x - OffsetX;
        int ly = y - OffsetY;
        if (Data.GetWall(lx, ly) == WallType.None) return;
        Data.SetWall(lx, ly, WallType.None);
        ChunkManager.Instance.RefreshWall(lx, ly, OffsetX, OffsetY, null);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);
        return new Vector3Int(x, y, 0);
    }

    public Vector3 CellToWorld(int x, int y) => new Vector3(x, y, 0);

    public int GetSurfaceWorldY(int worldX)
    {
        int lx = worldX - OffsetX;
        for (int ly = Data.Height - 1; ly >= 0; ly--)
        {
            if (Data.GetBlock(lx, ly) != BlockType.Air && Data.GetBlock(lx, ly + 1) == BlockType.Air)
                return ly + OffsetY;
        }
        return OffsetY;
    }
}