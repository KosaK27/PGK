using System;
using UnityEngine;

public class PlayerInventoryContainer : IContainer
{
    public int SlotCount => InventorySystem.Instance.TotalSlots;
    public event Action OnChanged;

    public PlayerInventoryContainer()
    {
        InventorySystem.Instance.OnInventoryChanged += () => OnChanged?.Invoke();
    }

    public ItemStack GetSlot(int index) => InventorySystem.Instance.GetSlot(index);

    public void SetSlot(int index, ItemStack stack)
    {
        InventorySystem.Instance.SetSlot(index, stack);
    }

    public int AddItem(ItemStack incoming) => InventorySystem.Instance.AddItem(incoming);
}