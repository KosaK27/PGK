using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [SerializeField] private BlockType blockToPlace = BlockType.Dirt;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
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