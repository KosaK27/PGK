// Assets/Scripts/World/Generation/BiomeData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "World/BiomeData")]
public class BiomeData : ScriptableObject
{
    [Header("Identyfikacja")]
    public BiomeType biomeType;
    public string displayName;

    [Header("Szerokość biomu")]
    public int minWidth = 50;
    public int maxWidth = 150;

    [Header("Teren — powierzchnia")]
    public BlockType surfaceBlock = BlockType.Grass;
    public BlockType subsurfaceBlock = BlockType.Dirt;
    public BlockType deepBlock = BlockType.Stone;
    public int subsurfaceDepth = 5;    // ile bloków Dirt pod powierzchnią

    [Header("Teren — wysokość")]
    public float heightScale = 0.02f;  // skala Perlin dla powierzchni
    public float heightAmplitude = 15f; // amplituda wzgórz
    public float baseHeightOffset = 0f; // przesunięcie bazowe (+/- od środka)

    [Header("Warstwy pionowe")]
    public List<VerticalLayer> verticalLayers = new();

    [Header("Rudy")]
    public List<OreData> ores = new();

    [Header("Struktury")]
    public List<StructureTemplate> structures = new();
    [Range(0f, 1f)]
    public float structureChance = 0.05f; // szansa na strukturę per kolumnę
}

[System.Serializable]
public class VerticalLayer
{
    public BlockType blockType;
    public int startDepth;  // głębokość od powierzchni
    public int endDepth;
}