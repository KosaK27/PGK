using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WallRegistry", menuName = "World/WallRegistry")]
public class WallRegistry : ScriptableObject
{
    [SerializeField] private List<WallData> walls = new();

    private Dictionary<WallType, WallData> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<WallType, WallData>(walls.Count);
        foreach (var wall in walls)
            if (wall != null)
                _lookup[wall.wallType] = wall;
    }

    public WallData Get(WallType type)
    {
        _lookup.TryGetValue(type, out var data);
        return data;
    }
}