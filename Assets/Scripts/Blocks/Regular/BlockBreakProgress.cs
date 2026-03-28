using System.Collections.Generic;
using UnityEngine;

public class BlockBreakProgress
{
    private Dictionary<Vector3Int, float> _progress = new();

    public float Get(Vector3Int cell)
    {
        _progress.TryGetValue(cell, out float val);
        return val;
    }

    public void Add(Vector3Int cell, float amount)
    {
        _progress.TryGetValue(cell, out float val);
        _progress[cell] = val + amount;
    }

    public void Reset(Vector3Int cell)
    {
        _progress.Remove(cell);
    }

    public bool IsComplete(Vector3Int cell, float hardness)
    {
        return Get(cell) >= hardness;
    }
}