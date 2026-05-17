using UnityEngine;

public class DoorObject : MultitileObject
{
    public DoorDefinition DoorDefinition { get; private set; }
    public bool IsOpen { get; private set; }

    private SpriteRenderer _sr;
    private BoxCollider2D _col;

    public void InitializeDoor(DoorDefinition def, Vector2Int origin)
    {
        DoorDefinition = def;
        def.sprite = def.closedSprite;
        def.size = def.closedSize;
        Initialize(def, origin);
        _sr = GetComponentInChildren<SpriteRenderer>();
        _col = GetComponent<BoxCollider2D>();
    }

    public override void Interact()
    {
        if (IsOpen) Close();
        else Open();
    }

    private bool CanOpen()
    {
        bool opensLeft = DoorDefinition.openDirection == DoorOpenDirection.Left;
        int extra = DoorDefinition.openSize.x - DoorDefinition.closedSize.x;

        for (int y = 0; y < DoorDefinition.openSize.y; y++)
        for (int i = 1; i <= extra; i++)
        {
            int checkX = opensLeft
                ? Origin.x - i
                : Origin.x + DoorDefinition.closedSize.x + (i - 1);
            var cell = new Vector2Int(checkX, Origin.y + y);
            if (MultitileObjectSystem.Instance.IsOccupied(cell)) return false;
            if (WorldManager.Instance.GetBlock(cell.x, cell.y) != BlockType.Air) return false;
        }
        return true;
    }

    private void Open()
    {
        if (!CanOpen()) return;
        IsOpen = true;

        bool opensLeft = DoorDefinition.openDirection == DoorOpenDirection.Left;
        int extra = DoorDefinition.openSize.x - DoorDefinition.closedSize.x;

        _sr.sprite = DoorDefinition.openSprite;
        _sr.flipX = opensLeft;

        float spriteX = opensLeft
            ? DoorDefinition.closedSize.x - DoorDefinition.openSize.x * 0.5f
            : DoorDefinition.openSize.x * 0.5f;

        _sr.transform.localPosition = new Vector3(spriteX, DoorDefinition.openSize.y * 0.5f, 0f);

        if (_col != null) _col.enabled = false;

        MultitileObjectSystem.Instance.UpdateCellMap(this, DoorDefinition.closedSize, DoorDefinition.openSize);
    }

    private void Close()
    {
        IsOpen = false;

        _sr.sprite = DoorDefinition.closedSprite;
        _sr.flipX = false;
        _sr.transform.localPosition = new Vector3(
            DoorDefinition.closedSize.x * 0.5f,
            DoorDefinition.closedSize.y * 0.5f,
            0f);

        if (_col != null)
        {
            _col.enabled = true;
            _col.size = new Vector2(DoorDefinition.closedSize.x, DoorDefinition.closedSize.y);
            _col.offset = new Vector2(DoorDefinition.closedSize.x * 0.5f, DoorDefinition.closedSize.y * 0.5f);
        }

        MultitileObjectSystem.Instance.UpdateCellMap(this, DoorDefinition.openSize, DoorDefinition.closedSize);
    }

    public Vector2Int CurrentSize => IsOpen ? DoorDefinition.openSize : DoorDefinition.closedSize;

    public Vector2Int CurrentOrigin =>
        IsOpen && DoorDefinition.openDirection == DoorOpenDirection.Left
            ? new Vector2Int(Origin.x - (DoorDefinition.openSize.x - DoorDefinition.closedSize.x), Origin.y)
            : Origin;
}