using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropTable", menuName = "Items/DropTable")]
public class DropTable : ScriptableObject
{
    [Serializable]
    public class DropEntry
    {
        public ItemDefinition item;
        [Range(0f, 1000f)]
        public float chance = 100f;
    }

    public List<DropEntry> drops = new();

    public List<ItemStack> Roll()
    {
        var results = new List<ItemStack>();

        foreach (var entry in drops)
        {
            if (entry.item == null) continue;

            float remaining = entry.chance;
            int count = 0;

            while (remaining > 0f)
            {
                if (UnityEngine.Random.Range(0f, 100f) < Mathf.Min(remaining, 100f))
                    count++;
                remaining -= 100f;
            }

            if (count > 0)
                results.Add(new ItemStack(entry.item, count));
        }

        return results;
    }
}