using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [SerializeField] private float pickupRadius  = 1.2f;
    [SerializeField] private float pickupDelay   = 3f;

    void Update()
    {
        TryPickup();
    }

    private void TryPickup()
    {
        var dropped = ItemDropSystem.Instance.GetNearbyItem(transform.position, pickupRadius);
        if (dropped == null) return;
        if (!dropped.CanBePickedUp) return;
        if (Time.time - dropped.SpawnTime < pickupDelay) return;

        var stack = dropped.ItemStack;
        if (stack == null || stack.IsEmpty) return;

        int overflow = InventorySystem.Instance.AddItem(new ItemStack(stack.item, stack.amount));

        if (overflow <= 0)
            ItemDropSystem.Instance.RemoveDroppedItem(dropped);
        else
            dropped.ItemStack = new ItemStack(stack.item, overflow);
    }
}