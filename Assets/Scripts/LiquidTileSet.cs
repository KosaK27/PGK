using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "LiquidTileSet", menuName = "World/LiquidTileSet")]
public class LiquidTileSet : ScriptableObject
{
    public static LiquidTileSet Instance { get; private set; }

    [SerializeField] private TileBase[] tiles;

    void OnEnable() => Instance = this;

    public TileBase Resolve(byte level, bool falling)
    {
        if (level == 0 || tiles == null || tiles.Length == 0) return null;
        if (falling) return tiles[tiles.Length - 1];
        int index = Mathf.CeilToInt(level / 255f * tiles.Length) - 1;
        return tiles[Mathf.Clamp(index, 0, tiles.Length - 1)];
    }
}