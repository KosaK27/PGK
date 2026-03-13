using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private int worldWidth  = 200;
    [SerializeField] private int worldHeight = 100;
    [SerializeField] private BlockRegistry blockRegistry;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap foregroundTilemap;
    [SerializeField] private Tilemap backgroundTilemap;

    public WorldData Data { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        blockRegistry.Initialize();
        Data = new WorldData(worldWidth, worldHeight);
    }

    void Start()
    {
        GenerateWorld();
        //LoadFromTilemap();
        RenderAll();
    }

    public BlockType GetBlock(int x, int y)
        => Data.GetBlock(x, y);

    public void PlaceBlock(int x, int y, BlockType type)
    {
        if (!Data.SetBlock(x, y, type)) return;
        RefreshTile(x, y);
    }

    public void DestroyBlock(int x, int y)
    {
        if (Data.GetBlock(x, y) == BlockType.Air) return;
        Data.SetBlock(x, y, BlockType.Air);
        RefreshTile(x, y);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
        => foregroundTilemap.WorldToCell(worldPos);

    public Vector3 CellToWorld(int x, int y)
        => foregroundTilemap.CellToWorld(new Vector3Int(x, y, 0));

    private void RefreshTile(int x, int y)
    {
        var cell = new Vector3Int(x, y, 0);
        var block = Data.GetBlock(x, y);
        var data  = blockRegistry.Get(block);

        foregroundTilemap.SetTile(cell, data?.tile);
    }

    private void RenderAll()
    {
        var positions = new Vector3Int[worldWidth * worldHeight];
        var tiles     = new TileBase[worldWidth * worldHeight];

        for (int y = 0; y < worldHeight; y++)
        for (int x = 0; x < worldWidth; x++)
        {
            int i = y * worldWidth + x;
            positions[i] = new Vector3Int(x, y, 0);

            var block = Data.GetBlock(x, y);
            var data  = blockRegistry.Get(block);
            tiles[i]  = data?.tile;
        }

        foregroundTilemap.SetTiles(positions, tiles);
    }

    private void LoadFromTilemap()
    {
        for (int x = 0; x < worldWidth; x++)
        for (int y = 0; y < worldHeight; y++)
        {
            var cell = new Vector3Int(x, y, 0);
            var tile = foregroundTilemap.GetTile(cell);
            
            if (tile == null)
            {
                Data.SetBlock(x, y, BlockType.Air);
                continue;
            }

            bool found = false;
            for (byte i = 1; i < 255; i++)
            {
                var blockType = (BlockType)i;
                var blockData = blockRegistry.Get(blockType);
                if (blockData != null && blockData.tile == tile)
                {
                    Data.SetBlock(x, y, blockType);
                    found = true;
                    break;
                }
            }

            if (!found)
                Data.SetBlock(x, y, BlockType.Air);
        }
    }

    private void GenerateWorld()
    {
        int groundLevel = worldHeight / 2;

        for (int x = 0; x < worldWidth; x++)
        for (int y = 0; y < worldHeight; y++)
        {
            BlockType type;
            if      (y == groundLevel)     type = BlockType.Sand;
            else if (y > groundLevel)      type = BlockType.Air;
            else if (y > groundLevel - 5)  type = BlockType.Dirt;
            else                           type = BlockType.Stone;

            Data.SetBlock(x, y, type);
        }
    }
}