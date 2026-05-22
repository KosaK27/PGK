using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private static string RootDir => Path.Combine(Application.persistentDataPath, "Saves");
    private static string CharactersDir => Path.Combine(RootDir, "Characters");
    private static string WorldsDir => Path.Combine(RootDir, "Worlds");
    private static string ProfilePath => Path.Combine(RootDir, "profile.json");
    private static string SettingsPath => Path.Combine(RootDir, "settings.json");

    public GameProfile Profile { get; private set; } = new();
    public List<CharacterSaveData> Characters { get; private set; } = new();
    public List<WorldSaveData> Worlds { get; private set; } = new();
    public SettingsData Settings { get; private set; } = new();

    public CharacterSaveData SelectedCharacter => Characters.Find(c => c.id == Profile.selectedCharacterId);
    public WorldSaveData SelectedWorld => Worlds.Find(w => w.id == Profile.selectedWorldId);

    private HashSet<Vector2Int> _discoveredChunksSet = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(CharactersDir);
        Directory.CreateDirectory(WorldsDir);
        Profile = LoadJson<GameProfile>(ProfilePath) ?? new GameProfile();
        Characters = LoadAll<CharacterSaveData>(CharactersDir);
        Worlds = LoadAll<WorldSaveData>(WorldsDir);
        Settings = LoadJson<SettingsData>(SettingsPath) ?? new SettingsData();
    }

    public CharacterSaveData CreateCharacter(string name)
    {
        var c = new CharacterSaveData { id = Guid.NewGuid().ToString(), characterName = name, createdAt = DateTime.UtcNow.Ticks, maxHP = 20, currentHP = 20 };
        Characters.Add(c);
        SaveCharacter(c);
        return c;
    }

    public void SaveCharacter(CharacterSaveData c) => WriteJson(c, Path.Combine(CharactersDir, c.id + ".json"));

    public void DeleteCharacter(string id)
    {
        Characters.RemoveAll(c => c.id == id);
        var path = Path.Combine(CharactersDir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
        if (Profile.selectedCharacterId == id) { Profile.selectedCharacterId = null; SaveProfile(); }
    }

    public WorldSaveData CreateWorld(string name, int width, int height, int seed)
    {
        var w = new WorldSaveData { id = Guid.NewGuid().ToString(), worldName = name, width = width, height = height, seed = seed, createdAt = DateTime.UtcNow.Ticks, lastPlayedAt = 0 };
        Worlds.Add(w);
        SaveWorld(w);
        return w;
    }

    public void SaveWorld(WorldSaveData w) => WriteJson(w, Path.Combine(WorldsDir, w.id + ".json"));

    public void DeleteWorld(string id)
    {
        Worlds.RemoveAll(w => w.id == id);
        var path = Path.Combine(WorldsDir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
        if (Profile.selectedWorldId == id) { Profile.selectedWorldId = null; SaveProfile(); }
    }

    public void SelectCharacter(string id) { Profile.selectedCharacterId = id; SaveProfile(); }
    public void SelectWorld(string id) { Profile.selectedWorldId = id; SaveProfile(); }
    public void SaveProfile() => WriteJson(Profile, ProfilePath);
    public void SaveSettings() => WriteJson(Settings, SettingsPath);

    public void CaptureWorldState(WorldSaveData save, WorldData world)
    {
        save.blockRLE = WorldRLE.EncodeBlocks(world);
        save.wallRLE = WorldRLE.EncodeWalls(world);
        save.liquidRLE = WorldRLE.EncodeLiquids(world);
        save.lastPlayedAt = DateTime.UtcNow.Ticks;
    }

    public void RestoreWorldState(WorldSaveData save, WorldData world)
    {
        WorldRLE.DecodeInto(world, save.blockRLE, save.wallRLE, save.liquidRLE);

        _discoveredChunksSet.Clear();
        if (save.discoveredChunks != null)
            foreach (var c in save.discoveredChunks)
                _discoveredChunksSet.Add(c);
    }

    public bool HasWorldState(WorldSaveData save) =>
        save != null && save.blockRLE != null && save.blockRLE.Count > 0;

    public void CaptureCharacterState(CharacterSaveData save, GameObject player, string worldId)
    {
        save.inventorySlots.Clear();
        save.hotbarSelectedIndex = InventorySystem.Instance.SelectedHotbarIndex;
        int total = InventorySystem.Instance.TotalSlots;
        for (int i = 0; i < total; i++)
        {
            var slot = InventorySystem.Instance.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;
            save.inventorySlots.Add(new ItemSlotSave { slotIndex = i, itemId = slot.item.itemId, amount = slot.amount });
        }
        var stats = player.GetComponent<PlayerStats>();
        if (stats != null) { save.currentHP = stats.currentHP; save.maxHP = stats.maxHP; }

        var acc = AccessorySystem.Instance;
        if (acc != null)
        {
            save.equippedAccessoryIds.Clear();
            for (int i = 0; i < acc.SlotCount; i++)
                save.equippedAccessoryIds.Add(acc.GetSlot(i)?.accessoryId ?? "");
        }

        save.worldPositions.RemoveAll(p => p.worldId == worldId);
        save.worldPositions.Add(new WorldPositionSave { worldId = worldId, positionX = player.transform.position.x, positionY = player.transform.position.y });
    }

    public void RestoreCharacterState(CharacterSaveData save, GameObject player, ItemRegistry registry, string worldId, Vector3 fallbackSpawnPos)
    {
        int total = InventorySystem.Instance.TotalSlots;
        for (int i = 0; i < total; i++) InventorySystem.Instance.SetSlot(i, null);
        foreach (var entry in save.inventorySlots)
        {
            var def = registry.Get(entry.itemId);
            if (def == null) continue;
            InventorySystem.Instance.SetSlot(entry.slotIndex, new ItemStack(def, entry.amount));
        }
        InventorySystem.Instance.SelectHotbarSlot(save.hotbarSelectedIndex);

        var stats = player.GetComponent<PlayerStats>();
        if (stats != null) { stats.currentHP = save.currentHP > 0 ? save.currentHP : save.maxHP; stats.maxHP = save.maxHP > 0 ? save.maxHP : 20; }

        var acc = AccessorySystem.Instance;
        if (acc != null && save.equippedAccessoryIds != null)
        {
            for (int i = 0; i < acc.SlotCount; i++)
            {
                string accId = i < save.equippedAccessoryIds.Count ? save.equippedAccessoryIds[i] : "";
                acc.Equip(i, registry.GetAccessoryById(accId));
            }
        }

        var worldPos = save.worldPositions.Find(p => p.worldId == worldId);
        player.transform.position = worldPos != null ? new Vector3(worldPos.positionX, worldPos.positionY, 0f) : fallbackSpawnPos;
    }

    public bool IsChunkDiscovered(int x, int y) => _discoveredChunksSet.Contains(new Vector2Int(x, y));

    public void DiscoverChunk(int x, int y)
    {
        var pos = new Vector2Int(x, y);
        if (_discoveredChunksSet.Add(pos))
        {
            if (SelectedWorld != null)
            {
                SelectedWorld.discoveredChunks ??= new List<Vector2Int>();
                SelectedWorld.discoveredChunks.Add(pos);
            }
        }
    }

    private static void WriteJson<T>(T obj, string path)
    {
        try { File.WriteAllText(path, JsonUtility.ToJson(obj, true)); }
        catch (Exception) { }
    }

    private static T LoadJson<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
        catch (Exception) { return null; }
    }

    private static List<T> LoadAll<T>(string dir) where T : class
    {
        var list = new List<T>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var obj = LoadJson<T>(file);
            if (obj != null) list.Add(obj);
        }
        return list;
    }
}