using UnityEngine;

public class PlayerStartInventory : MonoBehaviour
{
    [SerializeField] private ItemDefinition[] startItems;

    void Start()
    {
        foreach (var item in startItems)
            if (item != null)
                InventorySystem.Instance.AddItem(new ItemStack(item, 1));
    }
}