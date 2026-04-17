using System;
using System.Collections.Generic;

[Serializable]
public class CharacterSaveData
{
    public string id;
    public string characterName;
    public long createdAt;
    public List<ItemSlotSave> inventorySlots = new();
    public int hotbarSelectedIndex;
    public int currentHP;
    public int maxHP;
    public List<WorldPositionSave> worldPositions = new();
}

[Serializable]
public class WorldPositionSave
{
    public string worldId;
    public float positionX;
    public float positionY;
}

[Serializable]
public class ItemSlotSave
{
    public int slotIndex;
    public string itemId;
    public int amount;
}

[Serializable]
public class WorldSaveData
{
    public string id;
    public string worldName;
    public int width;
    public int height;
    public int seed;
    public long createdAt;
    public long lastPlayedAt;
    public List<RLEEntry> blocks = new();
    public List<RLEEntry> walls = new();
    public List<MultitileObjectSave> multitileObjects = new();
}

[Serializable]
public class RLEEntry
{
    public int type;
    public int count;
}

[Serializable]
public class MultitileObjectSave
{
    public string definitionName;
    public int originX;
    public int originY;
    public List<ItemSlotSave> containerSlots = new();
}

[Serializable]
public class GameProfile
{
    public string selectedCharacterId;
    public string selectedWorldId;
}