using System;
using System.Collections;
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
    public float ravineChance = 0.002f;
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

    [Header("Lakes")]
    public int minLakeWidth = 8;
    public int maxLakeWidth = 32;
    public int minLakeDepth = 5;
    public int maxLakeDepth = 10;
    public int lakeSpacing = 200;

    [Header("Structures")]
    public int structureMinSpacing = 150;

    [Header("Wall Generation")]
    public List<BlockToWall> wallMappings = new();

    private int _offsetX;
    private int _offsetY;
    private List<int> _lakeCenters = new();
    private List<PendingObjectPlacement> _pendingPlacements = new();

    private float waitTime = 0f;

    public struct PendingObjectPlacement
    {
        public MultitileObjectDefinition definition;
        public Vector2Int worldPos;
        public bool fillWithLoot;
    }

    [Serializable]
    public class BlockToWall
    {
        public BlockType blockType;
        public WallType wallType;
    }

    public void Generate(WorldData world, WorldGenerationProgress progress = null)
    {
        if (randomSeed) seed = UnityEngine.Random.Range(0, 999999);
        UnityEngine.Random.InitState(seed);
        _lakeCenters.Clear();
        _pendingPlacements.Clear();

        int width = world.Width;
        int height = world.Height;
        _offsetX = -world.Width / 2;
        _offsetY = -world.Height / 2;

        progress?.Report(0f, "Generating biomes...");
        var biomeMap = GenerateBiomeMap(width);
        progress?.Report(0.05f, "Generating surface...");
        var baseSurface = GenerateSurface(width, height, biomeMap);
        progress?.Report(0.1f, "Generating hills...");
        var hillMap = GenerateHillMap(width, height);
        var surface = MergeSurfaces(baseSurface, hillMap, width, height);
        progress?.Report(0.2f, "Filling terrain...");
        FillTerrain(world, width, height, biomeMap, surface);
        progress?.Report(0.35f, "Generating walls...");
        GenerateWalls(world, width, height);
        progress?.Report(0.4f, "Carving ravines...");
        CarveRavines(world, width, height, surface);
        progress?.Report(0.5f, "Carving caves...");
        GenerateCaves(world, width, height, surface);
        progress?.Report(0.65f, "Carving hill entrances...");
        CarveHillEntrances(world, width, height, surface, hillMap);
        progress?.Report(0.7f, "Generating lakes...");
        GenerateLakes(world, width, height, surface);
        progress?.Report(0.75f, "Generating ores...");
        GenerateOres(world, width, height, biomeMap, surface);
        progress?.Report(0.85f, "Generating trees...");
        GenerateTrees(world, width, height, biomeMap, surface);
        progress?.Report(0.9f, "Generating structures...");
        GenerateStructures(world, width, height, biomeMap, surface);
        progress?.Report(1f, "Done.");
    }

    public IEnumerator GenerateCoroutine(WorldData world, Action<float, string> onProgress)
    {
        if (randomSeed) seed = UnityEngine.Random.Range(0, 999999);
        UnityEngine.Random.InitState(seed);
        _lakeCenters.Clear();
        _pendingPlacements.Clear();

        int width = world.Width;
        int height = world.Height;
        _offsetX = -world.Width / 2;
        _offsetY = -world.Height / 2;

        onProgress(0f, "Generating biomes...");
        yield return new WaitForSeconds(waitTime);
        var biomeMap = GenerateBiomeMap(width);

        onProgress(0.05f, "Generating surface...");
        yield return new WaitForSeconds(waitTime);
        var baseSurface = GenerateSurface(width, height, biomeMap);

        onProgress(0.1f, "Generating hills...");
        yield return new WaitForSeconds(waitTime);
        var hillMap = GenerateHillMap(width, height);
        var surface = MergeSurfaces(baseSurface, hillMap, width, height);

        onProgress(0.2f, "Filling terrain...");
        yield return new WaitForSeconds(waitTime);
        FillTerrain(world, width, height, biomeMap, surface);

        onProgress(0.35f, "Generating walls...");
        yield return new WaitForSeconds(waitTime);
        GenerateWalls(world, width, height);

        onProgress(0.4f, "Carving ravines...");
        yield return new WaitForSeconds(waitTime);
        CarveRavines(world, width, height, surface);

        onProgress(0.5f, "Carving caves...");
        yield return new WaitForSeconds(waitTime);
        GenerateCaves(world, width, height, surface);

        onProgress(0.65f, "Carving hill entrances...");
        yield return new WaitForSeconds(waitTime);
        CarveHillEntrances(world, width, height, surface, hillMap);

        onProgress(0.7f, "Generating lakes...");
        yield return new WaitForSeconds(waitTime);
        GenerateLakes(world, width, height, surface);

        onProgress(0.75f, "Generating ores...");
        yield return new WaitForSeconds(waitTime);
        GenerateOres(world, width, height, biomeMap, surface);

        onProgress(0.85f, "Generating trees...");
        yield return new WaitForSeconds(waitTime);
        GenerateTrees(world, width, height, biomeMap, surface);

        onProgress(0.9f, "Generating structures...");
        yield return new WaitForSeconds(waitTime);
        GenerateStructures(world, width, height, biomeMap, surface);

        onProgress(1f, "Done.");

        WorldDataTransfer.PendingPlacements = new List<PendingObjectPlacement>(_pendingPlacements);
    }

    private void GenerateLakes(WorldData world, int width, int height, int[] surface)
    {
        int attempts = width / 100;
        WallType dirtWall = WallType.None;
        foreach (var m in wallMappings) if (m.blockType == BlockType.Dirt) dirtWall = m.wallType;
        for (int i = 0; i < attempts; i++)
        {
            int startX = UnityEngine.Random.Range(20, width - 20);
            int surfaceY = surface[startX];
            int lWidth = UnityEngine.Random.Range(10, 20);
            int lDepth = UnityEngine.Random.Range(4, 7);
            int halfW = lWidth / 2;
            int leftEdgeX = Mathf.Clamp(startX - halfW, 0, width - 1);
            int rightEdgeX = Mathf.Clamp(startX + halfW, 0, width - 1);
            if (Mathf.Abs(surface[leftEdgeX] - surface[rightEdgeX]) > 3) continue;
            bool caveBelow = false;
            for (int x = startX - halfW; x <= startX + halfW; x += 3)
            {
                for (int y = surfaceY - 4; y >= surfaceY - 8; y--)
                {
                    if (world.InBounds(x, y) && world.GetBlock(x, y) == BlockType.Air) { caveBelow = true; break; }
                }
                if (caveBelow) break;
            }
            if (caveBelow) continue;
            bool tooClose = false;
            foreach (int c in _lakeCenters) if (Mathf.Abs(startX - c) < 30) tooClose = true;
            if (tooClose) continue;
            _lakeCenters.Add(startX);
            int waterLevelY = surfaceY - 1;
            for (int x = startX - halfW; x <= startX + halfW; x++)
            {
                if (!world.InBounds(x, 0)) continue;
                float dist = Mathf.Abs(x - startX) / (float)halfW;
                int currentDepth = Mathf.RoundToInt(Mathf.Sqrt(Mathf.Clamp01(1 - dist * dist)) * lDepth);
                int bottomY = waterLevelY - currentDepth;
                for (int y = waterLevelY + 8; y >= bottomY - 5; y--)
                {
                    if (!world.InBounds(x, y)) continue;
                    if (y > waterLevelY) { world.SetBlock(x, y, BlockType.Air); world.SetWall(x, y, WallType.None); }
                    else if (y <= waterLevelY && y > bottomY) { world.SetBlock(x, y, BlockType.Water); world.SetWall(x, y, dirtWall); }
                    else if (y == bottomY) { world.SetBlock(x, y, BlockType.Sand); world.SetWall(x, y, dirtWall); }
                    else if (y < bottomY && y >= bottomY - 2) { world.SetBlock(x, y, BlockType.Dirt); }
                    else if (y < bottomY - 2)
                    {
                        if (world.GetBlock(x, y) == BlockType.Air || world.GetBlock(x, y) == BlockType.Dirt) world.SetBlock(x, y, BlockType.Stone);
                        break;
                    }
                }
            }
        }
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
        for (int x = 0; x < width; x++) result[x] = Mathf.Min(baseSurface[x] + hillMap[x], height - 10);
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
            if (UnityEngine.Random.value > hillEntranceChance) continue;
            int entranceSize = UnityEngine.Random.Range(hillEntranceMinSize, hillEntranceMaxSize + 1);
            int surfaceY = surface[x];
            int entranceY = UnityEngine.Random.Range(surfaceY - hillMap[x] + 4, surfaceY - entranceSize / 2);
            entranceY = Mathf.Clamp(entranceY, 5, height - 5);
            for (int ey = entranceY - entranceSize / 2; ey <= entranceY + entranceSize / 2; ey++)
                for (int ex = x - entranceSize * 2; ex <= x + 3; ex++)
                {
                    if (!world.InBounds(ex, ey)) continue;
                    float dy = Mathf.Abs(ey - entranceY) / (entranceSize / 2f);
                    if (dy > 1f) continue;
                    float halfW = entranceSize * Mathf.Sqrt(1f - dy * dy);
                    if (ex >= x - halfW && ex <= x + 2) { world.SetBlock(ex, ey, BlockType.Air); world.SetWall(ex, ey, WallType.None); }
                }
            x += entranceSize + 5;
        }
    }

    private void CarveRavines(WorldData world, int width, int height, int[] surface)
    {
        int x = 10;
        while (x < width - 10)
        {
            if (UnityEngine.Random.value < ravineChance)
            {
                int ravWidth = UnityEngine.Random.Range(ravineMinWidth, ravineMaxWidth + 1);
                int ravDepth = UnityEngine.Random.Range(ravineMinDepth, ravineMaxDepth + 1);
                for (int rx = x; rx < Mathf.Min(x + ravWidth, width - 1); rx++)
                {
                    float edge = (float)(rx - x) / ravWidth;
                    float taper = Mathf.Sin(edge * Mathf.PI);
                    int localDepth = Mathf.RoundToInt(ravDepth * taper);
                    int top = surface[rx];
                    int bottom = Mathf.Max(top - localDepth, 2);
                    for (int ry = bottom; ry <= top + 1; ry++) { world.SetBlock(rx, ry, BlockType.Air); world.SetWall(rx, ry, WallType.None); }
                }
                x += ravWidth + UnityEngine.Random.Range(20, 60);
            }
            else x++;
        }
    }

    private void GenerateWalls(WorldData world, int width, int height)
    {
        var mappingDict = new Dictionary<BlockType, WallType>();
        foreach (var m in wallMappings) mappingDict[m.blockType] = m.wallType;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var block = world.GetBlock(x, y);
                if (block == BlockType.Air) continue;
                if (mappingDict.TryGetValue(block, out var wallType)) world.SetWall(x, y, wallType);
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
            return available[UnityEngine.Random.Range(0, available.Count)];
        }
        void PlaceSections(int from, int to)
        {
            var guaranteed = new List<BiomeData>();
            foreach (var b in biomes) for (int n = counts[b]; n < b.minCount; n++) guaranteed.Add(b);
            for (int i = guaranteed.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (guaranteed[i], guaranteed[j]) = (guaranteed[j], guaranteed[i]);
            }
            int x = from;
            int gIdx = 0;
            while (x < to)
            {
                var biome = gIdx < guaranteed.Count ? guaranteed[gIdx++] : PickBiome();
                int w = UnityEngine.Random.Range(biome.minWidth, biome.maxWidth);
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
            float noise = Mathf.PerlinNoise(ox, 0f) + Mathf.PerlinNoise(ox * 2f, 100f) * 0.5f + Mathf.PerlinNoise(ox * 4f, 200f) * 0.25f;
            heights[x] = baseHeight + (int)biome.terrainElevation + Mathf.RoundToInt((noise / 1.75f - 0.5f) * biome.terrainHeight);
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
                    if (depth >= layer.startDepth && depth < layer.endDepth) { block = layer.blockType; break; }
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
                if (Mathf.PerlinNoise((x + ox) * caveScale, (y + oy) * caveScale) < caveThreshold) world.SetBlock(x, y, BlockType.Air);
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
        while (longWormIndices.Count < numLongWorms) longWormIndices.Add(UnityEngine.Random.Range(0, numWorms));
        for (int i = 0; i < numWorms; i++)
        {
            int startX = segmentWidth * i + UnityEngine.Random.Range(0, segmentWidth);
            startX = Mathf.Clamp(startX, 10, width - 10);
            int maxY = surface[startX] - minDepth;
            if (maxY <= 5) continue;
            int startY = UnityEngine.Random.Range(5, maxY);
            bool isLong = longWormIndices.Contains(i);
            float wx = startX, wy = startY;
            float angle = UnityEngine.Random.Range(-0.3f, 0.3f) * Mathf.PI;
            if (UnityEngine.Random.value > 0.5f) angle += Mathf.PI;
            int radius = isLong ? UnityEngine.Random.Range(4, 6) : UnityEngine.Random.Range(4, 8);
            int length = isLong ? UnityEngine.Random.Range(400, 700) : UnityEngine.Random.Range(100, 150);
            float angleNoise = seed * 0.01f + i * 100f;
            float smoothAngle = angle;
            for (int step = 0; step < length; step++)
            {
                float noiseVal = Mathf.PerlinNoise(wx * 0.02f + angleNoise, wy * 0.02f + angleNoise);
                float targetAngle = angle + (noiseVal - 0.5f) * (isLong ? 0.1f : 0.2f);
                targetAngle += (wy / height - 0.4f) * 0.1f;
                targetAngle -= Mathf.Sin(targetAngle) * 0.05f;
                smoothAngle = Mathf.LerpAngle(smoothAngle, targetAngle, 0.15f);
                angle = smoothAngle;
                wx += Mathf.Cos(angle) * 1.2f;
                wy += Mathf.Sin(angle) * 0.8f;
                int ix = Mathf.RoundToInt(wx), iy = Mathf.RoundToInt(wy);
                if (ix < 2 || ix >= width - 2 || iy < 2 || iy >= height - 2) break;
                if (surface[Mathf.Clamp(ix, 0, width - 1)] - iy < minDepth) continue;
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int bx = ix + dx, by = iy + dy;
                        if (world.InBounds(bx, by) && surface[Mathf.Clamp(bx, 0, width - 1)] - by >= caveStartDepth) world.SetBlock(bx, by, BlockType.Air);
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
                float ox = seed * 0.5f + i * 1000f, oy = seed * 0.9f + i * 1000f;
                for (int y = 0; y < height; y++)
                {
                    int depth = top - y;
                    if (depth < ore.minDepth || depth > ore.maxDepth || world.GetBlock(x, y) != biome.deepBlock) continue;
                    if (Mathf.PerlinNoise((x + ox) * 0.02f, (y + oy) * 0.02f) < ore.rarity) continue;
                    if (Mathf.PerlinNoise((x + ox) * 0.08f, (y + oy) * 0.08f) < 1f - ore.veinSize) continue;
                    world.SetBlock(x, y, ore.blockType);
                }
            }
        }
    }

    private void GenerateStructures(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        var placedRanges = new List<(int from, int to)>();
        GenerateStructuresInRange(world, width, height, biomeMap, surface, 0, width / 2, placedRanges);
        GenerateStructuresInRange(world, width, height, biomeMap, surface, width / 2, width, placedRanges);
    }

    private void GenerateStructuresInRange(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface, int fromX, int toX, List<(int from, int to)> placedRanges)
    {
        var counts = new Dictionary<StructureTemplate, int>();
        int minSpacing = structureMinSpacing;
        bool Overlaps(int x, int structWidth)
        {
            foreach (var r in placedRanges)
                if (x < r.to + minSpacing && x + structWidth > r.from - minSpacing) return true;
            return false;
        }
        bool IsGrounded(WorldData w, int startX, int startY, int structWidth)
        {
            int solidCount = 0;
            int required = Mathf.CeilToInt(structWidth * 0.5f);
            for (int x = startX; x < startX + structWidth; x++)
            {
                if (!w.InBounds(x, startY)) continue;
                if (w.GetBlock(x, startY) == BlockType.Water) return false;
                if (w.GetBlock(x, startY - 1) != BlockType.Air && w.GetBlock(x, startY - 1) != BlockType.Water) solidCount++;
            }
            return solidCount >= required;
        }
        for (int x = fromX; x < toX; x++)
        {
            var biome = biomeMap[x];
            if (biome.structures.Count == 0 || UnityEngine.Random.value > biome.structureChance) continue;
            var template = biome.structures[UnityEngine.Random.Range(0, biome.structures.Count)];
            if (template?.blocks == null) continue;
            if (Overlaps(x, template.size.x)) continue;
            int groundY = surface[x];
            if (!IsGrounded(world, x, groundY, template.size.x)) continue;
            bool allGrounded = true;
            for (int bx = x; bx < x + template.size.x; bx++)
            {
                if (bx < 0 || bx >= width) { allGrounded = false; break; }
                if (!world.InBounds(bx, surface[bx])) { allGrounded = false; break; }
                if (Mathf.Abs(surface[bx] - groundY) > 2) { allGrounded = false; break; }
                if (world.GetBlock(bx, surface[bx]) == BlockType.Water) { allGrounded = false; break; }
            }
            if (!allGrounded) continue;
            if (!counts.ContainsKey(template)) counts[template] = 0;
            if (counts[template] >= template.maxCount) continue;
            PlaceStructure(world, template, x, groundY + 1);
            counts[template]++;
            placedRanges.Add((x, x + template.size.x));
        }
        foreach (var biome in biomes)
        {
            foreach (var template in biome.structures)
            {
                if (template?.blocks == null) continue;
                if (!counts.ContainsKey(template)) counts[template] = 0;
                int halfMin = template.minCount / 2;
                if (counts[template] >= halfMin) continue;
                var cols = new List<int>();
                for (int x = fromX; x < toX; x++)
                {
                    if (biomeMap[x] != biome) continue;
                    if (Overlaps(x, template.size.x)) continue;
                    int groundY = surface[x];
                    bool flat = true;
                    for (int bx = x; bx < x + template.size.x; bx++)
                    {
                        if (bx < 0 || bx >= width) { flat = false; break; }
                        if (!world.InBounds(bx, surface[bx])) { flat = false; break; }
                        if (Mathf.Abs(surface[bx] - groundY) > 2) { flat = false; break; }
                        if (world.GetBlock(bx, surface[bx]) == BlockType.Water) { flat = false; break; }
                    }
                    if (!flat) continue;
                    bool solidBelow = false;
                    for (int bx = x; bx < x + template.size.x; bx++)
                    {
                        if (world.InBounds(bx, groundY - 1) && world.GetBlock(bx, groundY - 1) != BlockType.Air && world.GetBlock(bx, groundY - 1) != BlockType.Water)
                        {
                            solidBelow = true;
                            break;
                        }
                    }
                    if (solidBelow) cols.Add(x);
                }
                for (int i = cols.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (cols[i], cols[j]) = (cols[j], cols[i]);
                }
                while (counts[template] < halfMin && cols.Count > 0)
                {
                    int cx = cols[0];
                    cols.RemoveAt(0);
                    if (Overlaps(cx, template.size.x)) continue;
                    PlaceStructure(world, template, cx, surface[cx] + 1);
                    placedRanges.Add((cx, cx + template.size.x));
                    counts[template]++;
                }
            }
        }
    }

    private bool IsAreaStable(WorldData world, int startX, int surfaceY, int structWidth)
    {
        int solidCount = 0;
        int required = Mathf.CeilToInt(structWidth * 0.5f);
        for (int x = startX; x < startX + structWidth; x++)
        {
            if (!world.InBounds(x, surfaceY)) continue;
            if (world.GetBlock(x, surfaceY) == BlockType.Water) return false;
            if (world.GetBlock(x, surfaceY - 1) != BlockType.Air && world.GetBlock(x, surfaceY - 1) != BlockType.Water) solidCount++;
        }
        return solidCount >= required;
    }

    private void PlaceStructure(WorldData world, StructureTemplate template, int originX, int originY)
    {
        for (int y = 0; y < template.size.y; y++)
            for (int x = 0; x < template.size.x; x++)
            {
                world.SetBlock(originX + x, originY + y, template.GetBlock(x, y));
                var wall = template.GetWall(x, y);
                if (wall != WallType.None) world.SetWall(originX + x, originY + y, wall);
            }
        foreach (var placement in template.objects)
        {
            if (placement.definition == null) continue;
            var worldPos = new Vector2Int(originX + placement.localOrigin.x + _offsetX, originY + placement.localOrigin.y + _offsetY);
            _pendingPlacements.Add(new PendingObjectPlacement
            {
                definition = placement.definition,
                worldPos = worldPos,
                fillWithLoot = true
            });
        }
    }

    private void GenerateTrees(WorldData world, int width, int height, BiomeData[] biomeMap, int[] surface)
    {
        int lastTreeX = -3;
        for (int x = 2; x < width - 2; x++)
        {
            if (x - lastTreeX < 3 || UnityEngine.Random.value > treeChance || world.GetBlock(x, surface[x]) != BlockType.Grass) continue;
            int groundY = surface[x] + 1;
            int treeHeight = UnityEngine.Random.Range(minTreeHeight, maxTreeHeight + 1);
            for (int y = 0; y < treeHeight; y++) world.SetBlock(x, groundY + y, trunkBlock);
            int leafStart = groundY + treeHeight - 2;
            for (int lx = -2; lx <= 2; lx++)
                for (int ly = 0; ly <= 3; ly++)
                {
                    if (Mathf.Abs(lx) + ly > 3) continue;
                    int ax = x + lx, ay = leafStart + ly;
                    if (world.InBounds(ax, ay) && world.GetBlock(ax, ay) == BlockType.Air) world.SetBlock(ax, ay, leafBlock);
                }
            lastTreeX = x;
        }
    }
}