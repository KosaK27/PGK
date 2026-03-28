using System;

public class WorldData
{
    public readonly int Width;
    public readonly int Height;

    private readonly BlockType[] _blocks;
    private readonly WallType[]  _walls;

    public WorldData(int width, int height)
    {
        Width   = width;
        Height  = height;
        _blocks = new BlockType[width * height];
        _walls  = new WallType[width * height];
    }

    public BlockType GetBlock(int x, int y)                     => InBounds(x, y) ? _blocks[y * Width + x] : BlockType.Air;
    public bool      SetBlock(int x, int y, BlockType type)     { if (!InBounds(x, y)) return false; _blocks[y * Width + x] = type; return true; }

    public WallType  GetWall(int x, int y)                      => InBounds(x, y) ? _walls[y * Width + x] : WallType.None;
    public bool      SetWall(int x, int y, WallType type)       { if (!InBounds(x, y)) return false; _walls[y * Width + x] = type; return true; }

    public bool      InBounds(int x, int y)                     => x >= 0 && x < Width && y >= 0 && y < Height;

    public BlockType[] GetRawData()                             => _blocks;
    public WallType[]  GetRawWallData()                         => _walls;
}