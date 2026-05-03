using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "MultitileObjectRegistry", menuName = "World/MultitileObjectRegistry")]
public class MultitileObjectRegistry : ScriptableObject
{
    [SerializeField] private List<MultitileObjectDefinition> definitions = new();

    private Dictionary<string, MultitileObjectDefinition> _lookup;

    public void Initialize()
    {
        _lookup = new Dictionary<string, MultitileObjectDefinition>(definitions.Count);
        foreach (var def in definitions)
            if (def != null) _lookup[def.name] = def;
    }

    public MultitileObjectDefinition Get(string defName)
    {
        _lookup.TryGetValue(defName, out var def);
        return def;
    }

    public MultitileObjectDefinition GetByTile(TileBase tile)
    {
        foreach (var def in definitions)
            if (def != null && def.editorTile == tile) return def;
        return null;
    }
}