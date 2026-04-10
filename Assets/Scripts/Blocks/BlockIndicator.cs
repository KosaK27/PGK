using UnityEngine;
using UnityEngine.InputSystem;

public class BlockIndicator : MonoBehaviour
{
    [SerializeField] private Color _placeColor = new(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color _breakIdleColor = new(1f, 0.3f, 0.1f, 0.5f);
    [SerializeField] private Color _breakActiveColor = new(1f, 0.3f, 0.1f, 0.8f);
    [SerializeField] private Camera _cam;
    [SerializeField] private float _reach = 5f;

    private SpriteRenderer _placeSr;
    private SpriteRenderer _breakSr;
    private GameObject _placeGo;
    private GameObject _breakGo;

    void Awake()
    {
        if (_cam == null) _cam = Camera.main;
        _placeGo = MakeIndicator("PlaceHologram", 1);
        _breakGo = MakeIndicator("BreakHighlight", 199);
        _placeSr = _placeGo.GetComponent<SpriteRenderer>();
        _breakSr = _breakGo.GetComponent<SpriteRenderer>();
        _placeSr.enabled = false;
        _breakSr.enabled = false;
    }

    void Update()
    {
        if (InventorySystem.Instance == null || WorldManager.Instance == null) return;

        var item = InventorySystem.Instance.SelectedItem;
        bool hasBlock = item != null && !item.IsEmpty && item.item != null && item.item.isBlock;
        bool hasWall = item != null && !item.IsEmpty && item.item != null && item.item.isWall;
        bool hasMultitile = item != null && !item.IsEmpty && item.item != null
            && item.item.isMultitileObject && item.item.multitileObjectDefinition != null;
        bool hasTool = item != null && !item.IsEmpty && item.item != null && item.item.isTool;

        var cell = CellUnderMouse();
        bool inReach = InReach(cell);
        var cellV2 = new Vector2Int(cell.x, cell.y);

        bool cellHasBlock = WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air;
        bool cellHasWall = WorldManager.Instance.GetWall(cell.x, cell.y) != WallType.None;
        bool cellHasObject = MultitileObjectSystem.Instance != null && MultitileObjectSystem.Instance.IsOccupied(cellV2);
        bool cellIsSupporting = MultitileObjectSystem.Instance != null && MultitileObjectSystem.Instance.IsSupporting(cellV2);
        bool rightHeld = Mouse.current.rightButton.isPressed;

        if (inReach && hasMultitile)
        {
            var def = item.item.multitileObjectDefinition;
            bool canPlace = MultitileAreaIsClear(cell, def.size) && HasAllSupportBelow(cell, def.size);
            _placeSr.enabled = canPlace;
            if (canPlace)
            {
                _placeSr.sprite = item.item.sprite;
                _placeSr.color = _placeColor;
                _placeGo.transform.localScale = new Vector3(1f, 1f, 1f);
                _placeGo.transform.position = new Vector3(cell.x + def.size.x * 0.5f, cell.y + def.size.y * 0.5f);
            }
        }
        else if (inReach && hasBlock && !cellHasBlock && !cellHasObject && HasNeighbor(cell))
        {
            _placeSr.enabled = true;
            _placeSr.sprite = item.item.sprite;
            _placeSr.color = _placeColor;
            _placeGo.transform.localScale = Vector3.one;
            _placeGo.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
        }
        else if (inReach && hasWall && !cellHasWall && rightHeld)
        {
            _placeSr.enabled = true;
            _placeSr.sprite = item.item.sprite;
            _placeSr.color = _placeColor;
            _placeGo.transform.localScale = Vector3.one;
            _placeGo.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
        }
        else
        {
            _placeSr.enabled = false;
        }

        bool breaking = false;
        float progress = 0f;
        bool pressing = false;

        if (hasTool && inReach && cellHasObject)
        {
            breaking = true;
            pressing = Mouse.current.leftButton.isPressed;
            progress = MultitileObjectSystem.Instance.GetBreakProgress(cellV2);
            var obj = MultitileObjectSystem.Instance.Get(cellV2);
            if (obj != null)
            {
                float s = 1f + progress * 0.12f;
                _breakGo.transform.position = new Vector3(
                    obj.Origin.x + obj.Definition.size.x * 0.5f,
                    obj.Origin.y + obj.Definition.size.y * 0.5f);
                _breakGo.transform.localScale = new Vector3(obj.Definition.size.x * s, obj.Definition.size.y * s, 1f);
            }
        }
        else if (hasTool && inReach && cellHasBlock && !cellIsSupporting)
        {
            breaking = true;
            pressing = Mouse.current.leftButton.isPressed;
            progress = BreakSystem.Instance.GetBreakProgress(cell, BreakTarget.Block);
            float s = 1f + progress * 0.12f;
            _breakGo.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
            _breakGo.transform.localScale = new Vector3(s, s, 1f);
        }
        else if (hasTool && inReach && cellHasWall)
        {
            breaking = true;
            pressing = Mouse.current.rightButton.isPressed;
            progress = BreakSystem.Instance.GetBreakProgress(cell, BreakTarget.Wall);
            float s = 1f + progress * 0.12f;
            _breakGo.transform.position = new Vector3(cell.x + 0.5f, cell.y + 0.5f);
            _breakGo.transform.localScale = new Vector3(s, s, 1f);
        }

        if (breaking)
        {
            _breakSr.enabled = true;
            float pulse = pressing ? 0.5f + 0.15f * Mathf.Sin(Time.time * 14f) : 0f;
            _breakSr.color = pressing
                ? new Color(_breakActiveColor.r, _breakActiveColor.g, _breakActiveColor.b, _breakActiveColor.a + pulse)
                : _breakIdleColor;
        }
        else _breakSr.enabled = false;
    }

    private bool MultitileAreaIsClear(Vector3Int origin, Vector2Int size)
    {
        for (int y = 0; y < size.y; y++)
        for (int x = 0; x < size.x; x++)
        {
            var c = new Vector2Int(origin.x + x, origin.y + y);
            if (MultitileObjectSystem.Instance.IsOccupied(c)) return false;
            if (WorldManager.Instance.GetBlock(c.x, c.y) != BlockType.Air) return false;
        }
        return true;
    }

    private bool HasAllSupportBelow(Vector3Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            var below = new Vector2Int(origin.x + x, origin.y - 1);
            if (WorldManager.Instance.GetBlock(below.x, below.y) == BlockType.Air) return false;
        }
        return true;
    }

    private GameObject MakeIndicator(string name, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return go;
    }

    private bool InReach(Vector3Int cell)
        => Vector2.Distance(transform.position, new Vector2(cell.x + 0.5f, cell.y + 0.5f)) <= _reach;

    private Vector3Int CellUnderMouse()
    {
        var mp = Mouse.current.position.ReadValue();
        return WorldManager.Instance.WorldToCell(_cam.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 0)));
    }

    private bool HasNeighbor(Vector3Int cell)
    {
        Vector3Int[] n = { cell + Vector3Int.up, cell + Vector3Int.down, cell + Vector3Int.left, cell + Vector3Int.right };
        foreach (var nb in n)
            if (WorldManager.Instance.GetBlock(nb.x, nb.y) != BlockType.Air) return true;
        return false;
    }

    void OnDestroy() { if (_placeGo) Destroy(_placeGo); if (_breakGo) Destroy(_breakGo); }
}