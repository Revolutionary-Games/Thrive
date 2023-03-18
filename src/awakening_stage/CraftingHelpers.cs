using System.Collections.Generic;
using Godot;

/// <summary>
///   Utilities for player item crafting
/// </summary>
public static class CraftingHelpers
{
    public static List<IInventoryItem> CreateCraftingResult(CraftingRecipe recipe)
    {
        var result = new List<IInventoryItem>();

        foreach (var producedTuple in recipe.ProducesEquipment)
        {
            for (int i = 0; i < producedTuple.Value; ++i)
            {
                result.Add(SpawnHelpers.CreateEquipmentEntity(producedTuple.Key));
            }
        }

        if (result.Count < 1)
        {
            GD.PrintErr($"Recipe {recipe.InternalName} didn't produce any items");
        }

        return result;
    }
}
