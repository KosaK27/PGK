using System.Text;
using UnityEngine;

public static class ItemTooltip
{
    public static string BuildTitle(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return string.Empty;
        return stack.item.displayName;
    }

    public static string BuildAmount(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return string.Empty;
        if (stack.amount <= 1) return string.Empty;
        return $"x{stack.amount}";
    }

    public static string BuildType(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return string.Empty;
        var item = stack.item;

        if (item.isWeapon)
            return item.weaponType == WeaponType.Bow ? "Bow" : "Sword";
        if (item.isTool)
            return item.toolType switch
            {
                ToolType.Pickaxe => "Pickaxe",
                ToolType.Axe => "Axe",
                ToolType.Shovel => "Shovel",
                _ => "Tool"
            };
        if (item.isBlock) return "Block";
        if (item.isWall) return "Wall";
        if (item.isConsumable) return "Consumable";
        if (item.isAccessory) return "Accessory";
        if (item.isArmor) return "Armor";
        if (item.isMultitileObject) return "Object";
        return "Item";
    }

    public static string BuildStats(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return string.Empty;
        var item = stack.item;
        var sb = new StringBuilder();

        if (item.isWeapon)
        {
            sb.AppendLine($"Damage: {item.damage}");
            if (item.weaponType == WeaponType.Bow)
                sb.AppendLine($"Fire rate: {(1f / item.shootCooldown):F1}/s");
        }

        if (item.isTool)
            sb.AppendLine($"Breaking power: {item.breakingSpeed:F1}");

        if (item.isConsumable)
        {
            switch (item.consumableType)
            {
                case ConsumableType.HealPotion:
                    sb.AppendLine($"Heals: {item.healAmount} HP");
                    break;
                case ConsumableType.HeartContainer:
                    sb.AppendLine($"+{item.heartContainerAmount} max HP");
                    break;
                case ConsumableType.SummonBoss:
                    sb.AppendLine("Summons a boss");
                    break;
            }
        }

        if (item.isArmor && item.armorDefinition != null)
        {
            sb.AppendLine($"Defense: {item.armorDefinition.defense}");
            sb.AppendLine($"Slot: {item.armorDefinition.Slot}");
        }

        if (item.isAccessory && item.accessoryDefinition != null)
        {
            var effect = item.accessoryDefinition.effect switch
            {
                AccessoryEffect.LeatherBoots => "Effect: Autostep",
                AccessoryEffect.LightningBoots => "Effect: Dash + Autostep",
                AccessoryEffect.BatWings => "Effect: Double jump",
                _ => string.Empty
            };
            if (!string.IsNullOrEmpty(effect))
                sb.AppendLine(effect);
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildDescription(ItemStack stack)
    {
        if (stack == null || stack.IsEmpty) return string.Empty;
        return stack.item.description ?? string.Empty;
    }
}