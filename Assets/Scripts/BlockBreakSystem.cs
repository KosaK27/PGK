using UnityEngine;

public class BlockBreakSystem : MonoBehaviour
{
    public static BlockBreakSystem Instance { get; private set; }

    [SerializeField] private BlockRegistry blockRegistry;

    private BlockBreakProgress _breakProgress = new();
    private Vector3Int _currentBreakCell = new(int.MinValue, 0, 0);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryBreak(Vector3Int cell, float deltaTime)
    {
        var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
        if (blockType == BlockType.Air) return false;

        var data = blockRegistry.Get(blockType);
        if (data == null || !data.destructible) return false;

        if (cell != _currentBreakCell)
        {
            _breakProgress.Reset(_currentBreakCell);
            _currentBreakCell = cell;
        }

        _breakProgress.Add(cell, deltaTime);

        if (_breakProgress.IsComplete(cell, data.hardness))
        {
            _breakProgress.Reset(cell);
            _currentBreakCell = new(int.MinValue, 0, 0);
            WorldManager.Instance.DestroyBlock(cell.x, cell.y);
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

        return _breakProgress.Get(cell) / data.hardness;
    }

    public void CancelBreak()
    {
        _breakProgress.Reset(_currentBreakCell);
        _currentBreakCell = new(int.MinValue, 0, 0);
    }
}