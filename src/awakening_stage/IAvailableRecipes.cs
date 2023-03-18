using System.Collections.Generic;

public interface IAvailableRecipes
{
    public IEnumerable<CraftingRecipe> GetAvailableRecipes(
        IReadOnlyCollection<(WorldResource Resource, int Count)>? filter);
}
