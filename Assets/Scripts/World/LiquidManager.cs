using System.Collections.Generic;
using UnityEngine;

public class LiquidManager : MonoBehaviour
{
    public static LiquidManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float tickRate = 0.05f;

    private HashSet<Vector2Int> _activeWater = new HashSet<Vector2Int>();
    private float _timer;
    public bool isSimulating { get; private set; }

    void Awake() => Instance = this;

    public void AddActiveWater(int x, int y) => _activeWater.Add(new Vector2Int(x, y));

    public void NotifyBlockChanged(int x, int y)
    {
        if (isSimulating) return;
        WakeNeighbors(x, y);
    }

    void Update()
    {
        if (WorldManager.Instance == null) return;
        _timer += Time.deltaTime;
        if (_timer >= tickRate)
        {
            _timer = 0;
            if (_activeWater.Count > 0) Simulate();
        }
    }

    private void Simulate()
    {
        isSimulating = true;
        List<Vector2Int> currentTick = new List<Vector2Int>(_activeWater);
        _activeWater.Clear();

        currentTick.Sort((a, b) => a.y.CompareTo(b.y));

        foreach (var pos in currentTick)
        {
            UpdateBlock(pos.x, pos.y);
        }
        isSimulating = false;
    }

    private void UpdateBlock(int x, int y)
    {
        if (WorldManager.Instance.GetBlock(x, y) != BlockType.Water) return;

        if (TryMove(x, y, x, y - 1)) return;

        int side = (Random.value > 0.5f) ? 1 : -1;

        if (TryMove(x, y, x + side, y)) return;
        if (TryMove(x, y, x - side, y)) return;
    }

    private bool TryMove(int ox, int oy, int nx, int ny)
    {
        if (WorldManager.Instance.GetBlock(nx, ny) == BlockType.Air)
        {
            WorldManager.Instance.PlaceBlock(ox, oy, BlockType.Air);
            WorldManager.Instance.PlaceBlock(nx, ny, BlockType.Water);

            _activeWater.Add(new Vector2Int(nx, ny));


            WakeNeighbors(ox, oy);
            WakeNeighbors(nx, ny);

            return true;
        }
        return false;
    }

    private void WakeNeighbors(int x, int y)
    {
        for (int iy = -1; iy <= 1; iy++)
        {
            for (int ix = -1; ix <= 1; ix++)
            {
                BlockType b = WorldManager.Instance.GetBlock(x + ix, y + iy);
                if (b == BlockType.Water)
                {
                    _activeWater.Add(new Vector2Int(x + ix, y + iy));
                }
            }
        }
    }
}