using System;
using UnityEngine;

public class AccessoryContainer : IContainer
{
    public int SlotCount => AccessorySystem.Instance.SlotCount;
    public event Action OnChanged;

    private readonly ItemRegistry _registry;

    public AccessoryContainer(ItemRegistry registry)
    {
        _registry = registry;
        AccessorySystem.Instance.OnAccessoriesChanged += () => OnChanged?.Invoke();
    }

    public ItemStack GetSlot(int index)
    {
        var acc = AccessorySystem.Instance.GetSlot(index);
        if (acc == null) return null;
        var itemDef = _registry.GetItemByAccessoryId(acc.accessoryId);
        if (itemDef == null) return null;
        return new ItemStack(itemDef, 1);
    }

    public void SetSlot(int index, ItemStack stack)
    {
        if (stack == null || stack.IsEmpty)
        {
            AccessorySystem.Instance.Equip(index, null);
            return;
        }
        if (!stack.item.isAccessory || stack.item.accessoryDefinition == null) return;
        AccessorySystem.Instance.Equip(index, stack.item.accessoryDefinition);
    }

    public int AddItem(ItemStack incoming)
    {
        if (incoming == null || incoming.IsEmpty) return 0;
        if (!incoming.item.isAccessory || incoming.item.accessoryDefinition == null) return incoming.amount;
        for (int i = 0; i < SlotCount; i++)
        {
            if (AccessorySystem.Instance.GetSlot(i) == null)
            {
                AccessorySystem.Instance.Equip(i, incoming.item.accessoryDefinition);
                return 0;
            }
        }
        return incoming.amount;
    }
}