using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   A recipe for crafting some things from raw resources
/// </summary>
[TypeConverter(typeof(CraftingRecipeStringConverter))]
public class CraftingRecipe : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public CraftingRecipe(string name)
    {
        Name = name;
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public Dictionary<WorldResource, int> RequiredResources { get; private set; } = new();

    [JsonProperty]
    public Dictionary<EquipmentDefinition, int> ProducesEquipment { get; private set; } = new();

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public bool MatchesFilter(IReadOnlyCollection<(WorldResource Resource, int Count)> filter)
    {
        foreach (var (filterResource, filterCount) in filter)
        {
            if (!RequiredResources.TryGetValue(filterResource, out var amount))
                return false;

            // Filter doesn't match if filter wants 2 of some resource but this only takes one
            if (amount < filterCount)
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Checks if can craft this recipe
    /// </summary>
    /// <param name="availableMaterials">The materials that are available</param>
    /// <returns>Null if can craft, otherwise the material type that is missing</returns>
    public WorldResource? CanCraft(IReadOnlyDictionary<WorldResource, int> availableMaterials)
    {
        return ResourceAmountHelpers.CalculateMissingResource(availableMaterials, RequiredResources);
    }

    public bool HasEnoughResource(WorldResource resource, int availableAmount)
    {
        return ResourceAmountHelpers.HasEnoughResource(resource, availableAmount, RequiredResources);
    }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (RequiredResources.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (RequiredResources.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

        // TODO: allow producing something else than equipment
        if (ProducesEquipment.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty recipe outputs");

        if (ProducesEquipment.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad produced equipment amount");
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Recipe " + Name;
    }
}
