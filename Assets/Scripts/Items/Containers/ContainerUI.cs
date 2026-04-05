using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContainerUI : MonoBehaviour
{
    public static ContainerUI Instance { get; private set; }

    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private GameObject _slotPrefab;
    [SerializeField] private TextMeshProUGUI _titleLabel;

    [Header("Slot Colors")]
    [SerializeField] private Color _normalColor = new(0.15f, 0.2f, 0.3f, 0.9f);
    [SerializeField] private Color _selectedColor = new(0.3f, 0.4f, 0.6f, 1f);

    private readonly List<InventorySlotUI> _slots = new();
    private IContainer _container;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _panel.SetActive(false);
        ContainerUIManager.Instance.OnContainerOpened += Open;
        ContainerUIManager.Instance.OnContainerClosed += Close;
    }

    void OnDestroy()
    {
        if (ContainerUIManager.Instance == null) return;
        ContainerUIManager.Instance.OnContainerOpened -= Open;
        ContainerUIManager.Instance.OnContainerClosed -= Close;
    }

    private void Open(IContainer container)
    {
        if (_container != null) _container.OnChanged -= Refresh;

        _container = container;
        _container.OnChanged += Refresh;

        if (_titleLabel != null && ContainerUIManager.Instance.OpenObject?.Definition != null)
            _titleLabel.text = ContainerUIManager.Instance.OpenObject.Definition.displayName;

        BuildSlots();
        Refresh();
        _panel.SetActive(true);
        InventoryUI.Instance.OpenMainPanel();
    }

    private void Close()
    {
        if (_container != null) _container.OnChanged -= Refresh;
        _container = null;
        _panel.SetActive(false);
    }

    private void BuildSlots()
    {
        foreach (Transform child in _gridContainer) Destroy(child.gameObject);
        _slots.Clear();

        for (int i = 0; i < _container.SlotCount; i++)
        {
            var go = Instantiate(_slotPrefab, _gridContainer);
            var slot = go.GetComponent<InventorySlotUI>();
            int idx = i;
            slot.Init(idx, OnSlotClicked);
            SetSlotColor(slot, _normalColor);
            _slots.Add(slot);
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Refresh(_container.GetSlot(i));
    }

    private void OnSlotClicked(int index)
    {
        InventoryUI.Instance.OnContainerSlotClicked(index, _container);
    }

    public void RefreshSlot(int index)
    {
        if (index >= 0 && index < _slots.Count)
            _slots[index].Refresh(_container.GetSlot(index));
    }

    public void SetSlotIconVisible(int index, bool visible)
    {
        if (index >= 0 && index < _slots.Count)
            _slots[index].SetIconVisible(visible);
    }

    private void SetSlotColor(InventorySlotUI slot, Color color)
    {
        slot.SetHighlight(false, color, _selectedColor);
    }
}