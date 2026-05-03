using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "StructureTemplate", menuName = "World/StructureTemplate")]
public class StructureTemplate : ScriptableObject
{
    [Serializable]
    public class ObjectPlacement
    {
        public Vector2Int localOrigin;
        public MultitileObjectDefinition definition;
    }

    public string structureName;
    public Vector2Int size;
    public int minCount = 1;
    public int maxCount = 3;
    public BlockType[] blocks;
    public WallType[] walls;
    public List<ObjectPlacement> objects = new();

    public BlockType GetBlock(int x, int y)
    {
        if (x < 0 || x >= size.x || y < 0 || y >= size.y) return BlockType.Air;
        return blocks[y * size.x + x];
    }

    public WallType GetWall(int x, int y)
    {
        if (walls == null || walls.Length == 0) return WallType.None;
        if (x < 0 || x >= size.x || y < 0 || y >= size.y) return WallType.None;
        return walls[y * size.x + x];
    }

    public void BakeFromTilemap(Tilemap tilemap, BlockRegistry registry, BoundsInt bounds)
    {
        size = new Vector2Int(bounds.size.x, bounds.size.y);
        blocks = new BlockType[size.x * size.y];

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var tile = tilemap.GetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0));
            blocks[y * size.x + x] = tile == null ? BlockType.Air : registry.GetBlockType(tile);
        }
    }

    public void BakeWallsFromTilemap(Tilemap tilemap, WallRegistry registry, BoundsInt bounds)
    {
        walls = new WallType[size.x * size.y];

        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var tile = tilemap.GetTile(new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0));
            walls[y * size.x + x] = tile == null ? WallType.None : registry.GetWallType(tile);
        }
    }

    public void BakeObjectsFromTilemap(Tilemap tilemap, MultitileObjectRegistry registry, BoundsInt bounds)
    {
        objects.Clear();
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            var tile = tilemap.GetTile(pos);
            if (tile == null) continue;
            var def = registry.GetByTile(tile);
            if (def == null) continue;
            var local = new Vector2Int(pos.x - bounds.xMin, pos.y - bounds.yMin);
            objects.Add(new ObjectPlacement { localOrigin = local, definition = def });
        }
    }
}