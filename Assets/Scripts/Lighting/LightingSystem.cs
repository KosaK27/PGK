using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public readonly struct LightSourceSnapshot
{
    public readonly Vector2 Position;
    public readonly Color Color;
    public readonly float Strength;
    public LightSourceSnapshot(Vector2 position, Color color, float strength)
    {
        Position = position;
        Color = color;
        Strength = strength;
    }
}

public readonly struct BlockSnapshot
{
    public readonly bool IsSolid;
    public readonly bool HasWall;
    public BlockSnapshot(bool isSolid, bool hasWall)
    {
        IsSolid = isSolid;
        HasWall = hasWall;
    }
}

public class LightingSystem : MonoBehaviour
{
    public static LightingSystem Instance { get; private set; }

    [SerializeField] private float lightFalloff = 0.90f;

    [Header("Underground")]
    [SerializeField, Range(0f, 1f)] private float minUndergroundBrightness = 0f;
    [SerializeField, Range(0f, 0.1f)] private float skyLightCutoff = 0.02f;

    [Header("Point Light Shape")]
    [SerializeField] private float lightFalloffExponent = 2.2f;
    [SerializeField] private float lightCutoff = 0.04f;
    [SerializeField, Range(0.5f, 1f)] private float downwardFalloffMult = 0.75f;

    [Header("Point Light Solid Falloff")]
    [SerializeField] private float solidFalloffDay = 0.68f;
    [SerializeField] private float solidFalloffNight = 0.52f;

    [Header("Rebuild Radius")]
    [SerializeField] private int localRebuildRadius = 12;

    private static readonly (int dx, int dy, float dist)[] Dirs = { 
        (1, 0, 1f), (-1, 0, 1f), (0, 1, 1f), (0, -1, 1f),
        (1, 1, 1.4142f), (1, -1, 1.4142f), (-1, 1, 1.4142f), (-1, -1, 1.4142f)
    };

    private RenderTexture _lightMap;
    private Texture2D _lightTex;
    private NativeArray<Color> _lightBuffer;
    private BlockSnapshot[] _snapshotBuffer;

    private int _bufWidth, _bufHeight;
    private int _bufOriginX, _bufOriginY;
    private int _renderedWidth, _renderedHeight;
    private int _renderedOriginX, _renderedOriginY;

    private readonly List<LightSource> _sources = new();
    private readonly HashSet<int> _dirtyWorldColumns = new();
    private readonly HashSet<int> _pendingDirtyColumns = new();

    private bool _fullRebuildRequested = true;
    private bool _pendingFullRebuild = false;
    private Task _rebuildTask;
    private bool _uploadPending;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterSource(LightSource src) 
    { 
        _sources.Add(src); 
        MarkSourceDirty(src);
        _pendingFullRebuild = true; 
    }
    
    public void UnregisterSource(LightSource src) 
    { 
        MarkSourceDirty(src);
        _sources.Remove(src); 
        _pendingFullRebuild = true; 
    }
    
    public void RebuildLightMap() => _pendingFullRebuild = true;
    public void UpdateAmbientOnly() => _pendingFullRebuild = true;

    public void RebuildLightMapAt(int worldX, int worldY)
    {
        for (int dx = -localRebuildRadius; dx <= localRebuildRadius; dx++)
            _pendingDirtyColumns.Add(worldX + dx);
    }

    public void MarkSourceDirty(LightSource src) => RebuildLightMapAt(Mathf.RoundToInt(src.WorldPosition.x), Mathf.RoundToInt(src.WorldPosition.y));
    public void NotifyChunkLoaded(int chunkLocalX) => _pendingFullRebuild = true;
    public void NotifyChunkUnloaded(int chunkLocalX) => _pendingFullRebuild = true;

    public void SetWindow(int originWorldX, int originWorldY, int widthTiles, int heightTiles)
    {
        if (widthTiles == _bufWidth && heightTiles == _bufHeight && originWorldX == _bufOriginX && originWorldY == _bufOriginY) return;

        _bufOriginX = originWorldX;
        _bufOriginY = originWorldY;

        if (widthTiles != _bufWidth || heightTiles != _bufHeight)
        {
            _bufWidth = widthTiles;
            _bufHeight = heightTiles;

            _rebuildTask?.Wait();

            if (_lightBuffer.IsCreated) _lightBuffer.Dispose();
            _lightBuffer = new NativeArray<Color>(_bufWidth * _bufHeight, Allocator.Persistent);
            _snapshotBuffer = new BlockSnapshot[_bufWidth * _bufHeight];

            if (_lightTex != null) Destroy(_lightTex);
            _lightTex = new Texture2D(_bufWidth, _bufHeight, TextureFormat.RGBAFloat, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };

            if (_lightMap != null) { if (RenderTexture.active == _lightMap) RenderTexture.active = null; _lightMap.Release(); }
            _lightMap = new RenderTexture(_bufWidth, _bufHeight, 0, RenderTextureFormat.ARGBFloat) { filterMode = FilterMode.Bilinear };
            _lightMap.Create();
        }
        _pendingFullRebuild = true;
    }

    void LateUpdate()
    {
        if (_uploadPending && (_rebuildTask == null || _rebuildTask.IsCompleted))
        {
            if (_rebuildTask?.Exception != null) Debug.LogError($"[LightingSystem] Rebuild faulted: {_rebuildTask.Exception}");
            UploadToGPU();
            _uploadPending = false;
            _rebuildTask = null;
        }

        if (_rebuildTask != null && !_rebuildTask.IsCompleted) return;

        if (_pendingFullRebuild)
        {
            _fullRebuildRequested = true;
            _pendingFullRebuild = false;
            foreach (int col in _pendingDirtyColumns) _dirtyWorldColumns.Add(col);
            _pendingDirtyColumns.Clear();
        }
        else
        {
            foreach (int col in _pendingDirtyColumns) _dirtyWorldColumns.Add(col);
            _pendingDirtyColumns.Clear();
        }

        if (_fullRebuildRequested || _dirtyWorldColumns.Count > 0) KickOffRebuild();
    }

    private void KickOffRebuild()
    {
        var wm = WorldManager.Instance;
        if (wm == null || wm.Data == null || !_lightBuffer.IsCreated || _bufWidth == 0) return;

        float dayIntensity = DayNightSystem.Instance != null ? DayNightSystem.Instance.AmbientBrightness : 1f;
        float falloff = Mathf.Lerp(lightFalloff, lightFalloff, dayIntensity);
        float wallFalloff = Mathf.Lerp(lightFalloff, lightFalloff, dayIntensity);
        float solidFalloff = Mathf.Lerp(solidFalloffNight, solidFalloffDay, dayIntensity);

        UpdateSnapshotBuffer(wm);

        bool doFull = _fullRebuildRequested;
        var dirtyCols = new HashSet<int>(_dirtyWorldColumns);
        _fullRebuildRequested = false;
        _dirtyWorldColumns.Clear();

        var sources = new List<LightSourceSnapshot>(_sources.Count);
        foreach (var s in _sources) sources.Add(new LightSourceSnapshot(s.WorldPosition, s.LightColor, s.Strength));

        int bufW = _bufWidth, bufH = _bufHeight, originX = _bufOriginX, originY = _bufOriginY;
        _renderedWidth = bufW; _renderedHeight = bufH; _renderedOriginX = originX; _renderedOriginY = originY;

        float falloffExp = lightFalloffExponent, cutoff = lightCutoff, skyCutoff = skyLightCutoff, downMult = downwardFalloffMult;
        var buffer = _lightBuffer;
        var snap = _snapshotBuffer;

        _uploadPending = true;
        _rebuildTask = Task.Run(() =>
        {
            if (doFull)
            {
                for (int i = 0; i < buffer.Length; i++) buffer[i] = Color.clear;
                for (int bx = 0; bx < bufW; bx++) InjectSunlightColumn(bx, bufW, bufH, snap, buffer, dayIntensity);
            }
            else
            {
                foreach (int wx in dirtyCols)
                {
                    int bx = wx - originX;
                    if (bx < 0 || bx >= bufW) continue;
                    for (int by = 0; by < bufH; by++) buffer[by * bufW + bx] = Color.clear;
                    InjectSunlightColumn(bx, bufW, bufH, snap, buffer, dayIntensity);
                }
            }

            PropagateSunlight(bufW, bufH, snap, buffer, falloff, wallFalloff, solidFalloff, falloffExp, skyCutoff, downMult);
            foreach (var src in sources) Propagate(src, bufW, bufH, originX, originY, snap, buffer, falloff, wallFalloff, solidFalloff, falloffExp, cutoff, downMult);
        });
    }

    private void UpdateSnapshotBuffer(WorldManager wm)
    {
        for (int by = 0; by < _bufHeight; by++)
        for (int bx = 0; bx < _bufWidth; bx++)
        {
            int wx = bx + _bufOriginX, wy = by + _bufOriginY;
            var block = wm.GetBlock(wx, wy);
            var bd = wm.GetBlockData(wx, wy);
            _snapshotBuffer[by * _bufWidth + bx] = new BlockSnapshot(
                block != BlockType.Air && block != BlockType.Water && (bd == null || bd.isSolid),
                wm.GetWall(wx, wy) != WallType.None
            );
        }
    }

    private static void InjectSunlightColumn(int bx, int bufW, int bufH, BlockSnapshot[] snap, NativeArray<Color> buffer, float dayIntensity)
    {
        for (int by = bufH - 1; by >= 0; by--)
        {
            int idx = by * bufW + bx;
            buffer[idx] = new Color(0f, 0f, 0f, dayIntensity);
            if (snap[idx].IsSolid || snap[idx].HasWall) break;
        }
    }

    private static void PropagateSunlight(int bufW, int bufH, BlockSnapshot[] snap, NativeArray<Color> buffer, float falloff, float wallFalloff, float solidFalloff, float falloffExp, float cutoff, float downwardFalloffMult)
    {
        var queue = new Queue<(int x, int y, float rawStr)>();
        var visited = new float[bufW * bufH];

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].a > 0)
            {
                queue.Enqueue((i % bufW, i / bufW, buffer[i].a));
                visited[i] = buffer[i].a;
            }
        }

        while (queue.Count > 0)
        {
            var (x, y, rawStr) = queue.Dequeue();
            if (rawStr <= cutoff) continue;

            int idx = y * bufW + x;
            Color cur = buffer[idx];
            buffer[idx] = new Color(cur.r, cur.g, cur.b, Mathf.Max(cur.a, Mathf.Pow(rawStr, falloffExp)));

            for (int i = 0; i < 8; i++)
            {
                int nx = x + Dirs[i].dx, ny = y + Dirs[i].dy;
                if (nx < 0 || nx >= bufW || ny < 0 || ny >= bufH) continue;

                int nIdx = ny * bufW + nx;
                var cell = snap[nIdx];
                float baseMult = cell.IsSolid ? solidFalloff : cell.HasWall ? wallFalloff : falloff;
                if (Dirs[i].dy == -1 && !cell.IsSolid) baseMult *= downwardFalloffMult;

                float nextRaw = rawStr * Mathf.Pow(baseMult, Dirs[i].dist);
                if (nextRaw > visited[nIdx]) { visited[nIdx] = nextRaw; queue.Enqueue((nx, ny, nextRaw)); }
            }
        }
    }

    private static void Propagate(LightSourceSnapshot src, int bufW, int bufH, int originX, int originY, BlockSnapshot[] snap, NativeArray<Color> buffer, float falloff, float wallFalloff, float solidFalloff, float falloffExp, float cutoff, float downwardFalloffMult)
    {
        int startX = Mathf.RoundToInt(src.Position.x) - originX, startY = Mathf.RoundToInt(src.Position.y) - originY;
        if (startX < 0 || startX >= bufW || startY < 0 || startY >= bufH) return;

        var visited = new float[bufW * bufH];
        var queue = new Queue<(int x, int y, Color light, float rawStr)>();
        queue.Enqueue((startX, startY, new Color(src.Color.r, src.Color.g, src.Color.b, 1f), src.Strength));
        visited[startY * bufW + startX] = src.Strength;

        while (queue.Count > 0)
        {
            var (x, y, light, rawStr) = queue.Dequeue();
            if (rawStr <= cutoff) continue;

            int idx = y * bufW + x;
            Color cur = buffer[idx];
            float b = Mathf.Pow(rawStr, falloffExp), dr = light.r * b, dg = light.g * b, db = light.b * b;
            buffer[idx] = new Color(cur.r > dr ? cur.r : dr, cur.g > dg ? cur.g : dg, cur.b > db ? cur.b : db, cur.a);

            bool isStart = (x == startX && y == startY);

            for (int i = 0; i < 8; i++)
            {
                int nx = x + Dirs[i].dx, ny = y + Dirs[i].dy;
                if (nx < 0 || nx >= bufW || ny < 0 || ny >= bufH) continue;

                int nIdx = ny * bufW + nx;
                var cell = snap[nIdx];
                float baseMult = cell.IsSolid ? (isStart ? 0.82f : solidFalloff) : (cell.HasWall ? wallFalloff : falloff);
                if (Dirs[i].dy == -1 && !cell.IsSolid) baseMult *= downwardFalloffMult;

                float nextRaw = rawStr * Mathf.Pow(baseMult, Dirs[i].dist);
                if (nextRaw > visited[nIdx]) { visited[nIdx] = nextRaw; queue.Enqueue((nx, ny, light, nextRaw)); }
            }
        }
    }

    private void UploadToGPU()
    {
        if (WorldManager.Instance == null) return;
        _lightTex.SetPixelData(_lightBuffer, 0);
        _lightTex.Apply(false, false);
        Graphics.Blit(_lightTex, _lightMap);

        Shader.SetGlobalTexture("_LightMap", _lightMap);
        Shader.SetGlobalFloat("_WorldMinX", _renderedOriginX);
        Shader.SetGlobalFloat("_WorldMinY", _renderedOriginY);
        Shader.SetGlobalFloat("_WorldWidth", _renderedWidth);
        Shader.SetGlobalFloat("_WorldHeight", _renderedHeight);
        Shader.SetGlobalFloat("_MinBrightness", minUndergroundBrightness);
        Shader.SetGlobalFloat("_AmbientBrightness", DayNightSystem.Instance != null ? DayNightSystem.Instance.AmbientBrightness : 1f);
        if (DayNightSystem.Instance != null) Shader.SetGlobalColor("_SkyColor", DayNightSystem.Instance.GetSkyColor());
    }

    public RenderTexture GetLightMap() => _lightMap;

    void OnDestroy()
    {
        _rebuildTask?.Wait();
        if (_lightBuffer.IsCreated) _lightBuffer.Dispose();
        if (_lightMap != null) { if (RenderTexture.active == _lightMap) RenderTexture.active = null; _lightMap.Release(); }
    }
}