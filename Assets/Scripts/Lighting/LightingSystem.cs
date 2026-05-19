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

    [Header("Day Falloff")]
    [SerializeField] private float dayFalloff = 0.97f;
    [SerializeField] private float dayWallFalloff = 0.95f;

    [Header("Night Falloff")]
    [SerializeField] private float nightFalloff = 0.90f;
    [SerializeField] private float nightWallFalloff = 0.90f;

    [Header("Underground")]
    [SerializeField, Range(0f, 1f)] private float minUndergroundBrightness = 0f;
    [SerializeField, Range(0.1f, 0.9f)] private float dayBlockOcclusion = 0.40f;
    [SerializeField, Range(0f, 0.1f)] private float skyLightCutoff = 0.02f;

    [Header("Point Light Shape")]
    [SerializeField] private float lightFalloffExponent = 2.2f;
    [SerializeField] private float lightCutoff = 0.04f;

    [Header("Point Light Solid Falloff")]
    [SerializeField] private float solidFalloffDay = 0.68f;
    [SerializeField] private float solidFalloffNight = 0.52f;

    [Header("Rebuild Radius")]
    [SerializeField] private int localRebuildRadius = 12;

    private float _currentFalloff;
    private float _currentWallFalloff;
    private float _currentSolidFalloff;

    private RenderTexture _lightMap;
    private Texture2D _lightTex;
    private NativeArray<Color> _lightBuffer;

    private int _bufWidth, _bufHeight;
    private int _bufOriginX, _bufOriginY;

    private List<LightSource> _sources = new();
    private List<LightSourceSnapshot> _sourceSnapshot = new();

    private HashSet<int> _dirtyWorldColumns = new();
    private bool _fullRebuildRequested = true;
    private bool _pendingFullRebuild = false;
    private HashSet<int> _pendingDirtyColumns = new();

    private Task _rebuildTask = null;
    private bool _uploadPending = false;

    private readonly (int dx, int dy)[] _dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterSource(LightSource src) => _sources.Add(src);
    public void UnregisterSource(LightSource src) => _sources.Remove(src);
    public void RebuildLightMap() => _pendingFullRebuild = true;
    public void UpdateAmbientOnly() => _pendingFullRebuild = true;

    public void RebuildLightMapAt(int worldX, int worldY)
    {
        for (int dx = -localRebuildRadius; dx <= localRebuildRadius; dx++)
            _pendingDirtyColumns.Add(worldX + dx);
    }

    public void SetWindow(int originWorldX, int originWorldY, int widthTiles, int heightTiles)
    {
        bool sizeChanged = widthTiles != _bufWidth || heightTiles != _bufHeight;
        bool originChanged = originWorldX != _bufOriginX || originWorldY != _bufOriginY;

        if (!sizeChanged && !originChanged) return;

        _bufOriginX = originWorldX;
        _bufOriginY = originWorldY;

        if (sizeChanged)
        {
            _bufWidth = widthTiles;
            _bufHeight = heightTiles;

            if (_rebuildTask != null && !_rebuildTask.IsCompleted)
                _rebuildTask.Wait();

            if (_lightBuffer.IsCreated) _lightBuffer.Dispose();
            _lightBuffer = new NativeArray<Color>(_bufWidth * _bufHeight, Allocator.Persistent);

            if (_lightTex != null) Object.Destroy(_lightTex);
            _lightTex = new Texture2D(_bufWidth, _bufHeight, TextureFormat.RGBAFloat, false);
            _lightTex.filterMode = FilterMode.Bilinear;
            _lightTex.wrapMode = TextureWrapMode.Clamp;

            if (_lightMap != null)
            {
                if (RenderTexture.active == _lightMap) RenderTexture.active = null;
                _lightMap.Release();
            }
            _lightMap = new RenderTexture(_bufWidth, _bufHeight, 0, RenderTextureFormat.ARGBFloat);
            _lightMap.filterMode = FilterMode.Bilinear;
            _lightMap.Create();
        }

        _pendingFullRebuild = true;
    }

    public void NotifyChunkLoaded(int chunkLocalX) => _pendingFullRebuild = true;
    public void NotifyChunkUnloaded(int chunkLocalX) => _pendingFullRebuild = true;

    void LateUpdate()
    {
        if (_uploadPending && (_rebuildTask == null || _rebuildTask.IsCompleted))
        {
            if (_rebuildTask?.Exception != null)
                Debug.LogError($"[LightingSystem] Rebuild faulted: {_rebuildTask.Exception}");
            UploadToGPU();
            _uploadPending = false;
            _rebuildTask = null;
        }

        if (_rebuildTask != null && !_rebuildTask.IsCompleted) return;

        if (_pendingFullRebuild)
        {
            _fullRebuildRequested = true;
            _pendingFullRebuild = false;
            _dirtyWorldColumns.Clear();
            _pendingDirtyColumns.Clear();
        }
        else
        {
            foreach (int col in _pendingDirtyColumns)
                _dirtyWorldColumns.Add(col);
            _pendingDirtyColumns.Clear();
        }

        if (!_fullRebuildRequested && _dirtyWorldColumns.Count == 0) return;

        KickOffRebuild();
    }

    private void KickOffRebuild()
    {
        var wm = WorldManager.Instance;
        if (wm == null || wm.Data == null || !_lightBuffer.IsCreated || _bufWidth == 0) return;

        float dayIntensity = DayNightSystem.Instance != null ? DayNightSystem.Instance.AmbientBrightness : 1f;
        _currentFalloff = Mathf.Lerp(nightFalloff, dayFalloff, dayIntensity);
        _currentWallFalloff = Mathf.Lerp(nightWallFalloff, dayWallFalloff, dayIntensity);
        _currentSolidFalloff = Mathf.Lerp(solidFalloffNight, solidFalloffDay, dayIntensity);
        float airBlockLerp = Mathf.Lerp(0.25f, dayBlockOcclusion, dayIntensity);

        var snapshot = CaptureSnapshot(wm);

        bool doFull = _fullRebuildRequested;
        var dirtyCols = doFull ? null : new HashSet<int>(_dirtyWorldColumns);
        _fullRebuildRequested = false;
        _dirtyWorldColumns.Clear();

        _sourceSnapshot.Clear();
        foreach (var s in _sources)
            _sourceSnapshot.Add(new LightSourceSnapshot(s.WorldPosition, s.LightColor, s.Strength));
        var sources = new List<LightSourceSnapshot>(_sourceSnapshot);

        int bufW = _bufWidth;
        int bufH = _bufHeight;
        int originX = _bufOriginX;
        int originY = _bufOriginY;
        float falloff = _currentFalloff;
        float wallFalloff = _currentWallFalloff;
        float solidFalloff = _currentSolidFalloff;
        float falloffExp = lightFalloffExponent;
        float cutoff = lightCutoff;
        float skyCutoff = skyLightCutoff;
        var buffer = _lightBuffer;

        _uploadPending = true;
        _rebuildTask = Task.Run(() =>
        {
            if (doFull)
            {
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = Color.clear;
                for (int bx = 0; bx < bufW; bx++)
                    UpdateColumn(bx, bufW, bufH, originX, originY, snapshot, buffer, dayIntensity, airBlockLerp, wallFalloff, skyCutoff);
            }
            else
            {
                foreach (int wx in dirtyCols)
                {
                    int bx = wx - originX;
                    if (bx < 0 || bx >= bufW) continue;
                    for (int by = 0; by < bufH; by++)
                        buffer[by * bufW + bx] = Color.clear;
                }
                foreach (int wx in dirtyCols)
                {
                    int bx = wx - originX;
                    if (bx < 0 || bx >= bufW) continue;
                    UpdateColumn(bx, bufW, bufH, originX, originY, snapshot, buffer, dayIntensity, airBlockLerp, wallFalloff, skyCutoff);
                }
            }

            ApplyHorizontalSkyBleed(bufW, bufH, snapshot, buffer, airBlockLerp, wallFalloff, skyCutoff);

            foreach (var src in sources)
                Propagate(src, bufW, bufH, originX, originY, snapshot, buffer, falloff, wallFalloff, solidFalloff, falloffExp, cutoff);
        });
    }

    private BlockSnapshot[] CaptureSnapshot(WorldManager wm)
    {
        var cells = new BlockSnapshot[_bufWidth * _bufHeight];
        for (int by = 0; by < _bufHeight; by++)
        for (int bx = 0; bx < _bufWidth; bx++)
        {
            int wx = bx + _bufOriginX;
            int wy = by + _bufOriginY;
            var block = wm.GetBlock(wx, wy);
            var bd = wm.GetBlockData(wx, wy);
            bool solid = block != BlockType.Air && block != BlockType.Water && (bd == null || bd.isSolid);
            bool hasWall = wm.GetWall(wx, wy) != WallType.None;
            cells[by * _bufWidth + bx] = new BlockSnapshot(solid, hasWall);
        }
        return cells;
    }

    private static void UpdateColumn(
        int bx, int bufW, int bufH, int originX, int originY,
        BlockSnapshot[] snap, NativeArray<Color> buffer,
        float dayIntensity, float airBlockLerp, float wallFalloff, float skyCutoff)
    {
        float sky = dayIntensity;
        bool hitSolid = false;

        for (int by = bufH - 1; by >= 0; by--)
        {
            var cell = snap[by * bufW + bx];
            if (cell.IsSolid)
            {
                hitSolid = true;
                sky *= airBlockLerp;
            }
            else if (hitSolid && cell.HasWall)
            {
                sky *= wallFalloff;
            }
            if (sky < skyCutoff) sky = 0f;
            buffer[by * bufW + bx] = new Color(0f, 0f, 0f, sky);
        }
    }

    private static void ApplyHorizontalSkyBleed(
        int bufW, int bufH, BlockSnapshot[] snap, NativeArray<Color> buffer,
        float airBlockLerp, float wallFalloff, float skyCutoff)
    {
        for (int by = bufH - 1; by >= 0; by--)
        {
            for (int bx = 1; bx < bufW; bx++)
            {
                var cell = snap[by * bufW + bx];
                float neighbor = buffer[by * bufW + (bx - 1)].a;
                float mult = cell.IsSolid ? airBlockLerp : wallFalloff;
                float bled = neighbor * mult;
                if (bled < skyCutoff) bled = 0f;
                int idx = by * bufW + bx;
                Color cur = buffer[idx];
                if (bled > cur.a)
                    buffer[idx] = new Color(cur.r, cur.g, cur.b, bled);
            }
            for (int bx = bufW - 2; bx >= 0; bx--)
            {
                var cell = snap[by * bufW + bx];
                float neighbor = buffer[by * bufW + (bx + 1)].a;
                float mult = cell.IsSolid ? airBlockLerp : wallFalloff;
                float bled = neighbor * mult;
                if (bled < skyCutoff) bled = 0f;
                int idx = by * bufW + bx;
                Color cur = buffer[idx];
                if (bled > cur.a)
                    buffer[idx] = new Color(cur.r, cur.g, cur.b, bled);
            }
        }
    }

    private static void Propagate(
        LightSourceSnapshot src,
        int bufW, int bufH, int originX, int originY,
        BlockSnapshot[] snap, NativeArray<Color> buffer,
        float falloff, float wallFalloff, float solidFalloff,
        float falloffExp, float cutoff)
    {
        int startX = Mathf.RoundToInt(src.Position.x) - originX;
        int startY = Mathf.RoundToInt(src.Position.y) - originY;
        if (startX < 0 || startX >= bufW || startY < 0 || startY >= bufH) return;

        var visited = new float[bufW * bufH];
        var queue = new Queue<(int x, int y, Color light, float rawStr)>();

        int startIdx = startY * bufW + startX;
        queue.Enqueue((startX, startY, new Color(src.Color.r, src.Color.g, src.Color.b, 1f), src.Strength));
        visited[startIdx] = src.Strength;

        (int dx, int dy)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        while (queue.Count > 0)
        {
            var (x, y, light, rawStr) = queue.Dequeue();
            if (rawStr <= cutoff) continue;

            float brightness = Mathf.Pow(rawStr, falloffExp);
            int idx = y * bufW + x;
            Color cur = buffer[idx];
            float dr = light.r * brightness;
            float dg = light.g * brightness;
            float db = light.b * brightness;
            buffer[idx] = new Color(
                cur.r > dr ? cur.r : dr,
                cur.g > dg ? cur.g : dg,
                cur.b > db ? cur.b : db,
                cur.a
            );

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dirs[i].dx;
                int ny = y + dirs[i].dy;
                if (nx < 0 || nx >= bufW || ny < 0 || ny >= bufH) continue;

                int nIdx = ny * bufW + nx;
                var cell = snap[nIdx];
                float mult = cell.IsSolid ? solidFalloff : cell.HasWall ? wallFalloff : falloff;
                float nextRaw = rawStr * mult;

                if (nextRaw > visited[nIdx])
                {
                    visited[nIdx] = nextRaw;
                    queue.Enqueue((nx, ny, light, nextRaw));
                }
            }
        }
    }

    private void UploadToGPU()
    {
        var wm = WorldManager.Instance;
        if (wm == null) return;

        _lightTex.SetPixelData(_lightBuffer, 0);
        _lightTex.Apply(false, false);
        Graphics.Blit(_lightTex, _lightMap);

        Shader.SetGlobalTexture("_LightMap", _lightMap);
        Shader.SetGlobalFloat("_WorldMinX", _bufOriginX);
        Shader.SetGlobalFloat("_WorldMinY", _bufOriginY);
        Shader.SetGlobalFloat("_WorldWidth", _bufWidth);
        Shader.SetGlobalFloat("_WorldHeight", _bufHeight);
        Shader.SetGlobalFloat("_MinBrightness", minUndergroundBrightness);
        Shader.SetGlobalFloat("_AmbientBrightness", DayNightSystem.Instance != null ? DayNightSystem.Instance.AmbientBrightness : 1f);

        if (DayNightSystem.Instance != null)
            Shader.SetGlobalColor("_SkyColor", DayNightSystem.Instance.GetSkyColor());
    }

    public RenderTexture GetLightMap() => _lightMap;

    void OnDestroy()
    {
        _rebuildTask?.Wait();
        if (_lightBuffer.IsCreated) _lightBuffer.Dispose();
        if (_lightMap != null)
        {
            if (RenderTexture.active == _lightMap) RenderTexture.active = null;
            _lightMap.Release();
            _lightMap = null;
        }
    }
}