using System.Collections.Generic;
using UnityEngine;

public class LightingSystem : MonoBehaviour
{
    public static LightingSystem Instance { get; private set; }

    [Header("Ustawienia")]
    [SerializeField] private float falloff = 0.85f;
    [SerializeField] private float wallFalloff = 0.7f;

    private RenderTexture _lightMap;
    private Texture2D _lightTex;
    private Color[] _lightBuffer;

    private int _width, _height;

    private List<LightSource> _sources = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Initialize(int worldWidth, int worldHeight)
    {
        _width = worldWidth;
        _height = worldHeight;
        _lightBuffer = new Color[_width * _height];

        _lightTex = new Texture2D(_width, _height, TextureFormat.RGBAFloat, false);
        _lightTex.filterMode = FilterMode.Bilinear;
        _lightTex.wrapMode = TextureWrapMode.Clamp;

        _lightMap = new RenderTexture(_width, _height, 0, RenderTextureFormat.ARGBFloat);
        _lightMap.filterMode = FilterMode.Bilinear;
    }

    public void RegisterSource(LightSource src) => _sources.Add(src);
    public void UnregisterSource(LightSource src) => _sources.Remove(src);

    public void RebuildLightMap()
    {
        if (_lightBuffer == null) return;

        ApplyAmbientAndSources();

        _lightTex.SetPixels(_lightBuffer);
        _lightTex.Apply();
        Graphics.Blit(_lightTex, _lightMap);
    }

    public void UpdateAmbientOnly()
    {
        if (_lightBuffer == null) return;

        float ambient = DayNightSystem.Instance != null
            ? DayNightSystem.Instance.AmbientBrightness
            : 1f;
        Color ambientColor = new Color(ambient, ambient, ambient, 1f);

        for (int i = 0; i < _lightBuffer.Length; i++)
            _lightBuffer[i] = ambientColor;

        foreach (var src in _sources)
            Propagate(src);

        _lightTex.SetPixels(_lightBuffer);
        _lightTex.Apply();
        Graphics.Blit(_lightTex, _lightMap);
    }

    private void ApplyAmbientAndSources()
    {
        float ambient = DayNightSystem.Instance != null
            ? DayNightSystem.Instance.AmbientBrightness
            : 1f;
        Color ambientColor = new Color(ambient, ambient, ambient, 1f);

        for (int i = 0; i < _lightBuffer.Length; i++)
            _lightBuffer[i] = ambientColor;

        foreach (var src in _sources)
            Propagate(src);
    }

    private void Propagate(LightSource src)
    {
        var wm = WorldManager.Instance;
        if (wm == null || wm.Data == null) return;

        int startX = Mathf.RoundToInt(src.WorldPosition.x) - wm.OffsetX;
        int startY = Mathf.RoundToInt(src.WorldPosition.y) - wm.OffsetY;

        if (startX < 0 || startX >= _width || startY < 0 || startY >= _height) return;

        var queue = new Queue<(int x, int y, Color light)>();
        var visited = new HashSet<int>();

        Color seedLight = new Color(
            src.LightColor.r * src.Strength,
            src.LightColor.g * src.Strength,
            src.LightColor.b * src.Strength,
            1f
        );

        queue.Enqueue((startX, startY, seedLight));
        visited.Add(startY * _width + startX);

        while (queue.Count > 0)
        {
            var (x, y, light) = queue.Dequeue();

            if (x < 0 || x >= _width || y < 0 || y >= _height) continue;

            int idx = y * _width + x;
            _lightBuffer[idx] = Max(_lightBuffer[idx], light);

            if (light.r < 0.01f && light.g < 0.01f && light.b < 0.01f) continue;

            (int dx, int dy)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };
            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;

                int nIdx = ny * _width + nx;
                if (visited.Contains(nIdx)) continue;
                visited.Add(nIdx);

                int wx = nx + wm.OffsetX;
                int wy = ny + wm.OffsetY;

                bool hasBlock = wm.GetBlock(wx, wy) != BlockType.Air;
                if (hasBlock) continue;

                bool hasWall = wm.GetWall(wx, wy) != WallType.None;
                float mult = hasWall ? wallFalloff : falloff;

                Color nextLight = new Color(
                    light.r * mult,
                    light.g * mult,
                    light.b * mult,
                    1f
                );

                queue.Enqueue((nx, ny, nextLight));
            }
        }
    }

    private Color Max(Color a, Color b)
        => new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), 1f);

    public RenderTexture GetLightMap() => _lightMap;

    void OnDestroy()
    {
        if (_lightMap != null)
        {
            if (RenderTexture.active == _lightMap)
                RenderTexture.active = null;

            _lightMap.Release();
            _lightMap = null;
        }
    }
}