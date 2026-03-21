using UnityEngine;

[CreateAssetMenu(fileName = "OreData", menuName = "World/OreData")]
public class OreData : ScriptableObject
{
    public BlockType blockType;
    public float rarity    = 0.5f;
    public float veinSize  = 0.6f;
    public int   minDepth  = 10;
    public int   maxDepth  = 100;
}