using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CursorSystem : MonoBehaviour
{
    public static CursorSystem Instance { get; private set; }

    [Header("Cursor")]
    [SerializeField] private Image cursorImage;
    [SerializeField] private RectTransform cursorRect;
    [SerializeField] private Vector2 cursorSize = new Vector2(32f, 32f);

    [Header("Item Icon")]
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform iconRect;
    [SerializeField] private Vector2 iconSize = new Vector2(20f, 20f);
    [SerializeField] private Vector2 iconOffset = new Vector2(14f, -14f);

    private ItemStack _overrideStack;
    private bool _hasOverride;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        Cursor.visible = false;

        cursorRect.pivot = new Vector2(0f, 1f);
        cursorRect.sizeDelta = cursorSize;

        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.sizeDelta = iconSize;

        iconImage.enabled = false;
    }

    void OnDestroy()
    {
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var screenPos = new Vector3(mousePos.x, mousePos.y, 0f);

        cursorRect.position = screenPos;
        iconRect.position = new Vector3(screenPos.x + iconOffset.x, screenPos.y + iconOffset.y, 0f);

        UpdateIcon();
    }

    public void SetOverrideItem(ItemStack stack)
    {
        _overrideStack = stack;
        _hasOverride = true;
    }

    public void ClearOverride()
    {
        _overrideStack = null;
        _hasOverride = false;
    }

    private void UpdateIcon()
    {
        ItemStack stack = _hasOverride
            ? _overrideStack
            : InventorySystem.Instance.SelectedItem;

        bool hasItem = stack != null && !stack.IsEmpty;
        iconImage.enabled = hasItem;

        if (!hasItem) return;
        iconImage.sprite = stack.item.sprite;
    }
}