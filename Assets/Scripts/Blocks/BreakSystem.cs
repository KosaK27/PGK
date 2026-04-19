using UnityEngine;

public enum BreakTarget { Block, Wall }

public class BreakSystem : MonoBehaviour
{
    public static BreakSystem Instance { get; private set; }

    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private WallRegistry wallRegistry;
    [SerializeField] private ItemRegistry itemRegistry;

    private BlockBreakProgress _breakProgress = new();
    private Vector3Int _currentBreakCell = new(int.MinValue, 0, 0);
    private BreakTarget _currentTarget;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryBreak(Vector3Int cell, BreakTarget target, float deltaTime)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(cell.x, cell.y))) return false;
        if (!TryGetEffectiveHardness(cell, target, out float effectiveHardness)) return false;

        if (cell != _currentBreakCell || target != _currentTarget)
        {
            _breakProgress.Reset(_currentBreakCell);
            _currentBreakCell = cell;
            _currentTarget = target;
        }

        if (target == BreakTarget.Block)
        {
            if (MultitileObjectSystem.Instance.IsSupporting(new Vector2Int(cell.x, cell.y)))
                return false;
        }

        _breakProgress.Add(cell, deltaTime);

        if (_breakProgress.IsComplete(cell, effectiveHardness))
        {
            _breakProgress.Reset(cell);
            _currentBreakCell = new(int.MinValue, 0, 0);

            var worldPos = WorldManager.Instance.CellToWorld(cell.x, cell.y)
                           + new Vector3(0.5f, 0.5f, 0);

            if (target == BreakTarget.Block) FinishBreakBlock(cell, worldPos);
            else FinishBreakWall(cell, worldPos);

            return true;
        }

        return false;
    }

    public float GetBreakProgress(Vector3Int cell, BreakTarget target)
    {
        if (!TryGetEffectiveHardness(cell, target, out float effectiveHardness)) return 0f;
        return _breakProgress.Get(cell) / effectiveHardness;
    }

    public void CancelBreak()
    {
        _breakProgress.Reset(_currentBreakCell);
        _currentBreakCell = new(int.MinValue, 0, 0);
    }

    private bool TryGetEffectiveHardness(Vector3Int cell, BreakTarget target,
                                          out float effectiveHardness)
    {
        effectiveHardness = 0f;

        var selected = InventorySystem.Instance.SelectedItem;
        if (selected == null || selected.IsEmpty) return false;

        var item = selected.item;
        if (!item.isTool) return false;
        if (item.isWeapon) return false;

        var toolType = item.toolType;

        if (target == BreakTarget.Block)
        {
            var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
            if (blockType == BlockType.Air) return false;

            var data = blockRegistry.Get(blockType);
            if (data == null || !data.destructible) return false;

            effectiveHardness = toolType == data.requiredTool
                ? data.hardness / selected.item.breakingSpeed
                : data.hardness * 2f;
        }
        else
        {
            var wallType = WorldManager.Instance.GetWall(cell.x, cell.y);
            if (wallType == WallType.None) return false;

            var data = wallRegistry.Get(wallType);
            if (data == null || !data.destructible) return false;

            effectiveHardness = toolType != ToolType.None && toolType == data.requiredTool
                ? data.hardness / selected.item.breakingSpeed
                : data.hardness * 2f;
        }

        return true;
    }

    private void FinishBreakBlock(Vector3Int cell, Vector3 worldPos)
    {
        var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
        var data = blockRegistry.Get(blockType);

        WorldManager.Instance.DestroyBlock(cell.x, cell.y);

        if (data != null && data.dropType != BlockType.Air)
        {
            var itemDef = itemRegistry.GetByBlockType(data.dropType);
            if (itemDef != null)
                ItemDropSystem.Instance.DropItem(
                    new ItemStack(itemDef, data.dropAmount), worldPos);
        }
    }

    private void FinishBreakWall(Vector3Int cell, Vector3 worldPos)
    {
        var wallType = WorldManager.Instance.GetWall(cell.x, cell.y);
        var data = wallRegistry.Get(wallType);

        WorldManager.Instance.DestroyWall(cell.x, cell.y);

        if (data != null && data.dropType != WallType.None)
        {
            var itemDef = itemRegistry.GetByWallType(data.dropType);
            if (itemDef != null)
                ItemDropSystem.Instance.DropItem(
                    new ItemStack(itemDef, data.dropAmount), worldPos);
        }
    }
}