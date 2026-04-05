using System;
using UnityEngine;

public class ContainerData : IContainer
{
    public int SlotCount { get; }
    public event Action OnChanged;

    private readonly ItemStack[] _slots;

    public ContainerData(int slotCount)
    {
        SlotCount = slotCount;
        _slots = new ItemStack[slotCount];
    }

    public ItemStack GetSlot(int index) => _slots[index];

    public void SetSlot(int index, ItemStack stack)
    {
        if (index < 0 || index >= SlotCount) return;
        _slots[index] = stack;
        OnChanged?.Invoke();
    }

    public int AddItem(ItemStack incoming)
    {
        int remaining = incoming.amount;

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            var slot = _slots[i];
            if (slot == null || slot.IsEmpty || slot.item != incoming.item) continue;
            int space = slot.item.maxStack - slot.amount;
            if (space <= 0) continue;
            int add = Mathf.Min(space, remaining);
            slot.amount += add;
            remaining -= add;
        }

        for (int i = 0; i < SlotCount && remaining > 0; i++)
        {
            if (_slots[i] != null && !_slots[i].IsEmpty) continue;
            int add = Mathf.Min(incoming.item.maxStack, remaining);
            _slots[i] = new ItemStack(incoming.item, add);
            remaining -= add;
        }

        if (remaining != incoming.amount) OnChanged?.Invoke();
        return remaining;
    }
}