using System.Collections.Generic;
using UnityEngine;

public class ItemDropSystem : MonoBehaviour
{
    public static ItemDropSystem Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private float defaultLifetime = 300f;
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private ItemRegistry itemRegistry;

    [SerializeField] private float stackRadius = 3f;

    private List<DroppedItemStack> _droppedItems = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void DropItem(ItemStack stack, Vector2 worldPosition, float? dirX = null)
    {
        float dir = dirX ?? (Random.value > 0.5f ? 1f : -1f);
        var force = new Vector2(dir * Random.Range(1f, 3f), Random.Range(1f, 2.5f));
        SpawnDrop(stack, worldPosition, force);
    }

    public void TryStackWithNearby(DroppedItemStack source)
    {
        foreach (var dropped in _droppedItems)
        {
            if (dropped == null || dropped == source) continue;
            if (dropped.ItemStack.item != source.ItemStack.item) continue;

            float dist = Vector2.Distance(source.transform.position, dropped.transform.position);
            if (dist > stackRadius) continue;

            int canAdd = dropped.ItemStack.item.maxStack - dropped.ItemStack.amount;
            if (canAdd <= 0) continue;

            source.SetStackTarget(dropped);
            return;
        }
    }

    public void MergeInto(DroppedItemStack target, DroppedItemStack source)
    {
        if (target == null || target.ItemStack == null) return;
        if (source == null || source.ItemStack == null) return;

        int canAdd = target.ItemStack.item.maxStack - target.ItemStack.amount;
        int toAdd = Mathf.Min(canAdd, source.ItemStack.amount);
        target.AddAmount(toAdd);
        target.ResetLifetime();

        int remaining = source.ItemStack.amount - toAdd;
        if (remaining <= 0)
            RemoveDroppedItem(source);
        else
        {
            source.ItemStack = new ItemStack(source.ItemStack.item, remaining);
            source.RestoreCollision();
        }
    }

    public DroppedItemStack GetNearbyItem(Vector2 playerPosition, float pickupRadius)
    {
        foreach (var dropped in _droppedItems)
        {
            if (dropped == null) continue;
            if (!dropped.CanBePickedUp) continue;

            float dist = Vector2.Distance(playerPosition, dropped.transform.position);
            if (dist <= pickupRadius)
                return dropped;
        }
        return null;
    }

    public void RemoveDroppedItem(DroppedItemStack item)
    {
        _droppedItems.Remove(item);
        Destroy(item.gameObject);
    }


    private void SpawnDrop(ItemStack stack, Vector2 position, Vector2 force)
    {
        var go = Instantiate(droppedItemPrefab, position, Quaternion.identity);
        var dropped = go.GetComponent<DroppedItemStack>();
        dropped.Initialize(stack, defaultLifetime, force);
        _droppedItems.Add(dropped);
    }

}