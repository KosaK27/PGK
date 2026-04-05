using System;

public interface IContainer
{
    int SlotCount { get; }
    ItemStack GetSlot(int index);
    void SetSlot(int index, ItemStack stack);
    int AddItem(ItemStack incoming);
    event Action OnChanged;
}