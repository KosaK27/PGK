using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "StructureTemplate", menuName = "World/StructureTemplate")]
public class StructureTemplate : ScriptableObject
{
    public string     structureName;
    public Vector2Int size;
    public int        minCount = 1;
    public int        maxCount = 3;
    public BlockType[] blocks;

    public BlockType GetBlock(int x, int y)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y) return BlockType.Air;
        return blocks[y * size.x + x];
    }

    public void BakeFromTilemap(Tilemap tilemap, BlockRegistry registry, BoundsInt bounds)
    {
        size   = new Vector2Int(bounds.size.x, bounds.size.y);
        blocks = new BlockType[size.x * size.y];

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var tile = tilemap.GetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0));
            blocks[y * size.x + x] = tile == null ? BlockType.Air : registry.GetBlockType(tile);
        }
    }
}