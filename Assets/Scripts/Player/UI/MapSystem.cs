using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MapSystem : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public static MapSystem Instance { get; private set; }
    public static bool IsMapOpen { get; private set; } = false;

    [Header("UI Toggle")]
    [SerializeField] private GameObject _mapContent;
    [SerializeField] private GameObject _minimapObject;
    [SerializeField] private InputAction _toggleMapAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/m");

    [Header("Map References")]
    [SerializeField] private RawImage _image;

    [Header("Marker Sprites")]
    [SerializeField] private Sprite _playerSprite;
    [SerializeField] private Sprite _enemySprite;
    [SerializeField] private Sprite _bossSprite;
    [SerializeField] private Sprite _friendSprite;
    [SerializeField] private Vector2 _markerSize = new Vector2(16f, 16f);

    [Header("Map Configurations")]
    [SerializeField] private int _baseViewRadius = 80;
    [SerializeField] private int _texSize = 256;
    [SerializeField] private Color _fogOfWarColor = Color.black;

    [Header("Zoom Settings")]
    [SerializeField] private float _minZoom = 0.5f;
    [SerializeField] private float _maxZoom = 3.0f;
    [SerializeField] private float _zoomSpeed = 0.1f;

    private float _currentZoom = 1.0f;
    private Vector2 _panOffset = Vector2.zero;
    private Texture2D _tex;
    private Transform _player;
    private Color[] _pixels;

    private RectTransform _playerMarkerUI;
    private Dictionary<Transform, RectTransform> _activeMarkers = new Dictionary<Transform, RectTransform>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        _toggleMapAction.Enable();
        InitializeTexture();
    }

    void OnDisable()
    {
        _toggleMapAction.Disable();
        IsMapOpen = false;
    }

    void Start()
    {
        InitializeTexture();

        if (_mapContent == null && transform.childCount > 0)
        {
            _mapContent = transform.GetChild(0).gameObject;
        }

        if (_mapContent != null)
        {
            _mapContent.SetActive(false);
        }
        IsMapOpen = false;
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

        GameObject markerObj = new GameObject("MapMarker_Player", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        markerObj.transform.SetParent(_image.rectTransform, false);

        Image img = markerObj.GetComponent<Image>();
        img.sprite = _playerSprite;
        img.raycastTarget = false;

        _playerMarkerUI = markerObj.GetComponent<RectTransform>();
        _playerMarkerUI.sizeDelta = _markerSize;
        _playerMarkerUI.anchorMin = new Vector2(0.5f, 0.5f);
        _playerMarkerUI.anchorMax = new Vector2(0.5f, 0.5f);
        _playerMarkerUI.pivot = new Vector2(0.5f, 0.5f);
    }

    void Update()
    {
        if (_toggleMapAction.WasPressedThisFrame())
        {
            if (_mapContent != null)
            {
                bool nextState = !_mapContent.activeSelf;
                _mapContent.SetActive(nextState);

                IsMapOpen = nextState;

                if (_minimapObject != null)
                {
                    _minimapObject.SetActive(!nextState);
                }

                if (nextState)
                {
                    _panOffset = Vector2.zero;
                    _currentZoom = 1.0f;
                    InitializeTexture();
                }
            }
        }

        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (_player == null) return;

        var world = WorldManager.Instance;
        if (world != null && SaveManager.Instance != null)
        {
            int pLx = Mathf.RoundToInt(_player.position.x) - world.OffsetX;
            int pLy = Mathf.RoundToInt(_player.position.y) - world.OffsetY;
            int playerChunkX = Mathf.FloorToInt((float)pLx / Chunk.SIZE);
            int playerChunkY = Mathf.FloorToInt((float)pLy / Chunk.SIZE);

            SaveManager.Instance.DiscoverChunk(playerChunkX, playerChunkY);
        }

        if (_mapContent == null || !_mapContent.activeSelf) return;

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                _currentZoom -= Mathf.Sign(scroll) * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            }
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _panOffset = Vector2.zero;
            _currentZoom = 1.0f;
        }

        Redraw();
    }

    private void Redraw()
    {
        var world = WorldManager.Instance;
        if (world == null || world.Data == null) return;

        InitializeTexture();
        if (_playerMarkerUI == null) CreatePlayerMarker();

        int viewRadiusBlocks = Mathf.RoundToInt(_baseViewRadius * _currentZoom);
        int panX = Mathf.RoundToInt(_panOffset.x);
        int panY = Mathf.RoundToInt(_panOffset.y);

        int centerBlockX = Mathf.RoundToInt(_player.position.x) + panX;
        int centerBlockY = Mathf.RoundToInt(_player.position.y) + panY;

        int minBlockX = centerBlockX - viewRadiusBlocks;
        int maxBlockX = centerBlockX + viewRadiusBlocks;
        int minBlockY = centerBlockY - viewRadiusBlocks;
        int maxBlockY = centerBlockY + viewRadiusBlocks;

        for (int py = 0; py < _texSize; py++)
        {
            float tY = (float)py / (_texSize - 1);
            int wy = Mathf.RoundToInt(Mathf.Lerp(minBlockY, maxBlockY, tY));
            int ly = wy - world.OffsetY;
            int chunkY = Mathf.FloorToInt((float)ly / Chunk.SIZE);

            int rowOffset = py * _texSize;

            for (int px = 0; px < _texSize; px++)
            {
                float tX = (float)px / (_texSize - 1);
                int wx = Mathf.RoundToInt(Mathf.Lerp(minBlockX, maxBlockX, tX));
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

        float uiScaleX = _image.rectTransform.rect.width;
        float uiScaleY = _image.rectTransform.rect.height;

        if (_playerMarkerUI != null)
        {
            float relativeX = (float)-panX / (viewRadiusBlocks * 2f);
            float relativeY = (float)-panY / (viewRadiusBlocks * 2f);
            _playerMarkerUI.anchoredPosition = new Vector2(relativeX * uiScaleX, relativeY * uiScaleY);

            float limitX = uiScaleX / 2f;
            float limitY = uiScaleY / 2f;
            bool isPlayerInside = Mathf.Abs(_playerMarkerUI.anchoredPosition.x) <= limitX && Mathf.Abs(_playerMarkerUI.anchoredPosition.y) <= limitY;
            _playerMarkerUI.gameObject.SetActive(isPlayerInside);
        }

        float centerX = _player.position.x + _panOffset.x;
        float centerY = _player.position.y + _panOffset.y;
        float dynamicRadius = _baseViewRadius * _currentZoom;
        float step = (dynamicRadius * 2f) / _texSize;

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
        if (_activeMarkers.ContainsKey(worldObject)) return;

        Sprite chosenSprite = _enemySprite;
        if (type == MapMarkerType.Boss) chosenSprite = _bossSprite;
        else if (type == MapMarkerType.Friend) chosenSprite = _friendSprite;

        if (chosenSprite != null && _image != null)
        {
            GameObject markerObj = new GameObject("MapMarker_" + type, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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

    public void OnDrag(PointerEventData eventData)
    {
        if (_mapContent == null || !_mapContent.activeSelf) return;

        float dynamicRadius = _baseViewRadius * _currentZoom;
        float step = (dynamicRadius * 2f) / _texSize;
        float uiScaleX = _image.rectTransform.rect.width / _texSize;
        float uiScaleY = _image.rectTransform.rect.height / _texSize;

        _panOffset.x -= (eventData.delta.x / uiScaleX) * step;
        _panOffset.y -= (eventData.delta.y / uiScaleY) * step;
    }

    public void OnEndDrag(PointerEventData eventData) { }
}