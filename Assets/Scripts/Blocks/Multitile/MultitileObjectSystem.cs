using System.Collections.Generic;
using UnityEngine;

public class MultitileObjectSystem : MonoBehaviour
{
    public static MultitileObjectSystem Instance { get; private set; }
    private readonly Dictionary<Vector2Int, MultitileObject> _cellMap = new();
    private readonly List<MultitileObject> _objects = new();
    private MultitileObject _currentBreakTarget;

    public IReadOnlyList<MultitileObject> GetAllObjects() => _objects;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryPlace(Vector2Int origin, MultitileObjectDefinition def)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(origin.x, origin.y))) return false;

        for (int y = 0; y < def.size.y; y++)
        for (int x = 0; x < def.size.x; x++)
        {
            var cell = new Vector2Int(origin.x + x, origin.y + y);
            if (_cellMap.ContainsKey(cell)) return false;
            if (WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air) return false;
        }

        if (!HasAllSupportBelow(origin, def.size)) return false;

        GameObject go;
        MultitileObject obj;

        if (def is CraftingStationDefinition stationDef)
        {
            go = new GameObject($"CraftingStation_{stationDef.stationType}_{origin.x}_{origin.y}");
            var station = go.AddComponent<CraftingStationObject>();
            station.InitializeStation(stationDef, origin);
            obj = station;
        }
        else if (def is ChestDefinition chestDef)
        {
            go = new GameObject($"Chest_{origin.x}_{origin.y}");
            var chest = go.AddComponent<ChestObject>();
            chest.InitializeChest(chestDef, origin);
            obj = chest;
        }
        else
        {
            go = new GameObject($"MultitileObject_{def.displayName}_{origin.x}_{origin.y}");
            obj = go.AddComponent<MultitileObject>();
            obj.Initialize(def, origin);
        }

        Register(obj);
        return true;
    }

    public bool TryBreak(Vector2Int cell, float delta)
    {
        var obj = Get(cell);
        if (obj == null) return false;

        if (obj is ChestObject chest && !IsChestEmpty(chest)) return false;

        if (_currentBreakTarget != obj)
        {
            _currentBreakTarget?.ResetBreakProgress();
            _currentBreakTarget = obj;
        }

        obj.AddBreakProgress(delta);
        if (!obj.IsComplete()) return false;

        var dropPos = new Vector2(
            obj.Origin.x + obj.Definition.size.x * 0.5f,
            obj.Origin.y + obj.Definition.size.y * 0.5f);

        if (obj.Definition.dropItem != null)
            ItemDropSystem.Instance.DropItem(
                new ItemStack(obj.Definition.dropItem, obj.Definition.dropAmount), dropPos);

        if (ContainerUIManager.Instance.IsOpen)
            ContainerUIManager.Instance.CloseContainer();

        DestroyObject(obj);
        return true;
    }

    private bool IsChestEmpty(ChestObject chest)
    {
        for (int i = 0; i < chest.Container.SlotCount; i++)
        {
            var slot = chest.Container.GetSlot(i);
            if (slot != null && !slot.IsEmpty) return false;
        }
        return true;
    }

    public float GetBreakProgress(Vector2Int cell)
    {
        var obj = Get(cell);
        return obj?.GetProgress() ?? 0f;
    }

    public void CancelBreak()
    {
        _currentBreakTarget?.ResetBreakProgress();
        _currentBreakTarget = null;
    }

    public MultitileObject Get(Vector2Int cell)
    {
        _cellMap.TryGetValue(cell, out var obj);
        return obj;
    }

    public bool IsOccupied(Vector2Int cell) => _cellMap.ContainsKey(cell);

    public bool IsSupporting(Vector2Int cell)
    {
        foreach (var obj in _objects)
        {
            var def = obj.Definition;
            for (int x = 0; x < def.size.x; x++)
                if (obj.Origin.x + x == cell.x && obj.Origin.y - 1 == cell.y)
                    return true;
        }
        return false;
    }

    private void Register(MultitileObject obj)
    {
        var def = obj.Definition;
        for (int y = 0; y < def.size.y; y++)
        for (int x = 0; x < def.size.x; x++)
            _cellMap[new Vector2Int(obj.Origin.x + x, obj.Origin.y + y)] = obj;
        _objects.Add(obj);
    }

    private void DestroyObject(MultitileObject obj)
    {
        var def = obj.Definition;
        for (int y = 0; y < def.size.y; y++)
        for (int x = 0; x < def.size.x; x++)
            _cellMap.Remove(new Vector2Int(obj.Origin.x + x, obj.Origin.y + y));
        _objects.Remove(obj);
        if (_currentBreakTarget == obj) _currentBreakTarget = null;
        Destroy(obj.gameObject);
    }

    private bool HasAllSupportBelow(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            var below = new Vector2Int(origin.x + x, origin.y - 1);
            if (WorldManager.Instance.GetBlock(below.x, below.y) == BlockType.Air) return false;
        }
        return true;
    }

    public void PlaceDirect(Vector2Int origin, MultitileObjectDefinition def)
    {
        GameObject go;
        MultitileObject obj;

        if (def is CraftingStationDefinition stationDef)
        {
            go = new GameObject($"CraftingStation_{stationDef.stationType}_{origin.x}_{origin.y}");
            var station = go.AddComponent<CraftingStationObject>();
            station.InitializeStation(stationDef, origin);
            obj = station;
        }
        else if (def is ChestDefinition chestDef)
        {
            go = new GameObject($"Chest_{origin.x}_{origin.y}");
            var chest = go.AddComponent<ChestObject>();
            chest.InitializeChest(chestDef, origin);
            obj = chest;
        }
        else
        {
            go = new GameObject($"MultitileObject_{def.displayName}_{origin.x}_{origin.y}");
            obj = go.AddComponent<MultitileObject>();
            obj.Initialize(def, origin);
        }

        Register(obj);
    }
}