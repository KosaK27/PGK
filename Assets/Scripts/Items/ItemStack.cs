using System;

[Serializable]
public class ItemStack
{
    public ItemDefinition item;
    public int amount;

    public ItemStack(ItemDefinition item, int amount)
    {
        this.item  = item;
        this.amount = amount;
    }

    public bool IsEmpty => item == null || amount <= 0;
}