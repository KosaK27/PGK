// Assets/Scripts/World/Generation/OreData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "OreData", menuName = "World/OreData")]
public class OreData : ScriptableObject
{
    public BlockType blockType;
    public float frequency = 0.05f;
    public float threshold = 0.6f;
    public int minDepth = 10;
    public int maxDepth = 100;
    public float noiseScale = 0.1f;
}