using UnityEngine;
using UnityEngine.InputSystem;

public class BlockIndicator : MonoBehaviour
{
    [Header("Place Hologram")]
    [SerializeField] private Color hologramColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("Break Highlight")]
    [SerializeField] private Color breakIdleColor = new Color(1f, 0.3f, 0.1f, 0.5f);
    [SerializeField] private Color breakActiveColor = new Color(1f, 0.3f, 0.1f, 0.8f);

    [Header("Refs")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float blockReach = 5f;

    private SpriteRenderer _placeRenderer;
    private SpriteRenderer _breakRenderer;
    private GameObject _placeGO;
    private GameObject _breakGO;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        _placeGO = CreateIndicator("_PlaceHologram", 200);
        _breakGO = CreateIndicator("_BreakHighlight", 199);

        _placeRenderer = _placeGO.GetComponent<SpriteRenderer>();
        _breakRenderer = _breakGO.GetComponent<SpriteRenderer>();

        _placeRenderer.enabled = false;
        _breakRenderer.enabled = false;
    }

    void Update()
    {
        if (InventorySystem.Instance == null || WorldManager.Instance == null) return;

        var selected = InventorySystem.Instance.SelectedItem;
        bool hasBlock = selected != null && !selected.IsEmpty && selected.item != null && selected.item.isBlock;
        bool hasWall = selected != null && !selected.IsEmpty && selected.item != null && selected.item.isWall;
        bool hasTool = selected != null && !selected.IsEmpty && selected.item != null
                       && selected.item.isTool;

        var cell = GetCellUnderMouse();
        bool inReach = IsInReach(cell);

        bool cellHasBlock = WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air;
        bool cellHasWall = WorldManager.Instance.GetWall(cell.x, cell.y) != WallType.None;

        bool rightHeld = Mouse.current.rightButton.isPressed;

        if (inReach && hasBlock && !cellHasBlock && HasNeighbor(cell))
            ShowPlace(cell, selected.item.sprite);
        else if (inReach && hasWall && !cellHasWall && rightHeld)
            ShowPlace(cell, selected.item.sprite);
        else
            _placeRenderer.enabled = false;

        BreakTarget? target = null;
        if (hasTool && inReach && cellHasBlock) target = BreakTarget.Block;
        else if (hasTool && inReach && cellHasWall) target = BreakTarget.Wall;

        if (target.HasValue)
        {
            _breakRenderer.enabled = true;
            _breakGO.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            bool pressing = target == BreakTarget.Block
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.rightButton.isPressed;

            float progress = BreakSystem.Instance.GetBreakProgress(cell, target.Value);
            float pulse = pressing ? 0.5f + 0.15f * Mathf.Sin(Time.time * 14f) : 0f;

            _breakRenderer.color = pressing
                ? new Color(breakActiveColor.r, breakActiveColor.g,
                            breakActiveColor.b, breakActiveColor.a + pulse)
                : breakIdleColor;

            float s = 1f + progress * 0.12f;
            _breakGO.transform.localScale = new Vector3(s, s, 1f);
        }
        else
        {
            _breakRenderer.enabled = false;
        }
    }

    void ShowPlace(Vector3Int cell, Sprite sprite)
    {
        _placeRenderer.enabled = true;
        _placeRenderer.sprite = sprite;
        _placeRenderer.color = hologramColor;
        _placeGO.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    bool IsInReach(Vector3Int cell)
    {
        var center = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        return Vector2.Distance(transform.position, center) <= blockReach;
    }

    Vector3Int GetCellUnderMouse()
    {
        var mp = Mouse.current.position.ReadValue();
        var wp = mainCamera.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 0));
        return WorldManager.Instance.WorldToCell(wp);
    }

    bool HasNeighbor(Vector3Int cell)
    {
        Vector3Int[] n = {
            cell + Vector3Int.up, cell + Vector3Int.down,
            cell + Vector3Int.left, cell + Vector3Int.right
        };
        foreach (var nb in n)
            if (WorldManager.Instance.GetBlock(nb.x, nb.y) != BlockType.Air)
                return true;
        return false;
    }

    GameObject CreateIndicator(string goName, int sortOrder)
    {
        var go = new GameObject(goName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortOrder;
        sr.sprite = CreateWhiteSquareSprite();
        go.transform.localScale = Vector3.one;
        return go;
    }

    Sprite CreateWhiteSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    void OnDestroy()
    {
        if (_placeGO != null) Destroy(_placeGO);
        if (_breakGO != null) Destroy(_breakGO);
    }
}