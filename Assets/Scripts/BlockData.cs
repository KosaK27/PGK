using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockData : ScriptableObject
{
    public BlockType blockType;
    public string displayName;

    [Header("Tile")]
    public TileBase tile;

    [Header("Properties")]
    public bool Destructible = true;
}