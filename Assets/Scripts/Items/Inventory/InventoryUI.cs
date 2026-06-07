using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainInventoryPanel;
    [SerializeField] private RectTransform hotbarContainer;
    [SerializeField] private Transform mainGridContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject dragIconPrefab;

    [Header("Hotbar Highlight")]
    [SerializeField] private Color normalSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color selectedSlotColor = new Color(0.6f, 0.5f, 0.1f, 1f);

    [Header("Inventory Crafting")]
    [SerializeField] private Button inventoryCraftButton;
    [SerializeField] private CraftingStationDefinition inventoryCraftingDef;

    private List<InventorySlotUI> _hotbarSlots = new();
    private List<InventorySlotUI> _mainSlots = new();
    private bool _isOpen = false;
    private bool _isHolding = false;
    private int _holdFromIndex = -1;
    private IContainer _holdFromContainer = null;
    private ItemStack _holdStack = null;
    private GameObject _holdIcon = null;
    private Image _holdIconImage;
    private TextMeshProUGUI _holdIconCount;

    private Canvas _canvas;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        BuildHotbar();
        BuildMainGrid();
        PositionInventoryBelowHotbar();
        mainInventoryPanel.SetActive(false);

        if (inventoryCraftButton != null)
        {
            inventoryCraftButton.gameObject.SetActive(false);
            inventoryCraftButton.onClick.AddListener(OnInventoryCraftButtonClicked);
        }

        InventorySystem.Instance.OnInventoryChanged += RefreshAll;
        InventorySystem.Instance.OnHotbarSelectionChanged += OnHotbarSelectionChanged;

        RefreshAll();
        OnHotbarSelectionChanged(InventorySystem.Instance.SelectedHotbarIndex);
    }

    void Update()
    {
        HandleToggle();
        HandleHotbarKeys();
        UpdateHoldIconPosition();
        HandleConsumeFromHotbar();

        if (_isHolding && Mouse.current.rightButton.wasPressedThisFrame && !IsPointerOverUI())
        {
            if (_holdStack != null && !_holdStack.IsEmpty && _holdStack.item.isConsumable)
            {
                ConsumableSystem.Instance?.TryUseSelected();
                SetSlotIconVisible(_holdFromIndex, _holdFromContainer, true);
                CancelHold();
            }
            else
            {
                DropToWorld(_holdFromIndex, _holdFromContainer, _holdStack);
                CancelHold();
            }
        }
    }

    private void HandleConsumeFromHotbar()
    {
        if (_isHolding) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        if (IsPointerOverUI()) return;

        var selected = InventorySystem.Instance.SelectedItem;
        if (selected == null || selected.IsEmpty) return;
        if (!selected.item.isConsumable) return;

        ConsumableSystem.Instance?.TryUseSelected();
    }

    public void ForceOpen()
    {
        _isOpen = true;
        mainInventoryPanel.SetActive(true);
        inventoryCraftButton?.gameObject.SetActive(true);
    }

    public void CloseMainPanel()
    {
        if (AccessoryPanelUI.Instance != null && AccessoryPanelUI.Instance.IsOpen) return;
        if (ContainerUIManager.Instance.IsOpen) return;
        if (CraftingUIManager.Instance.IsOpen) return;
        _isOpen = false;
        mainInventoryPanel.SetActive(false);
        inventoryCraftButton?.gameObject.SetActive(false);
        ItemTooltipUI.Instance?.Hide();

        if (_isHolding)
        {
            SetSlotIconVisible(_holdFromIndex, _holdFromContainer, true);
            CancelHold();
        }
    }

    private void PositionInventoryBelowHotbar()
    {
        if (mainInventoryPanel == null || hotbarContainer == null) return;
        var invRect = mainInventoryPanel.GetComponent<RectTransform>();
        invRect.anchorMin = hotbarContainer.anchorMin;
        invRect.anchorMax = hotbarContainer.anchorMax;
        invRect.pivot = new Vector2(0f, 1f);
        invRect.anchoredPosition = new Vector2(
            hotbarContainer.anchoredPosition.x,
            hotbarContainer.anchoredPosition.y - hotbarContainer.rect.height - 8f);
    }

    private void BuildHotbar()
    {
        for (int i = 0; i < InventorySystem.Instance.hotbarSize; i++)
        {
            var go = Instantiate(slotPrefab, hotbarContainer);
            var slot = go.GetComponent<InventorySlotUI>();
            slot.Init(i, OnSlotClicked);
            _hotbarSlots.Add(slot);
        }
    }

    private void BuildMainGrid()
    {
        var inv = InventorySystem.Instance;
        int offset = inv.hotbarSize;
        int total = inv.mainRows * inv.mainCols;
        for (int i = 0; i < total; i++)
        {
            var go = Instantiate(slotPrefab, mainGridContainer);
            var slot = go.GetComponent<InventorySlotUI>();
            slot.Init(offset + i, OnSlotClicked);
            _mainSlots.Add(slot);
        }
    }

    public void RefreshAll()
    {
        var inv = InventorySystem.Instance;
        for (int i = 0; i < _hotbarSlots.Count; i++)
            _hotbarSlots[i].Refresh(inv.GetSlot(i));
        for (int i = 0; i < _mainSlots.Count; i++)
            _mainSlots[i].Refresh(inv.GetSlot(inv.hotbarSize + i));
    }

    private void OnHotbarSelectionChanged(int selectedIndex)
    {
        for (int i = 0; i < _hotbarSlots.Count; i++)
            _hotbarSlots[i].SetHighlight(i == selectedIndex, normalSlotColor, selectedSlotColor);
    }

    private void OnSlotClicked(int slotIndex)
    {
        HandleSlotClick(slotIndex, GetPlayerContainer());
    }

    public void OnContainerSlotClicked(int slotIndex, IContainer container)
    {
        HandleSlotClick(slotIndex, container);
    }

    private void HandleSlotClick(int slotIndex, IContainer container)
    {
        if (!_isHolding)
        {
            var stack = container.GetSlot(slotIndex);
            if (stack == null || stack.IsEmpty) return;

            _isHolding = true;
            _holdFromIndex = slotIndex;
            _holdFromContainer = container;
            _holdStack = new ItemStack(stack.item, stack.amount);

            _holdIcon = Instantiate(dragIconPrefab, _canvas.transform);
            _holdIconImage = _holdIcon.GetComponentInChildren<Image>();
            _holdIconCount = _holdIcon.GetComponentInChildren<TextMeshProUGUI>();
            _holdIconImage.sprite = stack.item.sprite;
            _holdIconCount.text = stack.amount > 1 ? stack.amount.ToString() : "";

            ItemTooltipUI.Instance?.Hide();
            SetSlotIconVisible(_holdFromIndex, _holdFromContainer, false);
        }
        else
        {
            SetSlotIconVisible(_holdFromIndex, _holdFromContainer, true);
            MoveSlotBetweenContainers(_holdFromContainer, _holdFromIndex, container, slotIndex);
            CancelHold();
        }
    }

    private void MoveSlotBetweenContainers(IContainer from, int fromIdx, IContainer to, int toIdx)
    {
        var fromStack = from.GetSlot(fromIdx);

        if (to is AccessoryContainer)
        {
            if (fromStack == null || fromStack.IsEmpty || !fromStack.item.isAccessory || fromStack.item.accessoryDefinition == null)
                return;
        }

        if (to is ArmorContainer)
        {
            if (fromStack == null || fromStack.IsEmpty || !fromStack.item.isArmor || fromStack.item.armorDefinition == null)
                return;
            if (fromStack.item.armorDefinition.Slot != (ArmorSlot)toIdx)
                return;
        }

        if (from is AccessoryContainer && to is not AccessoryContainer)
        {
            if (to.GetSlot(toIdx) != null && !to.GetSlot(toIdx).IsEmpty)
            {
                var toStack = to.GetSlot(toIdx);
                if (!toStack.item.isAccessory || toStack.item.accessoryDefinition == null)
                    return;
            }
        }

        if (from is ArmorContainer && to is not ArmorContainer)
        {
            var toStack = to.GetSlot(toIdx);
            if (toStack != null && !toStack.IsEmpty)
            {
                if (!toStack.item.isArmor || toStack.item.armorDefinition == null)
                    return;
                if (toStack.item.armorDefinition.Slot != (ArmorSlot)fromIdx)
                    return;
            }
        }

        var fromStackFinal = from.GetSlot(fromIdx);
        var toStackFinal = to.GetSlot(toIdx);

        if (fromStackFinal == null || fromStackFinal.IsEmpty)
        {
            from.SetSlot(fromIdx, toStackFinal);
            to.SetSlot(toIdx, null);
            return;
        }

        if (toStackFinal != null && !toStackFinal.IsEmpty && toStackFinal.item == fromStackFinal.item)
        {
            int space = toStackFinal.item.maxStack - toStackFinal.amount;
            if (space > 0)
            {
                int move = Mathf.Min(space, fromStackFinal.amount);
                toStackFinal.amount += move;
                fromStackFinal.amount -= move;
                from.SetSlot(fromIdx, fromStackFinal.amount <= 0 ? null : fromStackFinal);
                to.SetSlot(toIdx, toStackFinal);
                return;
            }
        }

        to.SetSlot(toIdx, fromStackFinal);
        from.SetSlot(fromIdx, toStackFinal);
    }

    private void SetSlotIconVisible(int index, IContainer container, bool visible)
    {
        if (container is AccessoryContainer)
        {
            AccessoryPanelUI.Instance?.SetSlotIconVisible(index, visible);
            return;
        }
        if (container is ArmorContainer)
        {
            ArmorPanelUI.Instance?.SetSlotIconVisible(index, visible);
            return;
        }
        if (container == GetPlayerContainer())
            GetPlayerSlotUI(index)?.SetIconVisible(visible);
        else
            ContainerUI.Instance?.SetSlotIconVisible(index, visible);
    }

    private void UpdateHoldIconPosition()
    {
        if (!_isHolding || _holdIcon == null) return;
        var mp = Mouse.current.position.ReadValue();
        _holdIcon.transform.position = new Vector3(mp.x, mp.y, 0f);
    }

    private void CancelHold()
    {
        _isHolding = false;
        _holdFromIndex = -1;
        _holdFromContainer = null;
        _holdStack = null;
        if (_holdIcon != null) { Destroy(_holdIcon); _holdIcon = null; }
    }

    private void HandleToggle()
    {
        if (!Keyboard.current.tabKey.wasPressedThisFrame) return;

        if (ContainerUIManager.Instance.IsOpen)
        {
            ContainerUIManager.Instance.CloseContainer();
            return;
        }

        if (CraftingUIManager.Instance.IsOpen)
        {
            CraftingUIManager.Instance.CloseStation();
            return;
        }

        if (AccessoryPanelUI.Instance != null && AccessoryPanelUI.Instance.IsOpen)
        {
            AccessoryPanelUI.Instance.Close();
            return;
        }

        _isOpen = !_isOpen;
        mainInventoryPanel.SetActive(_isOpen);
        inventoryCraftButton?.gameObject.SetActive(_isOpen);

        if (!_isOpen)
        {
            ItemTooltipUI.Instance?.Hide();
            if (_isHolding)
            {
                SetSlotIconVisible(_holdFromIndex, _holdFromContainer, true);
                CancelHold();
            }
        }
    }

    private void OnInventoryCraftButtonClicked()
    {
        if (CraftingUIManager.Instance.IsOpen)
            CraftingUIManager.Instance.CloseStation();
        else
            CraftingUIManager.Instance.OpenStation(inventoryCraftingDef, null);
    }

    private void HandleHotbarKeys()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(0); return; }
        if (Keyboard.current.digit2Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(1); return; }
        if (Keyboard.current.digit3Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(2); return; }
        if (Keyboard.current.digit4Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(3); return; }
        if (Keyboard.current.digit5Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(4); return; }
        if (Keyboard.current.digit6Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(5); return; }
        if (Keyboard.current.digit7Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(6); return; }
        if (Keyboard.current.digit8Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(7); return; }
        if (Keyboard.current.digit9Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(8); return; }
        if (Keyboard.current.digit0Key.wasPressedThisFrame) { InventorySystem.Instance.SelectHotbarSlot(9); return; }

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int cur = InventorySystem.Instance.SelectedHotbarIndex;
            int next = (int)Mathf.Repeat(cur - Mathf.Sign(scroll), InventorySystem.Instance.hotbarSize);
            InventorySystem.Instance.SelectHotbarSlot(next);
        }
    }

    private void DropToWorld(int slotIndex, IContainer container, ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector2 playerPos = player.transform.position;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        mouseWorld.z = 0;
        Vector2 dir = ((Vector2)mouseWorld - playerPos).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        ItemDropSystem.Instance.DropItem(stack, playerPos, dir.x);
        container.SetSlot(slotIndex, null);
    }

    private bool IsPointerOverUI()
    {
        var eventData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }

    private IContainer GetPlayerContainer()
    {
        return new PlayerInventoryContainer();
    }

    public void OpenMainPanel()
    {
        mainInventoryPanel.SetActive(true);
        inventoryCraftButton?.gameObject.SetActive(true);
    }

    private InventorySlotUI GetPlayerSlotUI(int index)
    {
        var inv = InventorySystem.Instance;
        if (index < inv.hotbarSize)
            return index < _hotbarSlots.Count ? _hotbarSlots[index] : null;
        int mainIdx = index - inv.hotbarSize;
        return mainIdx < _mainSlots.Count ? _mainSlots[mainIdx] : null;
    }
}