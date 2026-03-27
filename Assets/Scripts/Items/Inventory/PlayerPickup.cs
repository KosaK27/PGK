using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 1.2f;

    void Update()
    {
        var dropped = ItemDropSystem.Instance.GetNearbyItem(transform.position, pickupRadius);
        if (dropped == null) return;

        var stack = dropped.ItemStack;
        if (stack == null || stack.IsEmpty) return;

        int overflow = InventorySystem.Instance.AddItem(new ItemStack(stack.item, stack.amount));

        if (overflow <= 0)
            ItemDropSystem.Instance.RemoveDroppedItem(dropped);
        else
            dropped.ItemStack = new ItemStack(stack.item, overflow);
    }
}