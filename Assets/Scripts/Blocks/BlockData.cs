using UnityEngine;
using UnityEngine.Tilemaps;

public class BlockData : ScriptableObject
{
    public BlockType blockType;
    public string displayName;

    [Header("Tile")]
    public TileBase tile;

    [Header("Properties")]
    public bool destructible = true;
    public float hardness = 1f;

    [Header("Drop")]
    public BlockType dropType;
    public int dropAmount = 1;
}