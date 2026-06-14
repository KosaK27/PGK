using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "Items/ItemRegistry")]
public class ItemRegistry : ScriptableObject
{
    public static ItemRegistry Instance { get; private set; }

    [SerializeField] private List<ItemDefinition> items = new();
    private Dictionary<string, ItemDefinition> _lookup;

    public void Initialize()
    {
        Instance = this;
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

    public AccessoryDefinition GetAccessoryById(string accessoryId)
    {
        if (string.IsNullOrEmpty(accessoryId)) return null;
        foreach (var item in items)
            if (item != null && item.isAccessory && item.accessoryDefinition != null && item.accessoryDefinition.accessoryId == accessoryId)
                return item.accessoryDefinition;
        return null;
    }

    public ItemDefinition GetItemByAccessoryId(string accessoryId)
    {
        if (string.IsNullOrEmpty(accessoryId)) return null;
        foreach (var item in items)
            if (item != null && item.isAccessory && item.accessoryDefinition != null && item.accessoryDefinition.accessoryId == accessoryId)
                return item;
        return null;
    }

    public ArmorDefinition GetArmorById(string armorId)
    {
        if (string.IsNullOrEmpty(armorId)) return null;
        foreach (var item in items)
            if (item != null && item.isArmor && item.armorDefinition != null && item.armorDefinition.armorId == armorId)
                return item.armorDefinition;
        return null;
    }

    public ItemDefinition GetItemByArmorId(string armorId)
    {
        if (string.IsNullOrEmpty(armorId)) return null;
        foreach (var item in items)
            if (item != null && item.isArmor && item.armorDefinition != null && item.armorDefinition.armorId == armorId)
                return item;
        return null;
    }

    public ItemDefinition GetByBlockType(BlockType blockType)
    {
        foreach (var item in items)
            if (item != null && item.isBlock && item.blockType == blockType)
                return item;
        return null;
    }

    public ItemDefinition GetByWallType(WallType wallType)
    {
        foreach (var item in items)
            if (item != null && item.isWall && item.wallType == wallType)
                return item;
        return null;
    }
}