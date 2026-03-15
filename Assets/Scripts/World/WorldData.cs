using System;

public class WorldData
{
    public readonly int Width;
    public readonly int Height;
    private readonly BlockType[] _blocks;

    public WorldData(int width, int height)
    {
        Width  = width;
        Height = height;
        _blocks = new BlockType[width * height];
    }

    public BlockType GetBlock(int x, int y)
    {
        if (!InBounds(x, y)) return BlockType.Air;
        return _blocks[y * Width + x];
    }

    public bool SetBlock(int x, int y, BlockType type)
    {
        if (!InBounds(x, y)) return false;
        _blocks[y * Width + x] = type;
        return true;
    }

    public bool InBounds(int x, int y)
        => x >= 0 && x < Width && y >= 0 && y < Height;
    public BlockType[] GetRawData() => _blocks;
    
    public void LoadRawData(BlockType[] data)
    {
        if (data.Length != _blocks.Length)
            throw new ArgumentException("Nieprawidłowy rozmiar danych");
        Array.Copy(data, _blocks, data.Length);
    }
}