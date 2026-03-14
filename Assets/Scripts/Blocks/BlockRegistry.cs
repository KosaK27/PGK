using System.Collections.Generic;
using UnityEngine;

public class BlockRegistry : ScriptableObject
{
    [SerializeField] private List<BlockData> blocks = new();

    private Dictionary<BlockType, BlockData> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<BlockType, BlockData>(blocks.Count);
        foreach (var block in blocks)
        {
            if (block != null)
                _lookup[block.blockType] = block;
        }
    }

    public BlockData Get(BlockType type)
    {
        _lookup.TryGetValue(type, out var data);
        return data;
    }
}