using System;

public class WorldData
{
    public readonly int Width;
    public readonly int Height;

    private readonly BlockType[] _blocks;
    private readonly WallType[] _walls;
    private readonly byte[] _liquids;

    public WorldData(int width, int height)
    {
        Width = width;
        Height = height;
        _blocks = new BlockType[width * height];
        _walls = new WallType[width * height];
        _liquids = new byte[width * height];
    }

    public BlockType GetBlock(int x, int y) =>
        InBounds(x, y) ? _blocks[y * Width + x] : BlockType.Air;

    public bool SetBlock(int x, int y, BlockType type)
    {
        if (!InBounds(x, y)) return false;
        if (_liquids[y * Width + x] > 0) return false;
        _blocks[y * Width + x] = type;
        return true;
    }

    public WallType GetWall(int x, int y) =>
        InBounds(x, y) ? _walls[y * Width + x] : WallType.None;

    public bool SetWall(int x, int y, WallType type)
    {
        if (!InBounds(x, y)) return false;
        _walls[y * Width + x] = type;
        return true;
    }

    public byte GetLiquid(int x, int y) =>
        InBounds(x, y) ? _liquids[y * Width + x] : (byte)0;

    public bool SetLiquid(int x, int y, byte amount)
    {
        if (!InBounds(x, y)) return false;
        _liquids[y * Width + x] = amount;
        if (amount > 0)
            _blocks[y * Width + x] = BlockType.Water;
        else if (_blocks[y * Width + x] == BlockType.Water)
            _blocks[y * Width + x] = BlockType.Air;
        return true;
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    public BlockType[] GetRawData() => _blocks;
    public WallType[] GetRawWallData() => _walls;
    public byte[] GetRawLiquidData() => _liquids;

    public WorldData Clone()
    {
        var copy = new WorldData(Width, Height);
        Array.Copy(_blocks, copy._blocks, _blocks.Length);
        Array.Copy(_walls, copy._walls, _walls.Length);
        Array.Copy(_liquids, copy._liquids, _liquids.Length);
        return copy;
    }
}