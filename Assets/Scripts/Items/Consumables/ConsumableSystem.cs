using UnityEngine;

public class ConsumableSystem : MonoBehaviour
{
    public static ConsumableSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TryUseSelected()
    {
        var stack = InventorySystem.Instance.SelectedItem;
        if (stack == null || stack.IsEmpty) return;
        var item = stack.item;
        if (!item.isConsumable) return;

        bool used = false;

        switch (item.consumableType)
        {
            case ConsumableType.HealPotion:
                used = UseHealPotion(item);
                break;
            case ConsumableType.HeartContainer:
                used = UseHeartContainer(item);
                break;
            case ConsumableType.SummonBoss:
                used = UseSummonBoss(item);
                break;
        }

        if (used && item.consumeOnUse)
            InventorySystem.Instance.ConsumeSelected(1);
    }

    private bool UseHealPotion(ItemDefinition item)
    {
        var stats = GetPlayerStats();
        if (stats == null) return false;
        if (stats.currentHP >= stats.maxHP) return false;
        stats.Heal(item.healAmount);
        return true;
    }

    private bool UseHeartContainer(ItemDefinition item)
    {
        var stats = GetPlayerStats();
        if (stats == null) return false;
        stats.AddMaxHP(item.heartContainerAmount);
        return true;
    }

    private bool UseSummonBoss(ItemDefinition item)
    {
        if (item.bossPrefabToSummon == null) return false;
        BossSpawner.Instance.SpawnBossFromItem(item.bossPrefabToSummon);
        return true;
    }

    private PlayerStats GetPlayerStats()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.GetComponent<PlayerStats>() : null;
    }
}