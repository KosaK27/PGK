    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "BiomeData", menuName = "World/BiomeData")]
    public class BiomeData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;

        [Header("Size")]
        public int minWidth = 50;
        public int maxWidth = 150;

        [Header("Count")]
        public int minCount = 1;
        public int maxCount = 3;

        [Header("Surface Blocks")]
        public BlockType surfaceBlock    = BlockType.Grass;
        public BlockType subsurfaceBlock = BlockType.Dirt;
        public BlockType deepBlock       = BlockType.Stone;
        public int       subsurfaceDepth = 5;

        [Header("Terrain Shape")]
        public float terrainScale     = 0.02f;
        public float terrainHeight    = 15f;
        public float terrainElevation = 0f;

        [Header("Vertical Layers")]
        public List<VerticalLayer> verticalLayers = new();

        [Header("Ores")]
        public List<OreData> ores = new();

        [Header("Structures")]
        public List<StructureTemplate> structures    = new();
        [Range(0f, 1f)]
        public float structureChance = 0.05f;
    }

    [System.Serializable]
    public class VerticalLayer
    {
        public BlockType blockType;
        public int       startDepth;
        public int       endDepth;
    }