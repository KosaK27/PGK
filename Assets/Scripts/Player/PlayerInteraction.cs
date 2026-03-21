using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [SerializeField] private BlockType blockToPlace = BlockType.Dirt;

    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private LayerMask enemyLayer;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var cell = GetCellUnderMouse();
            var blockType = WorldManager.Instance.GetBlock(cell.x, cell.y);
            if (blockType == BlockType.Air)
            {
                var mousePos = Mouse.current.position.ReadValue();
                var worldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(mousePos.x, mousePos.y, 0));
                worldPos.z = 0;
                var hits = Physics2D.OverlapCircleAll(worldPos, attackRange, enemyLayer);
                foreach (var hit in hits)
                {
                    var entity = hit.GetComponent<EntityStats>();
                    if (entity != null)
                    {
                        entity.TakeDamage(attackDamage, transform.position);
                        break;
                    }
                }
            }
        }

        if (Mouse.current.leftButton.isPressed)
        {
            var cell = GetCellUnderMouse();
            BlockBreakSystem.Instance.TryBreak(cell, Time.deltaTime);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            BlockBreakSystem.Instance.CancelBreak();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            var cell = GetCellUnderMouse();
            BlockPlaceSystem.Instance.TryPlace(cell, blockToPlace);
        }
    }

    private Vector3Int GetCellUnderMouse()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        return WorldManager.Instance.WorldToCell(worldPos);
    }
}