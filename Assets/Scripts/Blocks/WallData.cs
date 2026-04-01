using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WallData", menuName = "World/WallData")]
public class WallData : ScriptableObject
{
    public WallType wallType;
    public string   displayName;
    public TileBase tile;
    public bool     destructible = true;
    public float    hardness     = 1f;
    public ToolType requiredTool = ToolType.None;
    public WallType dropType     = WallType.None;
    public int      dropAmount   = 1;
}