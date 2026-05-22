using System.Collections.Generic;
using UnityEngine;

public class LiquidManager : MonoBehaviour
{
    public static LiquidManager Instance { get; private set; }

    [SerializeField] private float tickRate = 0.05f;
    [SerializeField] private byte evaporateBelow = 3;

    private HashSet<Vector2Int> _activeCells = new();
    private HashSet<Vector2Int> _nextActive = new();
    private HashSet<Vector2Int> _pending = new();
    private float _timer;

    public bool IsSimulating { get; private set; }

    void Awake() => Instance = this;

    public void AddActiveCell(int x, int y) => _pending.Add(new Vector2Int(x, y));

    public void NotifyBlockChanged(int x, int y) => WakeNeighbors(x, y);

    void Update()
    {
        if (WorldManager.Instance == null) return;
        _timer += Time.deltaTime;
        if (_timer >= tickRate)
        {
            _timer = 0;
            foreach (var p in _pending)
                _activeCells.Add(p);
            _pending.Clear();
            if (_activeCells.Count > 0)
                Simulate();
        }
    }

    private void Simulate()
    {
        IsSimulating = true;
        _nextActive.Clear();

        var current = new List<Vector2Int>(_activeCells);
        _activeCells.Clear();
        current.Sort((a, b) => a.y.CompareTo(b.y));

        foreach (var pos in current)
            SimulateCell(pos.x, pos.y);

        (_activeCells, _nextActive) = (_nextActive, _activeCells);
        IsSimulating = false;
    }

    private void SimulateCell(int x, int y)
    {
        byte level = WorldManager.Instance.GetLiquid(x, y);
        if (level == 0) return;

        if (level < evaporateBelow)
        {
            Write(x, y, 0);
            return;
        }

        TryFlowDown(x, y, ref level);

        if (level > 1)
        {
            EqualizeWith(x, y, x - 1, y, ref level);
            EqualizeWith(x, y, x + 1, y, ref level);
        }

        if (level > 0)
            _nextActive.Add(new Vector2Int(x, y));
    }

    private void TryFlowDown(int x, int y, ref byte level)
    {
        if (!IsPassable(x, y - 1)) return;

        byte below = WorldManager.Instance.GetLiquid(x, y - 1);
        int space = 255 - below;
        if (space <= 0) return;

        int give = Mathf.Min(level, space);
        byte newLevel = (byte)(level - give);
        byte newBelow = (byte)(below + give);

        Write(x, y, newLevel);
        Write(x, y - 1, newBelow);
        level = newLevel;

        _nextActive.Add(new Vector2Int(x, y - 1));
        WakeNeighbors(x, y - 1);
    }

    private void EqualizeWith(int ax, int ay, int bx, int by, ref byte aLevel)
    {
        if (!IsPassable(bx, by)) return;

        byte bLevel = WorldManager.Instance.GetLiquid(bx, by);
        if (aLevel <= bLevel) return;

        int total = aLevel + bLevel;
        byte newA = (byte)((total + 1) / 2);
        byte newB = (byte)(total / 2);
        if (newA == aLevel) return;

        Write(ax, ay, newA);
        Write(bx, by, newB);
        aLevel = newA;

        if (newB > 0)
        {
            _nextActive.Add(new Vector2Int(bx, by));
            WakeNeighbors(bx, by);
        }
    }

    private void Write(int x, int y, byte amount) =>
        WorldManager.Instance.SetLiquid(x, y, amount);

    private bool IsPassable(int x, int y)
    {
        var block = WorldManager.Instance.GetBlock(x, y);
        return block == BlockType.Air || block == BlockType.Water;
    }

    private void WakeNeighbors(int x, int y)
    {
        for (int iy = -1; iy <= 1; iy++)
            for (int ix = -1; ix <= 1; ix++)
                if (WorldManager.Instance.GetLiquid(x + ix, y + iy) > 0)
                    _nextActive.Add(new Vector2Int(x + ix, y + iy));
    }
}