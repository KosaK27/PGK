using System.Collections.Generic;
using UnityEngine;

public enum StationType { Workbench, Furnace }

[CreateAssetMenu(fileName = "CraftingStationDefinition", menuName = "World/CraftingStationDefinition")]
public class CraftingStationDefinition : MultitileObjectDefinition
{
    [Header("Crafting Station")]
    public StationType stationType;
    public List<CraftingRecipe> recipes = new();
}