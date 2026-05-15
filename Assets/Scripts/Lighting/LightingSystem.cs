using System.Collections.Generic;
using UnityEngine;

public class LightingSystem : MonoBehaviour
{
    public static LightingSystem Instance { get; private set; }

    [Header("Ustawienia T³umienia w Dzieñ (Jasno, g³êbokie cienie)")]
    [SerializeField] private float dayFalloff = 0.97f;
    [SerializeField] private float dayWallFalloff = 0.95f;

    [Header("Ustawienia T³umienia w Nocy (Ciemno, skupione œwiat³a)")]
    [SerializeField] private float nightFalloff = 0.90f;
    [SerializeField] private float nightWallFalloff = 0.90f;

    [Header("Jasnoœæ podziemi")]
    [SerializeField, Range(0f, 1f)] private float minUndergroundBrightness = 0.03f;

    private float currentFalloff;
    private float currentWallFalloff;

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
        var wm = WorldManager.Instance;
        if (wm == null || wm.Data == null) return;

        float dayIntensity = 1f;
        if (DayNightSystem.Instance != null)
        {
            dayIntensity = DayNightSystem.Instance.AmbientBrightness;
        }

        currentFalloff = Mathf.Lerp(nightFalloff, dayFalloff, dayIntensity);
        currentWallFalloff = Mathf.Lerp(nightWallFalloff, dayWallFalloff, dayIntensity);

        for (int x = 0; x < _width; x++)
        {
            float currentSky = dayIntensity;
            for (int y = _height - 1; y >= 0; y--)
            {
                int wx = x + wm.OffsetX;
                int wy = y + wm.OffsetY;

                if (wm.GetBlock(wx, wy) != BlockType.Air)
                {
                    currentSky *= Mathf.Lerp(0.50f, 0.72f, dayIntensity);
                }
                else if (wm.GetWall(wx, wy) != WallType.None)
                {
                    currentSky *= currentWallFalloff;
                }

                int idx = y * _width + x;
                _lightBuffer[idx] = new Color(0f, 0f, 0f, currentSky);
            }
        }

        foreach (var src in _sources)
        {
            Propagate(src);
        }

        _lightTex.SetPixels(_lightBuffer);
        _lightTex.Apply();
        Graphics.Blit(_lightTex, _lightMap);

        Shader.SetGlobalTexture("_LightMap", _lightMap);
        Shader.SetGlobalFloat("_WorldMinX", wm.OffsetX);
        Shader.SetGlobalFloat("_WorldMinY", wm.OffsetY);
        Shader.SetGlobalFloat("_WorldWidth", wm.Data.Width);
        Shader.SetGlobalFloat("_WorldHeight", wm.Data.Height);
        Shader.SetGlobalFloat("_MinBrightness", minUndergroundBrightness);

        if (DayNightSystem.Instance != null)
        {
            Shader.SetGlobalColor("_SkyColor", DayNightSystem.Instance.GetSkyColor());
        }
    }

    public void UpdateAmbientOnly()
    {
        RebuildLightMap();
    }

    private void Propagate(LightSource src)
    {
        var wm = WorldManager.Instance;
        if (wm == null || wm.Data == null) return;

        int startX = Mathf.RoundToInt(src.WorldPosition.x) - wm.OffsetX;
        int startY = Mathf.RoundToInt(src.WorldPosition.y) - wm.OffsetY;

        if (startX < 0 || startX >= _width || startY < 0 || startY >= _height) return;

        var queue = new Queue<(int x, int y, Color light)>();
        var visited = new Dictionary<int, float>();

        Color seedLight = new Color(src.LightColor.r * src.Strength, src.LightColor.g * src.Strength, src.LightColor.b * src.Strength, 1f);

        queue.Enqueue((startX, startY, seedLight));
        visited[startY * _width + startX] = seedLight.grayscale;

        while (queue.Count > 0)
        {
            var (x, y, light) = queue.Dequeue();

            int idx = y * _width + x;
            _lightBuffer[idx] = Max(_lightBuffer[idx], light);

            if (light.r < 0.02f && light.g < 0.02f && light.b < 0.02f) continue;

            (int dx, int dy)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx;
                int ny = y + dy;

                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;

                int nIdx = ny * _width + nx;
                int wx = nx + wm.OffsetX;
                int wy = ny + wm.OffsetY;

                float mult = currentFalloff;
                if (wm.GetBlock(wx, wy) != BlockType.Air)
                {
                    mult = Mathf.Lerp(0.52f, 0.68f, currentFalloff >= dayFalloff ? 1f : 0f);
                }
                else if (wm.GetWall(wx, wy) != WallType.None)
                {
                    mult = currentWallFalloff;
                }

                Color nextLight = new Color(light.r * mult, light.g * mult, light.b * mult, 1f);
                float nextGrayscale = nextLight.grayscale;

                if (!visited.TryGetValue(nIdx, out float maxGray) || nextGrayscale > maxGray)
                {
                    visited[nIdx] = nextGrayscale;
                    queue.Enqueue((nx, ny, nextLight));
                }
            }
        }
    }

    private Color Max(Color a, Color b) => new Color(Mathf.Max(a.r, b.r), Mathf.Max(a.g, b.g), Mathf.Max(a.b, b.b), a.a);

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