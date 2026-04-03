using UnityEngine;
using UnityEngine.UI;

public class MinimapSystem : MonoBehaviour
{
    [SerializeField] private RawImage _image;
    [SerializeField] private RectTransform _playerDot;
    [SerializeField] private int _viewRadius = 40;
    [SerializeField] private int _texSize = 128;

    private Texture2D _tex;
    private Transform _player;
    private Color[] _pixels;

    void Start()
    {
        _tex = new Texture2D(_texSize, _texSize, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
        _pixels = new Color[_texSize * _texSize];
        _image.texture = _tex;
    }

    void Update()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player == null) return;
        Redraw();
        _playerDot.anchoredPosition = Vector2.zero;
    }

    private void Redraw()
    {
        var world = WorldManager.Instance;
        int cx = Mathf.RoundToInt(_player.position.x);
        int cy = Mathf.RoundToInt(_player.position.y);
        float step = (_viewRadius * 2f) / _texSize;

        for (int py = 0; py < _texSize; py++)
        for (int px = 0; px < _texSize; px++)
        {
            int wx = cx + Mathf.RoundToInt((px - _texSize / 2f) * step);
            int wy = cy + Mathf.RoundToInt((py - _texSize / 2f) * step);
            int lx = wx - world.OffsetX;
            int ly = wy - world.OffsetY;
            var block = world.Data.InBounds(lx, ly) ? world.Data.GetBlock(lx, ly) : BlockType.Air;
            _pixels[py * _texSize + px] = BlockColorMapper.Get(block);
        }

        _tex.SetPixels(_pixels);
        _tex.Apply();
    }
}