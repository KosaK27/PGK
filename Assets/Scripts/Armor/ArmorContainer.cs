using System;

public class ArmorContainer : IContainer
{
    public int SlotCount => 3;
    public event Action OnChanged;
    private readonly ItemRegistry _registry;
    public ArmorContainer(ItemRegistry registry)
    {
        _registry = registry;
        ArmorSystem.Instance.OnArmorChanged += () => OnChanged?.Invoke();
    }
    public ItemStack GetSlot(int index)
    {
        var armor = ArmorSystem.Instance.GetSlot((ArmorSlot)index);
        if (armor == null) return null;
        var itemDef = _registry.GetItemByArmorId(armor.armorId);
        if (itemDef == null) return null;
        return new ItemStack(itemDef, 1);
    }
    public void SetSlot(int index, ItemStack stack)
    {
        var slot = (ArmorSlot)index;
        if (stack == null || stack.IsEmpty) { ArmorSystem.Instance.Equip(slot, null); return; }
        if (!stack.item.isArmor || stack.item.armorDefinition == null) return;
        if (stack.item.armorDefinition.Slot != slot) return;
        ArmorSystem.Instance.Equip(slot, stack.item.armorDefinition);
    }
    public int AddItem(ItemStack incoming)
    {
        if (incoming == null || incoming.IsEmpty) return 0;
        if (!incoming.item.isArmor || incoming.item.armorDefinition == null) return incoming.amount;
        var slot = incoming.item.armorDefinition.Slot;
        if (ArmorSystem.Instance.GetSlot(slot) == null)
        {
            ArmorSystem.Instance.Equip(slot, incoming.item.armorDefinition);
            return 0;
        }
        return incoming.amount;
    }
}