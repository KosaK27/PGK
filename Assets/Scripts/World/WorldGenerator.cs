using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenerator", menuName = "World/WorldGenerator")]
public class WorldGenerator : ScriptableObject
{
    [Header("Seed")]
    public int  seed       = 0;
    public bool randomSeed = true;

    [Header("Biomes")]
    public BiomeData       defaultBiome;
    public List<BiomeData> biomes = new();

    [Header("Caves")]
    public float caveScale      = 0.05f;
    public float caveThreshold  = 0.45f;
    public int   caveStartDepth = 10;

    [Header("Trees")]
    public float treeChance = 0.08f;
    public int minTreeHeight = 4;
    public int maxTreeHeight = 7;
    public BlockType trunkBlock = BlockType.Log;
    public BlockType leafBlock = BlockType.Leaves;

    [Header("Wall Generation")]
    public List<BlockToWall> wallMappings = new();

    [System.Serializable]
    public class BlockToWall
    {
        public BlockType blockType;
        public WallType  wallType;
    }

    public void Generate(WorldData world)
    {
        if (randomSeed) seed = Random.Range(0, 999999);
        Random.InitState(seed);

        int width  = world.Width;
        int height = world.Height;

        var biomeMap = GenerateBiomeMap(width);
        var surface  = GenerateSurface(width, height, biomeMap);

        FillTerrain(world, width, height, biomeMap, surface);
        GenerateWalls(world, width, height, surface);
        GenerateCaves(world, width, height, surface);
        GenerateOres(world, width, height, biomeMap, surface);
        GenerateStructures(world, width, height, biomeMap, surface);
        GenerateTrees(world, width, height, biomeMap, surface); 
    }

    private void GenerateWalls(WorldData world, int width, int height, int[] surface)
    {
        var mappingDict = new Dictionary<BlockType, WallType>();
        foreach (var m in wallMappings)
            mappingDict[m.blockType] = m.wallType;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var block = world.GetBlock(x, y);
            if (block == BlockType.Air) continue;
            if (mappingDict.TryGetValue(block, out var wallType))
                world.SetWall(x, y, wallType);
        }
    }

    private BiomeData[] GenerateBiomeMap(int width)
    {
        var map      = new BiomeData[width];
        var sections = new List<(int end, BiomeData biome)>();

        int centerStart = width / 4;
        int centerEnd   = width * 3 / 4;

        var counts = new Dictionary<BiomeData, int>();
        foreach (var b in biomes) counts[b] = 0;

        BiomeData PickBiome()
        {
            var available = biomes.FindAll(b => counts[b] < b.maxCount);
            if (available.Count == 0) return defaultBiome;
            return available[Random.Range(0, available.Count)];
        }

        void PlaceSections(int from, int to)
        {
            var guaranteed = new List<BiomeData>();
            foreach (var b in biomes)
                for (int n = counts[b]; n < b.minCount; n++)
                    guaranteed.Add(b);

            for (int i = guaranteed.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (guaranteed[i], guaranteed[j]) = (guaranteed[j], guaranteed[i]);
            }

            int x = from;
            int gIdx = 0;

            while (x < to)
            {
                var biome = gIdx < guaranteed.Count ? guaranteed[gIdx++] : PickBiome();
                int w     = Random.Range(biome.minWidth, biome.maxWidth);
                sections.Add((Mathf.Min(x + w, to), biome));
                counts[biome]++;
                x += w;
            }
        }

        PlaceSections(0, centerStart);
        sections.Add((centerEnd, defaultBiome));
        PlaceSections(centerEnd, width);

        int idx = 0;
        for (int i = 0; i < width; i++)
        {
            while (idx < sections.Count - 1 && i >= sections[idx].end) idx++;
            map[i] = sections[idx].biome;
        }

        return map;
    }

    private int[] GenerateSurface(int width, int height, BiomeData[] biomeMap)
    {
        var   heights    = new int[width];
        int   baseHeight = height / 2;
        float offsetX    = seed * 0.1f;

        for (int x = 0; x < width; x++)
        {
            var   biome = biomeMap[x];
            float ox    = (x + offsetX) * biome.terrainScale;

            float noise = Mathf.PerlinNoise(ox,       0f)
                        + Mathf.PerlinNoise(ox * 2f, 100f) * 0.5f
                        + Mathf.PerlinNoise(ox * 4f, 200f) * 0.25f;

            heights[x] = baseHeight + (int)biome.terrainElevation +
                         Mathf.RoundToInt((noise / 1.75f - 0.5f) * biome.terrainHeight);
        }

        return heights;
    }

    private void FillTerrain(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            int top   = surface[x];

            for (int y = 0; y < height; y++)
            {
                if (y > top) { world.SetBlock(x, y, BlockType.Air); continue; }

                int depth = top - y;

                BlockType block = BlockType.Air;
                foreach (var layer in biome.verticalLayers)
                    if (depth >= layer.startDepth && depth < layer.endDepth)
                        { block = layer.blockType; break; }

                if (block != BlockType.Air) { world.SetBlock(x, y, block); continue; }

                if      (depth == 0)                    world.SetBlock(x, y, biome.surfaceBlock);
                else if (depth < biome.subsurfaceDepth) world.SetBlock(x, y, biome.subsurfaceBlock);
                else                                    world.SetBlock(x, y, biome.deepBlock);
            }
        }
    }

    private void GenerateCaves(WorldData world, int width, int height, int[] surface)
    {
        float ox = seed * 0.3f;
        float oy = seed * 0.7f;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (world.GetBlock(x, y) == BlockType.Air) continue;
            if (surface[x] - y < caveStartDepth) continue;

            if (Mathf.PerlinNoise((x + ox) * caveScale, (y + oy) * caveScale) < caveThreshold)
                world.SetBlock(x, y, BlockType.Air);
        }
    }

    private void GenerateOres(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            int top   = surface[x];

            for (int i = 0; i < biome.ores.Count; i++)
            {
                var   ore = biome.ores[i];
                float ox  = seed * 0.5f + i * 1000f;
                float oy  = seed * 0.9f + i * 1000f;

                for (int y = 0; y < height; y++)
                {
                    int depth = top - y;
                    if (depth < ore.minDepth || depth > ore.maxDepth) continue;
                    if (world.GetBlock(x, y) != biome.deepBlock) continue;

                    if (Mathf.PerlinNoise((x + ox) * 0.02f, (y + oy) * 0.02f) < ore.rarity) continue;
                    if (Mathf.PerlinNoise((x + ox) * 0.08f, (y + oy) * 0.08f) < 1f - ore.veinSize) continue;

                    world.SetBlock(x, y, ore.blockType);
                }
            }
        }
    }

    private void GenerateStructures(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        var counts = new Dictionary<StructureTemplate, int>();

        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            if (biome.structures.Count == 0 || Random.value > biome.structureChance) continue;

            var template = biome.structures[Random.Range(0, biome.structures.Count)];
            if (template?.blocks == null) continue;

            if (!counts.ContainsKey(template)) counts[template] = 0;
            if (counts[template] >= template.maxCount) continue;

            PlaceStructure(world, template, x, surface[x]);
            counts[template]++;
        }

        foreach (var biome in biomes)
        foreach (var template in biome.structures)
        {
            if (template?.blocks == null) continue;
            if (!counts.ContainsKey(template)) counts[template] = 0;
            if (counts[template] >= template.minCount) continue;

            var cols = new List<int>();
            for (int x = 0; x < width; x++)
                if (biomeMap[x] == biome) cols.Add(x);

            for (int i = cols.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cols[i], cols[j]) = (cols[j], cols[i]);
            }

            while (counts[template] < template.minCount && cols.Count > 0)
            {
                PlaceStructure(world, template, cols[0], surface[cols[0]]);
                cols.RemoveAt(0);
                counts[template]++;
            }
        }
    }

    private void PlaceStructure(WorldData world, StructureTemplate template, int originX, int originY)
    {
        for (int y = 0; y < template.size.y; y++)
        for (int x = 0; x < template.size.x; x++)
        {
            var block = template.GetBlock(x, y);
            if (block != BlockType.Air)
                world.SetBlock(originX + x, originY + y, block);
        }
    }

    private void GenerateTrees(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        for (int x = 2; x < width - 2; x++)
        {
            if (Random.value > treeChance) continue;

            int groundY = surface[x] + 1;

            if (world.GetBlock(x, surface[x]) != BlockType.Grass)
                continue;

            int treeHeight = Random.Range(minTreeHeight, maxTreeHeight + 1);

            for (int y = 0; y < treeHeight; y++)
            {
                world.SetBlock(x, groundY + y, trunkBlock);
            }

            int leafStart = groundY + treeHeight - 2;

            for (int lx = -2; lx <= 2; lx++)
            for (int ly = 0; ly <= 3; ly++)
            {
                int ax = x + lx;
                int ay = leafStart + ly;

                if (Mathf.Abs(lx) + ly > 3) continue;

                if (world.GetBlock(ax, ay) == BlockType.Air)
                    world.SetBlock(ax, ay, leafBlock);
            }
        }
    }
}