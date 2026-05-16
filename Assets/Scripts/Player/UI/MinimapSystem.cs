using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapSystem : MonoBehaviour
{
    public static MinimapSystem Instance { get; private set; }

    [Header("Map References")]
    [SerializeField] private RawImage _image;

    [Header("Marker Sprites")]
    [SerializeField] private Sprite _playerSprite;
    [SerializeField] private Sprite _enemySprite;
    [SerializeField] private Sprite _bossSprite;
    [SerializeField] private Sprite _friendSprite;
    [SerializeField] private Vector2 _markerSize = new Vector2(12f, 12f);

    [Header("Map Configurations")]
    [SerializeField] private int _viewRadius = 40;
    [SerializeField] private int _texSize = 128;
    [SerializeField] private Color _fogOfWarColor = Color.black;

    private Texture2D _tex;
    private Transform _player;
    private Color[] _pixels;
    private RectTransform _playerMarkerUI;
    private Dictionary<Transform, RectTransform> _activeMarkers = new Dictionary<Transform, RectTransform>();
    private List<System.Action> _registrationQueue = new List<System.Action>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            foreach (var action in _registrationQueue)
            {
                action.Invoke();
            }
            _registrationQueue.Clear();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeTexture();
    }

    private void InitializeTexture()
    {
        if (_tex == null)
        {
            _tex = new Texture2D(_texSize, _texSize, TextureFormat.RGB24, false)
            { filterMode = FilterMode.Point };
            _pixels = new Color[_texSize * _texSize];
            _image.texture = _tex;
        }
    }

    private void CreatePlayerMarker()
    {
        if (_playerMarkerUI != null || _playerSprite == null || _image == null) return;

        GameObject markerObj = new GameObject("MinimapMarker_Player", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObj.transform.SetParent(_image.rectTransform, false);

        Image img = markerObj.GetComponent<Image>();
        img.sprite = _playerSprite;
        img.raycastTarget = false;

        _playerMarkerUI = markerObj.GetComponent<RectTransform>();
        _playerMarkerUI.sizeDelta = _markerSize;
        _playerMarkerUI.anchorMin = new Vector2(0.5f, 0.5f);
        _playerMarkerUI.anchorMax = new Vector2(0.5f, 0.5f);
        _playerMarkerUI.pivot = new Vector2(0.5f, 0.5f);
        _playerMarkerUI.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player == null) return;

        Redraw();
    }

    private void Redraw()
    {
        var world = WorldManager.Instance;
        if (world == null || world.Data == null) return;

        InitializeTexture();
        if (_playerMarkerUI == null) CreatePlayerMarker();

        float centerX = _player.position.x;
        float centerY = _player.position.y;

        int cx = Mathf.RoundToInt(centerX);
        int cy = Mathf.RoundToInt(centerY);
        float step = (_viewRadius * 2f) / _texSize;

        for (int py = 0; py < _texSize; py++)
        {
            float offsetFactorY = (py - _texSize / 2f) * step;
            int wy = cy + Mathf.RoundToInt(offsetFactorY);
            int ly = wy - world.OffsetY;
            int chunkY = Mathf.FloorToInt((float)ly / Chunk.SIZE);

            int rowOffset = py * _texSize;

            for (int px = 0; px < _texSize; px++)
            {
                float offsetFactorX = (px - _texSize / 2f) * step;
                int wx = cx + Mathf.RoundToInt(offsetFactorX);
                int lx = wx - world.OffsetX;
                int chunkX = Mathf.FloorToInt((float)lx / Chunk.SIZE);

                int pixelIndex = rowOffset + px;

                if (lx < 0 || ly < 0 || !world.Data.InBounds(lx, ly))
                {
                    _pixels[pixelIndex] = _fogOfWarColor;
                    continue;
                }

                if (SaveManager.Instance != null && !SaveManager.Instance.IsChunkDiscovered(chunkX, chunkY))
                {
                    _pixels[pixelIndex] = _fogOfWarColor;
                }
                else
                {
                    var block = world.Data.GetBlock(lx, ly);
                    _pixels[pixelIndex] = BlockColorMapper.Get(block);
                }
            }
        }

        _tex.SetPixels(_pixels);
        _tex.Apply();

        List<Transform> toRemove = new List<Transform>();
        foreach (var kvp in _activeMarkers)
        {
            if (kvp.Key == null)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
                continue;
            }
            UpdateMapUIElement(kvp.Key, kvp.Value, centerX, centerY, step);
        }

        foreach (var t in toRemove)
        {
            _activeMarkers.Remove(t);
        }
    }

    public void RegisterMarker(Transform worldObject, MapMarkerType type)
    {
        if (Instance == null)
        {
            _registrationQueue.Add(() => RegisterMarker(worldObject, type));
            return;
        }

        if (_activeMarkers.ContainsKey(worldObject)) return;

        Sprite chosenSprite = _enemySprite;
        if (type == MapMarkerType.Boss) chosenSprite = _bossSprite;
        else if (type == MapMarkerType.Friend) chosenSprite = _friendSprite;

        if (chosenSprite != null && _image != null)
        {
            GameObject markerObj = new GameObject("MinimapMarker_" + type, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerObj.transform.SetParent(_image.rectTransform, false);

            Image img = markerObj.GetComponent<Image>();
            img.sprite = chosenSprite;
            img.raycastTarget = false;

            RectTransform rt = markerObj.GetComponent<RectTransform>();
            rt.sizeDelta = _markerSize;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            _activeMarkers.Add(worldObject, rt);
        }
    }

    public void UnregisterMarker(Transform worldObject)
    {
        if (_activeMarkers.TryGetValue(worldObject, out RectTransform markerUI))
        {
            if (markerUI != null) Destroy(markerUI.gameObject);
            _activeMarkers.Remove(worldObject);
        }
    }

    private void UpdateMapUIElement(Transform worldTransform, RectTransform uiElement, float centerX, float centerY, float step)
    {
        if (worldTransform == null || uiElement == null) return;

        float dx = (worldTransform.position.x - centerX) / step;
        float dy = (worldTransform.position.y - centerY) / step;

        float uiScaleX = _image.rectTransform.rect.width / _texSize;
        float uiScaleY = _image.rectTransform.rect.height / _texSize;

        uiElement.anchoredPosition = new Vector2(dx * uiScaleX, dy * uiScaleY);

        float limitX = _image.rectTransform.rect.width / 2f;
        float limitY = _image.rectTransform.rect.height / 2f;

        bool isInside = Mathf.Abs(uiElement.anchoredPosition.x) <= limitX && Mathf.Abs(uiElement.anchoredPosition.y) <= limitY;
        uiElement.gameObject.SetActive(isInside);
    }
}