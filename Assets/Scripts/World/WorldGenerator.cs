using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenerator", menuName = "World/WorldGenerator")]
public class WorldGenerator : ScriptableObject
{
    [Header("Seed")]
    public int seed = 0;
    public bool randomSeed = true;

    [Header("Biomy")]
    public BiomeData defaultBiome;
    public List<BiomeData> biomes = new();

    [Header("Jaskinie")]
    public float caveNoiseScale = 0.05f;
    public float caveThreshold  = 0.45f;
    public int   caveStartDepth = 10;

    public void Generate(WorldData world)
    {
        if (randomSeed) seed = Random.Range(0, 999999);
        Random.InitState(seed);

        int width  = world.Width;
        int height = world.Height;

        var biomeMap      = GenerateBiomeMap(width);
        var surfaceHeights = GenerateSurfaceHeights(width, height, biomeMap);

        FillTerrain(world, width, height, biomeMap, surfaceHeights);
        GenerateCaves(world, width, height, surfaceHeights);
        GenerateOres(world, width, height, biomeMap, surfaceHeights);
        GenerateStructures(world, width, height, biomeMap, surfaceHeights);
    }

    // --- Biomy ---

    private BiomeData[] GenerateBiomeMap(int width)
    {
        // zwracamy BiomeData zamiast BiomeType — koniec z problemem porównania
        var map = new BiomeData[width];
        var sections = new List<(int end, BiomeData biome)>();

        int centerStart = width / 4;
        int centerEnd   = width * 3 / 4;

        // lewa strona
        int x = 0;
        while (x < centerStart)
        {
            var biome = biomes.Count > 0 ? biomes[Random.Range(0, biomes.Count)] : defaultBiome;
            int w = Random.Range(biome.minWidth, biome.maxWidth);
            sections.Add((Mathf.Min(x + w, centerStart), biome));
            x += w;
        }

        // środek — default
        sections.Add((centerEnd, defaultBiome));

        // prawa strona
        x = centerEnd;
        while (x < width)
        {
            var biome = biomes.Count > 0 ? biomes[Random.Range(0, biomes.Count)] : defaultBiome;
            int w = Random.Range(biome.minWidth, biome.maxWidth);
            sections.Add((Mathf.Min(x + w, width), biome));
            x += w;
        }

        int idx = 0;
        for (int i = 0; i < width; i++)
        {
            while (idx < sections.Count - 1 && i >= sections[idx].end)
                idx++;
            map[i] = sections[idx].biome;
        }

        return map;
    }

    // --- Powierzchnia ---

    private int[] GenerateSurfaceHeights(int width, int height, BiomeData[] biomeMap)
    {
        var heights = new int[width];
        int baseHeight = height / 2;
        float offsetX  = seed * 0.1f;

        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];

            float n1 = Mathf.PerlinNoise((x + offsetX) * biome.heightScale,         0f);
            float n2 = Mathf.PerlinNoise((x + offsetX) * biome.heightScale * 2f, 100f) * 0.5f;
            float n3 = Mathf.PerlinNoise((x + offsetX) * biome.heightScale * 4f, 200f) * 0.25f;

            float combined = (n1 + n2 + n3) / 1.75f;
            heights[x] = baseHeight + (int)biome.baseHeightOffset +
                         Mathf.RoundToInt((combined - 0.5f) * biome.heightAmplitude);
        }

        return heights;
    }

    // --- Teren ---

    private void FillTerrain(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surfaceHeights)
    {
        for (int x = 0; x < width; x++)
        {
            var biome   = biomeMap[x];
            int surface = surfaceHeights[x];

            for (int y = 0; y < height; y++)
            {
                if (y > surface) { world.SetBlock(x, y, BlockType.Air); continue; }

                int depth = surface - y;

                BlockType layerBlock = BlockType.Air;
                foreach (var layer in biome.verticalLayers)
                {
                    if (depth >= layer.startDepth && depth < layer.endDepth)
                    { layerBlock = layer.blockType; break; }
                }

                if (layerBlock != BlockType.Air) { world.SetBlock(x, y, layerBlock); continue; }

                if      (depth == 0)                    world.SetBlock(x, y, biome.surfaceBlock);
                else if (depth < biome.subsurfaceDepth) world.SetBlock(x, y, biome.subsurfaceBlock);
                else                                    world.SetBlock(x, y, biome.deepBlock);
            }
        }
    }

    // --- Jaskinie ---

    private void GenerateCaves(WorldData world, int width, int height, int[] surfaceHeights)
    {
        float ox = seed * 0.3f;
        float oy = seed * 0.7f;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (world.GetBlock(x, y) == BlockType.Air) continue;
            if (surfaceHeights[x] - y < caveStartDepth) continue;

            float noise = Mathf.PerlinNoise((x + ox) * caveNoiseScale, (y + oy) * caveNoiseScale);
            if (noise < caveThreshold)
                world.SetBlock(x, y, BlockType.Air);
        }
    }

    // --- Rudy ---

    private void GenerateOres(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surfaceHeights)
    {
        float ox = seed * 0.5f;
        float oy = seed * 0.9f;

        for (int x = 0; x < width; x++)
        {
            var biome   = biomeMap[x];
            int surface = surfaceHeights[x];

            foreach (var ore in biome.ores)
            for (int y = 0; y < height; y++)
            {
                int depth = surface - y;
                if (depth < ore.minDepth || depth > ore.maxDepth) continue;
                if (world.GetBlock(x, y) == BlockType.Air) continue;

                float noise = Mathf.PerlinNoise((x + ox) * ore.noiseScale, (y + oy) * ore.noiseScale);
                if (noise > ore.threshold)
                    world.SetBlock(x, y, ore.blockType);
            }
        }
    }

    // --- Struktury ---

    private void GenerateStructures(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surfaceHeights)
    {
        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            if (biome.structures.Count == 0) continue;
            if (Random.value > biome.structureChance) continue;

            var template = biome.structures[Random.Range(0, biome.structures.Count)];
            if (template?.blocks == null) continue;

            PlaceStructure(world, template, x, surfaceHeights[x]);
        }
    }

    private void PlaceStructure(WorldData world, StructureTemplate template, int originX, int originY)
    {
        for (int y = 0; y < template.size.y; y++)
        for (int x = 0; x < template.size.x; x++)
        {
            var block = template.GetBlock(x, y);
            if (block == BlockType.Air) continue;
            world.SetBlock(originX + x, originY + y, block);
        }
    }
}