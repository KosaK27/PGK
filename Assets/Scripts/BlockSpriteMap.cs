using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockSpriteMap", menuName = "World/BlockSpriteMap")]
public class BlockSpriteMap : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public BlockType blockType;
        public Sprite sprite;
    }

    public List<Entry> entries = new();

    private Dictionary<BlockType, Sprite> _map;

    public void Initialize()
    {
        _map = new Dictionary<BlockType, Sprite>();
        foreach (var e in entries)
            if (e.sprite != null)
                _map[e.blockType] = e.sprite;
    }

    public Sprite Get(BlockType type)
    {
        if (_map == null) Initialize();
        _map.TryGetValue(type, out var sprite);
        return sprite;
    }
}