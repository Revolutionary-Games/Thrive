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
