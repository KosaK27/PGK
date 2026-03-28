using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform playerDot;

    [Header("Settings")]
    [SerializeField] private int viewRadius = 40;
    [SerializeField] private int texSize = 128;

    [SerializeField] private Color airColor = new Color(0.08f, 0.08f, 0.15f);
    [SerializeField] private Color dirtColor = new Color(0.55f, 0.35f, 0.15f);
    [SerializeField] private Color stoneColor = new Color(0.45f, 0.45f, 0.45f);
    [SerializeField] private Color sandColor = new Color(0.87f, 0.78f, 0.45f);
    [SerializeField] private Color grassColor = new Color(0.25f, 0.65f, 0.20f);
    [SerializeField] private Color snowColor = Color.white;
    [SerializeField] private Color copperColor = new Color(0.80f, 0.45f, 0.20f);
    [SerializeField] private Color ironColor = new Color(0.75f, 0.75f, 0.80f);
    [SerializeField] private Color coalColor = new Color(0.20f, 0.20f, 0.20f);

    private Texture2D _tex;
    private Transform _player;
    private Color[] _pixels;

    void Start()
    {
        _tex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false)
        { filterMode = FilterMode.Point };
        _pixels = new Color[texSize * texSize];
        minimapImage.texture = _tex;
    }

    void Update()
    {
        if (_player == null)
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (_player == null) return;

        RedrawMinimap();
        UpdatePlayerDot();
    }

    void RedrawMinimap()
    {
        var world = WorldManager.Instance;
        int cx = Mathf.RoundToInt(_player.position.x);
        int cy = Mathf.RoundToInt(_player.position.y);
        float step = (viewRadius * 2f) / texSize;

        for (int py = 0; py < texSize; py++)
            for (int px = 0; px < texSize; px++)
            {
                int wx = cx + Mathf.RoundToInt((px - texSize / 2f) * step);
                int wy = cy + Mathf.RoundToInt((py - texSize / 2f) * step);
                int lx = wx - world.OffsetX;
                int ly = wy - world.OffsetY;

                BlockType block = world.Data.InBounds(lx, ly)
                    ? world.Data.GetBlock(lx, ly)
                    : BlockType.Air;

                _pixels[py * texSize + px] = BlockToColor(block);
            }

        _tex.SetPixels(_pixels);
        _tex.Apply();
    }

    void UpdatePlayerDot()
    {
        playerDot.anchoredPosition = Vector2.zero;
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