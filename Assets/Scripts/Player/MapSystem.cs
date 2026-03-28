using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private RawImage mapImage;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private Transform markersContainer;

    [Header("Settings")]
    [SerializeField] private int textureScale = 2;
    [SerializeField] private Color airColor = new Color(0.08f, 0.08f, 0.15f, 1f);
    [SerializeField] private Color dirtColor = new Color(0.55f, 0.35f, 0.15f, 1f);
    [SerializeField] private Color stoneColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color sandColor = new Color(0.87f, 0.78f, 0.45f, 1f);
    [SerializeField] private Color grassColor = new Color(0.25f, 0.65f, 0.20f, 1f);
    [SerializeField] private Color snowColor = Color.white;
    [SerializeField] private Color copperColor = new Color(0.80f, 0.45f, 0.20f, 1f);
    [SerializeField] private Color ironColor = new Color(0.75f, 0.75f, 0.80f, 1f);
    [SerializeField] private Color coalColor = new Color(0.20f, 0.20f, 0.20f, 1f);

    private Texture2D _mapTex;
    private bool _isOpen;
    private Transform _player;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        mapPanel.SetActive(false);
        BakeTexture();
    }

    void Update()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            Toggle();

        if (_isOpen)
            UpdatePlayerMarker();
    }

    void Toggle()
    {
        _isOpen = !_isOpen;
        mapPanel.SetActive(_isOpen);

        if (_isOpen)
        {
            Canvas.ForceUpdateCanvases();
            UpdatePlayerMarker();
        }
    }

    void UpdatePlayerMarker()
    {
        if (_player == null || playerMarker == null) return;

        var world = WorldManager.Instance;

        float lx = _player.position.x - world.OffsetX;
        float ly = _player.position.y - world.OffsetY;
        float nx = Mathf.Clamp01(lx / world.Data.Width);
        float ny = Mathf.Clamp01(ly / world.Data.Height);

        Rect rect = mapImage.rectTransform.rect;

        playerMarker.anchoredPosition = new Vector2(
            (nx - 0.5f) * rect.width,
            (ny - 0.5f) * rect.height
        );
    }

    void BakeTexture()
    {
        var world = WorldManager.Instance;
        int w = world.Data.Width * textureScale;
        int h = world.Data.Height * textureScale;

        _mapTex = new Texture2D(w, h, TextureFormat.RGB24, false)
        { filterMode = FilterMode.Point };

        for (int ly = 0; ly < world.Data.Height; ly++)
            for (int lx = 0; lx < world.Data.Width; lx++)
            {
                var col = BlockToColor(world.Data.GetBlock(lx, ly));
                for (int py = 0; py < textureScale; py++)
                    for (int px = 0; px < textureScale; px++)
                        _mapTex.SetPixel(lx * textureScale + px,
                                         ly * textureScale + py, col);
            }
        _mapTex.Apply();
        mapImage.texture = _mapTex;
    }

    public void RefreshBlock(int worldX, int worldY)
    {
        var world = WorldManager.Instance;
        int lx = worldX - world.OffsetX;
        int ly = worldY - world.OffsetY;
        var col = BlockToColor(world.Data.GetBlock(lx, ly));
        for (int py = 0; py < textureScale; py++)
            for (int px = 0; px < textureScale; px++)
                _mapTex.SetPixel(lx * textureScale + px,
                                 ly * textureScale + py, col);
        _mapTex.Apply();
    }

    Color BlockToColor(BlockType b) => b switch
    {
        BlockType.Dirt => dirtColor,
        BlockType.Stone => stoneColor,
        BlockType.Sand => sandColor,
        BlockType.Grass => grassColor,
        BlockType.Snow => snowColor,
        BlockType.CopperOre => copperColor,
        BlockType.IronOre => ironColor,
        BlockType.CoalOre => coalColor,
        _ => airColor,
    };
}