using UnityEngine;

public class BlockBreakSystem : MonoBehaviour
{
    public static BlockBreakSystem Instance { get; private set; }

    [SerializeField] private BlockRegistry blockRegistry;
    [SerializeField] private ItemRegistry  itemRegistry;

    private BlockBreakProgress _breakProgress = new();
    private Vector3Int _currentBreakCell = new(int.MinValue, 0, 0);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryBreak(Vector3Int cell, float deltaTime)
    {
        if (!ChunkManager.Instance.IsChunkLoaded(new Vector2(cell.x, cell.y))) return false;

        var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
        if (blockType == BlockType.Air) return false;

        var data = blockRegistry.Get(blockType);
        if (data == null || !data.destructible) return false;

        var selected = InventorySystem.Instance.SelectedItem;
        bool isTool  = selected != null && !selected.IsEmpty && selected.item.isTool;
        var toolType = isTool ? selected.item.toolType : ToolType.None;

        if (toolType == ToolType.Sword) return false;

        float effectiveHardness;
        if (isTool && toolType == data.requiredTool)
            effectiveHardness = data.hardness / selected.item.breakingSpeed;
        else
            effectiveHardness = data.hardness * 2f;

        if (cell != _currentBreakCell)
        {
            _breakProgress.Reset(_currentBreakCell);
            _currentBreakCell = cell;
        }

        _breakProgress.Add(cell, deltaTime);

        if (_breakProgress.IsComplete(cell, effectiveHardness))
        {
            _breakProgress.Reset(cell);
            _currentBreakCell = new(int.MinValue, 0, 0);

            var blockPos = WorldManager.Instance.CellToWorld(cell.x, cell.y) + new Vector3(0.5f, 0.5f, 0);
            WorldManager.Instance.DestroyBlock(cell.x, cell.y);

            if (data.dropType != BlockType.Air)
            {
                var itemDef = itemRegistry.GetByBlockType(data.dropType);
                if (itemDef != null)
                    ItemDropSystem.Instance.DropItem(new ItemStack(itemDef, data.dropAmount), blockPos);
            }
            return true;
        }

        return false;
    }

    public float GetBreakProgress(Vector3Int cell)
    {
        var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
        if (blockType == BlockType.Air) return 0f;

        var data = blockRegistry.Get(blockType);
        if (data == null) return 0f;

        var selected = InventorySystem.Instance.SelectedItem;
        bool isTool  = selected != null && !selected.IsEmpty && selected.item.isTool;
        var toolType = isTool ? selected.item.toolType : ToolType.None;

        float effectiveHardness;
        if (isTool && toolType == data.requiredTool)
            effectiveHardness = data.hardness / selected.item.breakingSpeed;
        else
            effectiveHardness = data.hardness * 2f;

        return _breakProgress.Get(cell) / effectiveHardness;
    }

    public void CancelBreak()
    {
        _breakProgress.Reset(_currentBreakCell);
        _currentBreakCell = new(int.MinValue, 0, 0);
    }
}