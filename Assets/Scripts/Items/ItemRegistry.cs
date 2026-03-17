// Assets/Scripts/Items/ItemRegistry.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/ItemRegistry")]
public class ItemRegistry : ScriptableObject
{
    [SerializeField] private List<ItemDefinition> items = new();

    private Dictionary<string, ItemDefinition> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<string, ItemDefinition>(items.Count);
        foreach (var item in items)
            if (item != null)
                _lookup[item.itemId] = item;
    }

    public ItemDefinition Get(string itemId)
    {
        _lookup.TryGetValue(itemId, out var def);
        return def;
    }

    public ItemDefinition GetByBlockType(BlockType blockType)
    {
        foreach (var item in items)
            if (item.isBlock && item.blockType == blockType)
                return item;
        return null;
    }
}