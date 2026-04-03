using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

    [SerializeField] private GameObject _panel;
    [SerializeField] private RawImage _image;
    [SerializeField] private RectTransform _playerMarker;
    [SerializeField] private int _scale = 2;

    private Texture2D _tex;
    private bool _open;
    private Transform _player;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() { _panel.SetActive(false); BakeTexture(); }

    void Update()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (Keyboard.current.mKey.wasPressedThisFrame) Toggle();
        if (_open) UpdateMarker();
    }

    private void Toggle()
    {
        _open = !_open;
        _panel.SetActive(_open);
        if (_open) { Canvas.ForceUpdateCanvases(); UpdateMarker(); }
    }

    private void UpdateMarker()
    {
        if (_player == null || _playerMarker == null) return;
        var world = WorldManager.Instance;
        float nx = Mathf.Clamp01((_player.position.x - world.OffsetX) / world.Data.Width);
        float ny = Mathf.Clamp01((_player.position.y - world.OffsetY) / world.Data.Height);
        Rect rect = _image.rectTransform.rect;
        _playerMarker.anchoredPosition = new Vector2((nx - 0.5f) * rect.width, (ny - 0.5f) * rect.height);
    }

    private void BakeTexture()
    {
        var world = WorldManager.Instance;
        _tex = new Texture2D(world.Data.Width * _scale, world.Data.Height * _scale, TextureFormat.RGB24, false)
               { filterMode = FilterMode.Point };

        for (int ly = 0; ly < world.Data.Height; ly++)
        for (int lx = 0; lx < world.Data.Width; lx++)
        {
            var c = BlockColorMapper.Get(world.Data.GetBlock(lx, ly));
            for (int py = 0; py < _scale; py++)
            for (int px = 0; px < _scale; px++)
                _tex.SetPixel(lx * _scale + px, ly * _scale + py, c);
        }

        _tex.Apply();
        _image.texture = _tex;
    }

    public void RefreshBlock(int wx, int wy)
    {
        var world = WorldManager.Instance;
        int lx = wx - world.OffsetX;
        int ly = wy - world.OffsetY;
        var c = BlockColorMapper.Get(world.Data.GetBlock(lx, ly));

        for (int py = 0; py < _scale; py++)
        for (int px = 0; px < _scale; px++)
            _tex.SetPixel(lx * _scale + px, ly * _scale + py, c);
        _tex.Apply();
    }
}