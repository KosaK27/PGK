using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float blockReach = 5f;

    private bool _smartCursorEnabled = false;
    private Vector3Int _smartTargetCell;
    private bool _hasSmartTarget = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (InventorySystem.Instance == null || WorldManager.Instance == null) return;

        if (MapSystem.IsMapOpen)
        {
            BreakSystem.Instance.CancelBreak();
            MultitileObjectSystem.Instance.CancelBreak();
            return;
        }

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            _smartCursorEnabled = !_smartCursorEnabled;

        HandleBreaking();
        HandlePlacement();
        HandleInteract();
        HandleDropKey();
    }

    private void HandleBreaking()
    {
        var selected = InventorySystem.Instance.SelectedItem;
        bool hasPickaxe = selected != null && !selected.IsEmpty && selected.item.isTool && selected.item.toolType == ToolType.Pickaxe;
        bool hasAxe     = selected != null && !selected.IsEmpty && selected.item.isTool && selected.item.toolType == ToolType.Axe;
        bool hasShovel  = selected != null && !selected.IsEmpty && selected.item.isTool && selected.item.toolType == ToolType.Shovel;

        if (!hasPickaxe && !hasAxe && !hasShovel)
        {
            BreakSystem.Instance.CancelBreak();
            MultitileObjectSystem.Instance.CancelBreak();
            _hasSmartTarget = false;
            return;
        }

        Vector3Int targetCell;

        if (_smartCursorEnabled && hasPickaxe)
        {
            targetCell = GetSmartPickaxeTarget();
            _hasSmartTarget = targetCell.x != int.MinValue;
            _smartTargetCell = _hasSmartTarget ? targetCell : GetCellUnderMouse();
            targetCell = _smartTargetCell;
        }
        else if (_smartCursorEnabled && hasAxe)
        {
            targetCell = GetSmartAxeTarget();
            _hasSmartTarget = targetCell.x != int.MinValue;
            _smartTargetCell = _hasSmartTarget ? targetCell : GetCellUnderMouse();
            targetCell = _smartTargetCell;
        }
        else
        {
            _hasSmartTarget = false;
            targetCell = GetCellUnderMouse();
        }

        if (!IsInReach(targetCell))
        {
            BreakSystem.Instance.CancelBreak();
            MultitileObjectSystem.Instance.CancelBreak();
            return;
        }

        if (Mouse.current.leftButton.isPressed)
        {
            var cellV2 = new Vector2Int(targetCell.x, targetCell.y);
            if (MultitileObjectSystem.Instance.IsOccupied(cellV2))
            {
                BreakSystem.Instance.CancelBreak();
                MultitileObjectSystem.Instance.TryBreak(cellV2, Time.deltaTime);
            }
            else if (hasPickaxe)
            {
                MultitileObjectSystem.Instance.CancelBreak();
                BreakSystem.Instance.TryBreak(targetCell, BreakTarget.Block, Time.deltaTime);
            }
            else if (hasAxe)
            {
                MultitileObjectSystem.Instance.CancelBreak();
                BreakSystem.Instance.TryBreak(targetCell, BreakTarget.Wall, Time.deltaTime);
            }
            else
            {
                BreakSystem.Instance.CancelBreak();
                MultitileObjectSystem.Instance.CancelBreak();
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            BreakSystem.Instance.CancelBreak();
            MultitileObjectSystem.Instance.CancelBreak();
        }
    }

    private Vector3Int GetSmartPickaxeTarget()
    {
        Vector2 playerPos = transform.position;
        Vector2 mouseDir  = ((Vector2)GetMouseWorldPos() - playerPos).normalized;

        bool horizontal = Mathf.Abs(mouseDir.x) >= Mathf.Abs(mouseDir.y);

        int playerCellX = Mathf.FloorToInt(playerPos.x + 0.25f);
        int playerCellY = Mathf.FloorToInt(playerPos.y + 0.25f);

        if (horizontal)
        {
            int dirX   = mouseDir.x >= 0 ? 1 : -1;
            int startX = playerCellX + dirX;

            for (int col = startX; Mathf.Abs(col - startX) <= 3; col += dirX)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    foreach (int sign in new[] { 0, 1, -1 })
                    {
                        var cell = new Vector3Int(col, playerCellY + dy * sign, 0);
                        if (IsValidBreakTarget(cell, BreakTarget.Block))
                            return cell;
                    }
                }
            }
        }
        else
        {
            int dirY = mouseDir.y >= 0 ? 1 : -1;

            int startY = dirY > 0
                ? playerCellY + 2
                : playerCellY - 1;

            for (int row = startY; Mathf.Abs(row - startY) <= 2; row += dirY)
            {
                foreach (int sign in new[] { 0, 1, -1 })
                {
                    var cell = new Vector3Int(playerCellX + sign, row, 0);
                    if (IsValidBreakTarget(cell, BreakTarget.Block))
                        return cell;
                }
            }
        }

        return new Vector3Int(int.MinValue, 0, 0);
    }

    private Vector3Int GetSmartAxeTarget()
    {
        Vector2 playerPos   = transform.position;
        float closestDist   = float.MaxValue;
        Vector3Int closest  = new Vector3Int(int.MinValue, 0, 0);

        for (int x = -3; x <= 3; x++)
        for (int y = -2; y <= 3; y++)
        {
            var cell = new Vector3Int(
                Mathf.FloorToInt(playerPos.x + x + 0.5f),
                Mathf.FloorToInt(playerPos.y + y + 0.5f), 0);

            if (!IsValidBreakTarget(cell, BreakTarget.Wall)) continue;

            float dist = Vector2.Distance(playerPos, new Vector2(cell.x + 0.5f, cell.y + 0.5f));
            if (dist < closestDist) { closestDist = dist; closest = cell; }
        }

        return closest;
    }

    private bool IsValidBreakTarget(Vector3Int cell, BreakTarget target)
    {
        if (!IsInReach(cell)) return false;

        if (target == BreakTarget.Block)
        {
            var block = WorldManager.Instance.GetBlock(cell.x, cell.y);
            if (block == BlockType.Air || block == BlockType.Water) return false;
            if (MultitileObjectSystem.Instance.IsOccupied(new Vector2Int(cell.x, cell.y))) return false;
            if (MultitileObjectSystem.Instance.IsSupporting(new Vector2Int(cell.x, cell.y))) return false;
            return true;
        }
        else
        {
            return WorldManager.Instance.GetWall(cell.x, cell.y) != WallType.None;
        }
    }

    private void HandlePlacement()
    {
        var selected  = InventorySystem.Instance.SelectedItem;
        bool hasTool   = selected != null && !selected.IsEmpty && selected.item.isTool;
        bool hasWeapon = selected != null && !selected.IsEmpty && selected.item.isWeapon;

        if (hasTool || hasWeapon) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        var cell = GetCellUnderMouse();
        if (!IsInReach(cell) || selected == null || selected.IsEmpty) return;

        var cellV2 = new Vector2Int(cell.x, cell.y);
        if (MultitileObjectSystem.Instance.IsOccupied(cellV2)) return;

        if (selected.item.isBlock)
        {
            if (PlaceSystem.Instance.TryPlace(cell, selected.item.blockType))
                InventorySystem.Instance.ConsumeSelected(1);
        }
        else if (selected.item.isWall)
        {
            if (PlaceSystem.Instance.TryPlace(cell, selected.item.wallType))
                InventorySystem.Instance.ConsumeSelected(1);
        }
        else if (selected.item.isMultitileObject && selected.item.multitileObjectDefinition != null)
        {
            var origin = new Vector2Int(cell.x, cell.y);
            var def    = selected.item.multitileObjectDefinition;

            if (def is DoorDefinition doorDef)
            {
                var cloned = Instantiate(doorDef);
                cloned.sourceName    = doorDef.name;
                cloned.openDirection = transform.localScale.x > 0
                    ? DoorOpenDirection.Left
                    : DoorOpenDirection.Right;
                def = cloned;
            }

            if (MultitileObjectSystem.Instance.TryPlace(origin, def))
                InventorySystem.Instance.ConsumeSelected(1);
        }
    }

    private void HandleInteract()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        var cell = GetCellUnderMouse();
        if (!IsInReach(cell)) return;
        var obj = MultitileObjectSystem.Instance.Get(new Vector2Int(cell.x, cell.y));
        obj?.Interact();
    }

    private void HandleDropKey()
    {
        if (!Keyboard.current.qKey.wasPressedThisFrame) return;

        int idx   = InventorySystem.Instance.SelectedHotbarIndex;
        var stack = InventorySystem.Instance.GetSlot(idx);
        if (stack == null || stack.IsEmpty) return;

        int amount     = Keyboard.current.leftCtrlKey.isPressed ? stack.amount : 1;
        Vector2 playerPos = transform.position;
        var mousePos   = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        mouseWorld.z   = 0;
        Vector2 dir    = ((Vector2)mouseWorld - playerPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        ItemDropSystem.Instance.DropItem(new ItemStack(stack.item, amount), playerPos, dir.x);
        InventorySystem.Instance.RemoveItem(stack.item, amount);
    }

    private bool IsInReach(Vector3Int cell)
        => Vector2.Distance(transform.position, new Vector2(cell.x + 0.5f, cell.y + 0.5f)) <= blockReach;

    private Vector3Int GetCellUnderMouse()
        => WorldManager.Instance.WorldToCell(GetMouseWorldPos());

    private Vector3 GetMouseWorldPos()
    {
        var mp = Mouse.current.position.ReadValue();
        return mainCamera.ScreenToWorldPoint(new Vector3(mp.x, mp.y, 0));
    }

    public bool IsSmartCursorEnabled() => _smartCursorEnabled;
    public bool HasSmartTarget()       => _hasSmartTarget && _smartTargetCell.x != int.MinValue;
    public Vector3Int GetSmartTarget() => _smartTargetCell;
}