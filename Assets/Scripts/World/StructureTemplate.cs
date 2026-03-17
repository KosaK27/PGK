// Assets/Scripts/World/Generation/StructureTemplate.cs
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "StructureTemplate", menuName = "World/StructureTemplate")]
public class StructureTemplate : ScriptableObject
{
    public string structureName;
    public Vector2Int size;
    public BlockType[] blocks; // flat array, row by row

    // Wywołaj w edytorze żeby wczytać z Tilemapa
    public void BakeFromTilemap(Tilemap tilemap, BlockRegistry registry, BoundsInt bounds)
    {
        size = new Vector2Int(bounds.size.x, bounds.size.y);
        blocks = new BlockType[size.x * size.y];

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
            var tile = tilemap.GetTile(cell);

            blocks[y * size.x + x] = BlockType.Air;

            if (tile == null) continue;

            for (byte i = 1; i < 255; i++)
            {
                var blockType = (BlockType)i;
                var blockData = registry.Get(blockType);
                if (blockData != null && blockData.tile == tile)
                {
                    blocks[y * size.x + x] = blockType;
                    break;
                }
            }
        }
    }

    public BlockType GetBlock(int x, int y)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y) return BlockType.Air;
        return blocks[y * size.x + x];
    }
}