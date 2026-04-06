using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Items/CraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Output")]
    public ItemDefinition outputItem;
    public int outputAmount = 1;

    [Header("Inputs")]
    public List<RecipeIngredient> ingredients = new();
}

[Serializable]
public class RecipeIngredient
{
    public ItemDefinition item;
    public int amount;
}