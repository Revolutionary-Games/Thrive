using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   A recipe for crafting some things from raw resources
/// </summary>
public class CraftingRecipe : IRegistryType
{
    private readonly Dictionary<WorldResource, int> requiredResources = new();
    private readonly Dictionary<EquipmentDefinition, int> producesEquipment = new();

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;

    [JsonProperty(nameof(RequiredResources))]
    private Dictionary<string, int>? requiredResourcesRaw;

    [JsonProperty(nameof(ProducesEquipment))]
    private Dictionary<string, int>? producesEquipmentRaw;
#pragma warning restore 169,649

    [JsonConstructor]
    public CraftingRecipe(string name)
    {
        Name = name;
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonIgnore]
    public Dictionary<WorldResource, int> RequiredResources => requiredResources;

    [JsonIgnore]
    public Dictionary<EquipmentDefinition, int> ProducesEquipment => producesEquipment;

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
    ///   Checks if the player can craft this recipe
    /// </summary>
    /// <param name="availableMaterials">The materials that are available</param>
    /// <returns>Null if can be crafted, otherwise the material type that is missing</returns>
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

        if (requiredResourcesRaw == null || requiredResourcesRaw.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (requiredResourcesRaw.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

        // TODO: allow producing something else than equipment
        if (producesEquipmentRaw == null || producesEquipmentRaw.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty recipe outputs");

        if (producesEquipmentRaw.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad produced equipment amount");
    }

    public void Resolve(SimulationParameters simulationParameters)
    {
        // Check already checked that these exist
        foreach (var entry in requiredResourcesRaw!)
        {
            requiredResources.Add(simulationParameters.GetWorldResource(entry.Key), entry.Value);
        }

        foreach (var entry in producesEquipmentRaw!)
        {
            producesEquipment.Add(simulationParameters.GetBaseEquipmentDefinition(entry.Key), entry.Value);
        }
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
