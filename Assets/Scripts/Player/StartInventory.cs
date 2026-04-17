using UnityEngine;

public class PlayerStartInventory : MonoBehaviour
{
    [SerializeField] private ItemDefinition[] startItems;

    void Start()
    {
        var sm = SaveManager.Instance;
        if (sm != null && sm.SelectedCharacter != null && sm.SelectedCharacter.inventorySlots.Count > 0)
            return;

        foreach (var item in startItems)
            if (item != null)
                InventorySystem.Instance.AddItem(new ItemStack(item, 1));
    }
}