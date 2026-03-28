using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] public int hotbarSize = 10;
    [SerializeField] public int mainRows = 4;
    [SerializeField] public int mainCols = 10;

    public int TotalSlots => hotbarSize + mainRows * mainCols;

    private ItemStack[] _slots;

    public int SelectedHotbarIndex { get; private set; } = 0;
    public ItemStack SelectedItem => _slots[SelectedHotbarIndex];

    public event Action OnInventoryChanged;
    public event Action<int> OnHotbarSelectionChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        _slots = new ItemStack[TotalSlots];
    }

    public ItemStack GetSlot(int index) => _slots[index];

    public void SetSlot(int index, ItemStack stack)
    {
        _slots[index] = stack;
        OnInventoryChanged?.Invoke();
    }

    public int AddItem(ItemStack incoming)
    {
        int remaining = incoming.amount;

        for (int i = 0; i < TotalSlots && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot == null || slot.IsEmpty || slot.item != incoming.item) continue;
            int space = slot.item.maxStack - slot.amount;
            if (space <= 0) continue;
            int add = Mathf.Min(space, remaining);
            slot.amount += add;
            remaining -= add;
        }

        for (int i = 0; i < TotalSlots && remaining > 0; i++)
        {
            if (_slots[i] != null && !_slots[i].IsEmpty) continue;
            int add = Mathf.Min(incoming.item.maxStack, remaining);
            _slots[i] = new ItemStack(incoming.item, add);
            remaining -= add;
        }

        if (remaining != incoming.amount)
            OnInventoryChanged?.Invoke();

        return remaining;
    }

    public int RemoveItem(ItemDefinition item, int amount)
    {
        int removed = 0;
        for (int i = 0; i < TotalSlots && removed < amount; i++)
        {
            var slot = _slots[i];
            if (slot == null || slot.IsEmpty || slot.item != item) continue;
            int take = Mathf.Min(slot.amount, amount - removed);
            slot.amount -= take;
            removed += take;
            if (slot.amount <= 0) _slots[i] = null;
        }
        if (removed > 0) OnInventoryChanged?.Invoke();
        return removed;
    }

    public void ConsumeSelected(int amount = 1)
    {
        var slot = _slots[SelectedHotbarIndex];
        if (slot == null || slot.IsEmpty) return;
        slot.amount -= amount;
        if (slot.amount <= 0) _slots[SelectedHotbarIndex] = null;
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(ItemDefinition item, int amount = 1)
    {
        int count = 0;
        foreach (var s in _slots)
            if (s != null && !s.IsEmpty && s.item == item) count += s.amount;
        return count >= amount;
    }

    public void MoveSlot(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        var from = _slots[fromIndex];
        var to   = _slots[toIndex];

        if (from == null || from.IsEmpty)
        {
            _slots[fromIndex] = to;
            _slots[toIndex]   = null;
            OnInventoryChanged?.Invoke();
            return;
        }

        if (to != null && !to.IsEmpty && to.item == from.item)
        {
            int space = to.item.maxStack - to.amount;
            if (space > 0)
            {
                int move = Mathf.Min(space, from.amount);
                to.amount += move;
                from.amount -= move;
                if (from.amount <= 0) _slots[fromIndex] = null;
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        _slots[toIndex]   = from;
        _slots[fromIndex] = to;
        OnInventoryChanged?.Invoke();
    }

    public void SelectHotbarSlot(int index)
    {
        index = Mathf.Clamp(index, 0, hotbarSize - 1);
        if (SelectedHotbarIndex == index) return;
        SelectedHotbarIndex = index;
        OnHotbarSelectionChanged?.Invoke(index);
    }
}