using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AccessoryPanelUI : MonoBehaviour
{
    public static AccessoryPanelUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform slotsContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Registry")]
    [SerializeField] private ItemRegistry itemRegistry;

    private List<InventorySlotUI> _slots = new();
    private AccessoryContainer _container;
    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _container = new AccessoryContainer(itemRegistry);
        _container.OnChanged += RefreshAll;
        BuildSlots();
        RefreshAll();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
            Toggle();
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
        panel.SetActive(_isOpen);

        if (_isOpen)
            InventoryUI.Instance.ForceOpen();
        else
            InventoryUI.Instance.CloseMainPanel();
    }

    public void Close()
    {
        _isOpen = false;
        panel.SetActive(false);
    }

    private void BuildSlots()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        _slots.Clear();

        for (int i = 0; i < AccessorySystem.Instance.SlotCount; i++)
        {
            var go = Instantiate(slotPrefab, slotsContainer);
            var slot = go.GetComponent<InventorySlotUI>();
            int idx = i;
            slot.Init(idx, OnSlotClicked);
            _slots.Add(slot);
        }
    }

    private void RefreshAll()
    {
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Refresh(_container.GetSlot(i));
    }

    private void OnSlotClicked(int index)
    {
        InventoryUI.Instance.OnContainerSlotClicked(index, _container);
    }

    public void SetSlotIconVisible(int index, bool visible)
    {
        if (index >= 0 && index < _slots.Count)
            _slots[index].SetIconVisible(visible);
    }
}