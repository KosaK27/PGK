using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float  blockReach = 5f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        if (InventorySystem.Instance == null || WorldManager.Instance == null) return;
        HandleBreak();
        HandlePlaceOrWallBreak();
        HandleDropKey();
    }

    private void HandleBreak()
    {
        var selected = InventorySystem.Instance.SelectedItem;
        bool isSword = selected != null && !selected.IsEmpty
                    && selected.item.isTool && selected.item.toolType == ToolType.Sword;
        if (isSword) { BreakSystem.Instance.CancelBreak(); return; }

        if (Mouse.current.leftButton.isPressed)
        {
            var cell = GetCellUnderMouse();
            if (IsInReach(cell)) BreakSystem.Instance.TryBreak(cell, BreakTarget.Block, Time.deltaTime);
            else                 BreakSystem.Instance.CancelBreak();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            BreakSystem.Instance.CancelBreak();
    }

    private void HandlePlaceOrWallBreak()
    {
        var selected = InventorySystem.Instance.SelectedItem;
        bool hasTool = selected != null && !selected.IsEmpty && selected.item.isTool
                    && selected.item.toolType != ToolType.Sword;

        if (Mouse.current.rightButton.isPressed && hasTool)
        {
            var cell = GetCellUnderMouse();
            if (IsInReach(cell)) BreakSystem.Instance.TryBreak(cell, BreakTarget.Wall, Time.deltaTime);
            else                 BreakSystem.Instance.CancelBreak();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame && hasTool)
        {
            BreakSystem.Instance.CancelBreak();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame && !hasTool)
        {
            var cell = GetCellUnderMouse();
            if (!IsInReach(cell)) return;

            if (selected != null && !selected.IsEmpty)
            {
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
            }
        }
    }

    private void HandleDropKey()
    {
        if (!Keyboard.current.qKey.wasPressedThisFrame) return;

        int idx   = InventorySystem.Instance.SelectedHotbarIndex;
        var stack = InventorySystem.Instance.GetSlot(idx);
        if (stack == null || stack.IsEmpty) return;

        int amount         = Keyboard.current.leftCtrlKey.isPressed ? stack.amount : 1;
        Vector2 playerPos  = transform.position;
        var mousePos       = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        mouseWorld.z = 0;
        Vector2 dir = ((Vector2)mouseWorld - playerPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        ItemDropSystem.Instance.DropItem(new ItemStack(stack.item, amount), playerPos, dir.x);
        InventorySystem.Instance.RemoveItem(stack.item, amount);
    }

    private bool IsInReach(Vector3Int cell)
    {
        var cellCenter = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
        return Vector2.Distance(transform.position, cellCenter) <= blockReach;
    }

    private Vector3Int GetCellUnderMouse()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        return WorldManager.Instance.WorldToCell(worldPos);
    }
}