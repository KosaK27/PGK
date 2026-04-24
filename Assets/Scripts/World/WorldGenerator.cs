using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenerator", menuName = "World/WorldGenerator")]
public class WorldGenerator : ScriptableObject
{
    [Header("Seed")]
    public int seed = 0;
    public bool randomSeed = true;

    [Header("Biomes")]
    public BiomeData defaultBiome;
    public List<BiomeData> biomes = new();

    [Header("Caves")]
    public float caveScale = 0.05f;
    public float caveThreshold = 0.45f;
    public int caveStartDepth = 10;

    [Header("Surface Ravines")]
    public float ravineChance = 0.006f;
    public int ravineMinWidth = 3;
    public int ravineMaxWidth = 10;
    public int ravineMinDepth = 15;
    public int ravineMaxDepth = 35;

    [Header("Hills")]
    public float hillScale = 0.025f;
    public float hillHeight = 55f;
    public float hillThreshold = 0.40f;
    public int hillSafeZone = 200;
    public float hillEntranceChance = 0.6f;
    public int hillEntranceMinSize = 4;
    public int hillEntranceMaxSize = 9;

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
        public WallType wallType;
    }

    public void Generate(WorldData world)
    {
        if (randomSeed) seed = Random.Range(0, 999999);
        Random.InitState(seed);

        int width = world.Width;
        int height = world.Height;

        var biomeMap = GenerateBiomeMap(width);
        var baseSurface = GenerateSurface(width, height, biomeMap);
        var hillMap = GenerateHillMap(width, height);
        var surface = MergeSurfaces(baseSurface, hillMap, width, height);

        FillTerrain(world, width, height, biomeMap, surface);
        GenerateWalls(world, width, height);
        CarveRavines(world, width, height, surface);
        GenerateCaves(world, width, height, surface);
        CarveHillEntrances(world, width, height, surface, hillMap);
        GenerateOres(world, width, height, biomeMap, surface);
        GenerateStructures(world, width, height, biomeMap, surface);
        GenerateTrees(world, width, height, biomeMap, surface);
    }

    private int[] GenerateHillMap(int width, int height)
    {
        var hillBoost = new int[width];
        int centerX = width / 2;
        float ox = seed * 0.17f + 500f;

        for (int x = 0; x < width; x++)
        {
            int distFromCenter = Mathf.Abs(x - centerX);
            float safeBlend = Mathf.Clamp01((distFromCenter - hillSafeZone) / 150f);

            float n1 = Mathf.PerlinNoise((x + ox) * hillScale, seed * 0.3f);
            float n2 = Mathf.PerlinNoise((x + ox) * hillScale * 0.4f, seed * 0.6f + 200f);
            float combined = n1 * 0.6f + n2 * 0.4f;

            if (combined > hillThreshold)
            {
                float t = (combined - hillThreshold) / (1f - hillThreshold);
                hillBoost[x] = Mathf.RoundToInt(t * hillHeight * safeBlend);
            }
        }

        var smoothed = new int[width];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int x = 0; x < width; x++)
            {
                int distFromCenter = Mathf.Abs(x - centerX);
                int r = distFromCenter < hillSafeZone ? 5 : 2;
                int sum = 0, count = 0;
                for (int dx = -r; dx <= r; dx++)
                {
                    int nx = Mathf.Clamp(x + dx, 0, width - 1);
                    sum += (pass == 0 ? hillBoost : smoothed)[nx];
                    count++;
                }
                smoothed[x] = sum / count;
            }
            if (pass < 1) System.Array.Copy(smoothed, hillBoost, width);
        }

        return smoothed;
    }

    private int[] MergeSurfaces(int[] baseSurface, int[] hillMap, int width, int height)
    {
        var result = new int[width];
        for (int x = 0; x < width; x++)
            result[x] = Mathf.Min(baseSurface[x] + hillMap[x], height - 10);
        return result;
    }

    private void CarveHillEntrances(WorldData world, int width, int height, int[] surface, int[] hillMap)
    {
        int centerX = width / 2;

        for (int x = 5; x < width - 5; x++)
        {
            if (hillMap[x] < 20) continue;
            int distFromCenter = Mathf.Abs(x - centerX);
            if (distFromCenter < hillSafeZone) continue;
            if (Random.value > hillEntranceChance) continue;

            int entranceSize = Random.Range(hillEntranceMinSize, hillEntranceMaxSize + 1);
            int surfaceY = surface[x];

            int entranceY = Random.Range(
                surfaceY - hillMap[x] + 4,
                surfaceY - entranceSize / 2
            );
            entranceY = Mathf.Clamp(entranceY, 5, height - 5);

            for (int ey = entranceY - entranceSize / 2; ey <= entranceY + entranceSize / 2; ey++)
            for (int ex = x - entranceSize * 2; ex <= x + 3; ex++)
            {
                if (!world.InBounds(ex, ey)) continue;
                float dy = Mathf.Abs(ey - entranceY) / (entranceSize / 2f);
                if (dy > 1f) continue;
                float halfW = entranceSize * Mathf.Sqrt(1f - dy * dy);
                if (ex >= x - halfW && ex <= x + 2)
                {
                    world.SetBlock(ex, ey, BlockType.Air);
                    world.SetWall(ex, ey, WallType.None);
                }
            }

            x += entranceSize + 5;
        }
    }

    private void CarveRavines(WorldData world, int width, int height, int[] surface)
    {
        int x = 10;
        while (x < width - 10)
        {
            if (Random.value < ravineChance)
            {
                int ravWidth = Random.Range(ravineMinWidth, ravineMaxWidth + 1);
                int ravDepth = Random.Range(ravineMinDepth, ravineMaxDepth + 1);

                for (int rx = x; rx < Mathf.Min(x + ravWidth, width - 1); rx++)
                {
                    float edge = (float)(rx - x) / ravWidth;
                    float taper = Mathf.Sin(edge * Mathf.PI);
                    int localDepth = Mathf.RoundToInt(ravDepth * taper);
                    int top = surface[rx];
                    int bottom = Mathf.Max(top - localDepth, 2);

                    for (int ry = bottom; ry <= top + 1; ry++)
                    {
                        world.SetBlock(rx, ry, BlockType.Air);
                        world.SetWall(rx, ry, WallType.None);
                    }
                }

                x += ravWidth + Random.Range(20, 60);
            }
            else
            {
                x++;
            }
        }
    }

    private void GenerateWalls(WorldData world, int width, int height)
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
        var map = new BiomeData[width];
        var sections = new List<(int end, BiomeData biome)>();

        int centerStart = width / 4;
        int centerEnd = width * 3 / 4;

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
                int w = Random.Range(biome.minWidth, biome.maxWidth);
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
        var heights = new int[width];
        int baseHeight = height / 2;
        float offsetX = seed * 0.1f;

        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            float ox = (x + offsetX) * biome.terrainScale;

            float noise = Mathf.PerlinNoise(ox, 0f)
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
            int top = surface[x];

            for (int y = 0; y < height; y++)
            {
                if (y > top) { world.SetBlock(x, y, BlockType.Air); continue; }

                int depth = top - y;

                BlockType block = BlockType.Air;
                foreach (var layer in biome.verticalLayers)
                    if (depth >= layer.startDepth && depth < layer.endDepth)
                        { block = layer.blockType; break; }

                if (block != BlockType.Air) { world.SetBlock(x, y, block); continue; }

                if (depth == 0) world.SetBlock(x, y, biome.surfaceBlock);
                else if (depth < biome.subsurfaceDepth) world.SetBlock(x, y, biome.subsurfaceBlock);
                else world.SetBlock(x, y, biome.deepBlock);
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

        ConnectCaves(world, width, height, surface);
    }

    private void ConnectCaves(WorldData world, int width, int height, int[] surface)
    {
        int numWorms = (width * height) / 20000;
        int numLongWorms = Mathf.Max(1, numWorms / 15);
        int minDepth = caveStartDepth + 4;
        int segmentWidth = width / Mathf.Max(numWorms, 1);

        var longWormIndices = new HashSet<int>();
        while (longWormIndices.Count < numLongWorms)
            longWormIndices.Add(Random.Range(0, numWorms));

        for (int i = 0; i < numWorms; i++)
        {
            int startX = segmentWidth * i + Random.Range(0, segmentWidth);
            startX = Mathf.Clamp(startX, 10, width - 10);

            int maxY = surface[startX] - minDepth;
            if (maxY <= 5) continue;
            int startY = Random.Range(5, maxY);

            bool isLong = longWormIndices.Contains(i);
            float wx = startX;
            float wy = startY;
            float angle = Random.Range(-0.3f, 0.3f) * Mathf.PI;
            if (Random.value > 0.5f) angle += Mathf.PI;
            int radius = isLong ? Random.Range(4, 6) : Random.Range(4, 8);
            int length = isLong ? Random.Range(400, 700) : Random.Range(100, 150);
            float angleNoise = seed * 0.01f + i * 100f;
            float smoothAngle = angle;

            for (int step = 0; step < length; step++)
            {
                float noiseVal = Mathf.PerlinNoise(wx * 0.02f + angleNoise, wy * 0.02f + angleNoise);
                float targetAngle = angle + (noiseVal - 0.5f) * (isLong ? 0.1f : 0.2f);

                float verticalBias = (wy / height - 0.4f) * 0.1f;
                targetAngle += verticalBias;

                float horizontalPull = Mathf.Sin(targetAngle) * 0.05f;
                targetAngle -= horizontalPull;

                smoothAngle = Mathf.LerpAngle(smoothAngle, targetAngle, 0.15f);
                angle = smoothAngle;

                wx += Mathf.Cos(angle) * 1.2f;
                wy += Mathf.Sin(angle) * 0.8f;

                int ix = Mathf.RoundToInt(wx);
                int iy = Mathf.RoundToInt(wy);

                if (ix < 2 || ix >= width - 2 || iy < 2 || iy >= height - 2) break;
                if (surface[Mathf.Clamp(ix, 0, width - 1)] - iy < minDepth) continue;

                for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int bx = ix + dx;
                    int by = iy + dy;
                    if (!world.InBounds(bx, by)) continue;
                    if (surface[Mathf.Clamp(bx, 0, width - 1)] - by < caveStartDepth) continue;
                    world.SetBlock(bx, by, BlockType.Air);
                }
            }
        }
    }

    private void GenerateOres(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        for (int x = 0; x < width; x++)
        {
            var biome = biomeMap[x];
            int top = surface[x];

            for (int i = 0; i < biome.ores.Count; i++)
            {
                var ore = biome.ores[i];
                float ox = seed * 0.5f + i * 1000f;
                float oy = seed * 0.9f + i * 1000f;

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
        int lastTreeX = -3;

        for (int x = 2; x < width - 2; x++)
        {
            if (x - lastTreeX < 3) continue;
            if (Random.value > treeChance) continue;

            int groundY = surface[x] + 1;

            if (world.GetBlock(x, surface[x]) != BlockType.Grass)
                continue;

            int treeHeight = Random.Range(minTreeHeight, maxTreeHeight + 1);

            for (int y = 0; y < treeHeight; y++)
                world.SetBlock(x, groundY + y, trunkBlock);

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

            lastTreeX = x;
        }
    }
}