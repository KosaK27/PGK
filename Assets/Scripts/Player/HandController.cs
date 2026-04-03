using UnityEngine;
using UnityEngine.InputSystem;

public class HandController : MonoBehaviour
{
    [SerializeField] private Transform toolRoot;

    private SpriteRenderer _toolRenderer;
    private ToolSwing _toolSwing;
    private MeleeWeapon _meleeWeapon;
    private PlayerArmAnimator _arm;
    private PlayerMovement _movement;

    void Awake()
    {
        _arm = GetComponent<PlayerArmAnimator>();
        _toolSwing = GetComponent<ToolSwing>();
        _meleeWeapon = GetComponent<MeleeWeapon>();
        _movement = GetComponent<PlayerMovement>();

        if (toolRoot != null)
            _toolRenderer = toolRoot.GetComponentInChildren<SpriteRenderer>();

        _toolSwing.Setup(toolRoot, _toolRenderer);
        _meleeWeapon.Setup(toolRoot, _toolRenderer);
    }

    void Update()
    {
        var selected = InventorySystem.Instance?.SelectedItem;
        bool hasItem = selected != null && !selected.IsEmpty && selected.item != null;

        if (!hasItem || !selected.item.IsHandheld)
        {
            HideAll();
            _movement.ActionState = PlayerActionState.None;
            return;
        }

        bool lmbHeld = Mouse.current.leftButton.isPressed;
        bool lmbPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool lmbReleased = Mouse.current.leftButton.wasReleasedThisFrame;

        if (selected.item.isWeapon)
        {
            _meleeWeapon.UpdateWeapon(selected.item, lmbPressed, lmbHeld);
            _movement.ActionState = _meleeWeapon.IsSwinging
                ? PlayerActionState.UsingWeapon
                : PlayerActionState.None;
        }
        else if (selected.item.isTool)
        {
            _toolSwing.UpdateSwing(selected.item, lmbHeld);
            _movement.ActionState = lmbHeld
                ? PlayerActionState.UsingTool
                : PlayerActionState.None;
            if (lmbReleased) _meleeWeapon.Cancel();
        }
    }

    private void HideAll()
    {
        if (_toolRenderer != null) _toolRenderer.enabled = false;
        _toolSwing.Cancel();
        _meleeWeapon.Cancel();
    }
}