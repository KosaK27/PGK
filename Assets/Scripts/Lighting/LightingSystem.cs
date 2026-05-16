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

    private Queue<(int x, int y, Color light)> _queue = new();
    private Dictionary<int, float> _visited = new();
    private readonly (int dx, int dy)[] _dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    private HashSet<int> _columnsToUpdate = new HashSet<int>();
    private bool _fullRebuildRequested = true;

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

        _fullRebuildRequested = true;
    }

    public void RegisterSource(LightSource src) => _sources.Add(src);
    public void UnregisterSource(LightSource src) => _sources.Remove(src);

    public void RebuildLightMap()
    {
        _fullRebuildRequested = true;
    }

    public void RebuildLightMapAt(int worldX, int worldY)
    {
        var wm = WorldManager.Instance;
        if (wm == null) return;

        int localX = worldX - wm.OffsetX;
        if (localX >= 0 && localX < _width)
        {
            _columnsToUpdate.Add(localX);
        }
    }

    void LateUpdate()
    {
        if (_fullRebuildRequested || _columnsToUpdate.Count > 0)
        {
            ExecuteIncrementalRebuild();
        }
    }

    private void ExecuteIncrementalRebuild()
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

        int offsetX = wm.OffsetX;
        int offsetY = wm.OffsetY;
        float airBlockBlockLerp = Mathf.Lerp(0.50f, 0.72f, dayIntensity);

        if (_fullRebuildRequested)
        {
            _fullRebuildRequested = false;
            _columnsToUpdate.Clear();
            for (int x = 0; x < _width; x++)
            {
                UpdateColumn(x, wm, offsetX, offsetY, dayIntensity, airBlockBlockLerp);
            }
        }
        else
        {
            foreach (int x in _columnsToUpdate)
            {
                UpdateColumn(x, wm, offsetX, offsetY, dayIntensity, airBlockBlockLerp);
            }
            _columnsToUpdate.Clear();
        }

        int sourceCount = _sources.Count;
        for (int i = 0; i < sourceCount; i++)
        {
            Propagate(_sources[i], wm, offsetX, offsetY);
        }

        _lightTex.SetPixels(_lightBuffer);
        _lightTex.Apply();
        Graphics.Blit(_lightTex, _lightMap);

        Shader.SetGlobalTexture("_LightMap", _lightMap);
        Shader.SetGlobalFloat("_WorldMinX", offsetX);
        Shader.SetGlobalFloat("_WorldMinY", offsetY);
        Shader.SetGlobalFloat("_WorldWidth", wm.Data.Width);
        Shader.SetGlobalFloat("_WorldHeight", wm.Data.Height);
        Shader.SetGlobalFloat("_MinBrightness", minUndergroundBrightness);

        if (DayNightSystem.Instance != null)
        {
            Shader.SetGlobalColor("_SkyColor", DayNightSystem.Instance.GetSkyColor());
        }
    }

    private void UpdateColumn(int x, WorldManager wm, int offsetX, int offsetY, float dayIntensity, float airBlockBlockLerp)
    {
        float currentSky = dayIntensity;
        int wx = x + offsetX;

        for (int y = _height - 1; y >= 0; y--)
        {
            int wy = y + offsetY;
            BlockType block = wm.GetBlock(wx, wy);

            if (block != BlockType.Air && block != BlockType.Water)
            {
                currentSky *= airBlockBlockLerp;
            }
            else if (wm.GetWall(wx, wy) != WallType.None)
            {
                currentSky *= currentWallFalloff;
            }

            _lightBuffer[y * _width + x] = new Color(0f, 0f, 0f, currentSky);
        }
    }

    public void UpdateAmbientOnly()
    {
        _fullRebuildRequested = true;
    }

    private void Propagate(LightSource src, WorldManager wm, int offsetX, int offsetY)
    {
        int startX = Mathf.RoundToInt(src.WorldPosition.x) - offsetX;
        int startY = Mathf.RoundToInt(src.WorldPosition.y) - offsetY;

        if (startX < 0 || startX >= _width || startY < 0 || startY >= _height) return;

        _queue.Clear();
        _visited.Clear();

        float strength = src.Strength;
        Color srcColor = src.LightColor;
        Color seedLight = new Color(srcColor.r * strength, srcColor.g * strength, srcColor.b * strength, 1f);

        _queue.Enqueue((startX, startY, seedLight));
        _visited[startY * _width + startX] = seedLight.grayscale;

        while (_queue.Count > 0)
        {
            var (x, y, light) = _queue.Dequeue();

            int idx = y * _width + x;
            Color currentBuffer = _lightBuffer[idx];

            _lightBuffer[idx] = new Color(
                currentBuffer.r > light.r ? currentBuffer.r : light.r,
                currentBuffer.g > light.g ? currentBuffer.g : light.g,
                currentBuffer.b > light.b ? currentBuffer.b : light.b,
                currentBuffer.a
            );

            if (light.r < 0.02f && light.g < 0.02f && light.b < 0.02f) continue;

            for (int i = 0; i < 4; i++)
            {
                int nx = x + _dirs[i].dx;
                int ny = y + _dirs[i].dy;

                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;

                int nIdx = ny * _width + nx;
                int wx = nx + offsetX;
                int wy = ny + offsetY;

                float mult = currentFalloff;
                BlockType block = wm.GetBlock(wx, wy);
                if (block != BlockType.Air && block != BlockType.Water)
                {
                    mult = currentFalloff >= dayFalloff ? 1f : 0f;
                    mult = mult * 0.16f + 0.52f;
                }
                else if (wm.GetWall(wx, wy) != WallType.None)
                {
                    mult = currentWallFalloff;
                }

                Color nextLight = new Color(light.r * mult, light.g * mult, light.b * mult, 1f);
                float nextGrayscale = nextLight.grayscale;

                if (!_visited.TryGetValue(nIdx, out float maxGray) || nextGrayscale > maxGray)
                {
                    _visited[nIdx] = nextGrayscale;
                    _queue.Enqueue((nx, ny, nextLight));
                }
            }
        }
    }

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