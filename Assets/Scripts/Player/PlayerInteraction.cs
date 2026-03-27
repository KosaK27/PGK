using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera    mainCamera;
    [SerializeField] private float     attackRange  = 1.5f;
    [SerializeField] private int       attackDamage = 15;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float     blockReach   = 5f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        HandleAttackAndBreak();
        HandlePlacement();
        HandleDropKey();
    }

    private void HandleAttackAndBreak()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var cell = GetCellUnderMouse();
            if (WorldManager.Instance.GetBlock(cell.x, cell.y) == BlockType.Air)
                TryAttack();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            var cell = GetCellUnderMouse();
            if (IsInReach(cell)) BlockBreakSystem.Instance.TryBreak(cell, Time.deltaTime);
            else                 BlockBreakSystem.Instance.CancelBreak();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            BlockBreakSystem.Instance.CancelBreak();
    }

    private void TryAttack()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        worldPos.z   = 0;
        var hits = Physics2D.OverlapCircleAll(worldPos, attackRange, enemyLayer);
        foreach (var hit in hits)
        {
            var entity = hit.GetComponent<EntityStats>();
            if (entity != null) { entity.TakeDamage(attackDamage, transform.position); break; }
        }
    }

    private void HandlePlacement()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) return;
        var cell = GetCellUnderMouse();
        if (!IsInReach(cell)) return;

        var selected = InventorySystem.Instance.SelectedItem;
        if (selected == null || selected.IsEmpty || !selected.item.isBlock) return;

        if (BlockPlaceSystem.Instance.TryPlace(cell, selected.item.blockType))
            InventorySystem.Instance.ConsumeSelected(1);
    }

    private void HandleDropKey()
    {
        if (!Keyboard.current.qKey.wasPressedThisFrame) return;

        int idx   = InventorySystem.Instance.SelectedHotbarIndex;
        var stack = InventorySystem.Instance.GetSlot(idx);
        if (stack == null || stack.IsEmpty) return;

        int amount = Keyboard.current.leftCtrlKey.isPressed ? stack.amount : 1;

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