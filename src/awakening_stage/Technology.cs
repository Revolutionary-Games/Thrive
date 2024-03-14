using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class Technology : IRegistryType
{
    private readonly Lazy<Texture2D> lockedIcon;
    private readonly Lazy<Texture2D> unlockedIcon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonProperty]
    private HashSet<string> requiresTechnologies = new();

    [JsonConstructor]
    public Technology(string name)
    {
        Name = name;

        lockedIcon = new Lazy<Texture2D>(LoadLockedIcon);
        unlockedIcon = new Lazy<Texture2D>(LoadUnlockedIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public string LockedIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public string UnlockedIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public IReadOnlyList<CraftingRecipe> GrantsRecipes { get; private set; } = new List<CraftingRecipe>();

    [JsonProperty]
    public IReadOnlyList<StructureDefinition> GrantsStructures { get; private set; } = new List<StructureDefinition>();

    /// <summary>
    ///   Units are industrial and later "proper" units
    /// </summary>
    [JsonProperty]
    public IReadOnlyList<UnitType> GrantsUnits { get; private set; } = new List<UnitType>();

    [JsonProperty]
    public IReadOnlyList<SpaceStructureDefinition> GrantsSpaceStructures { get; private set; } =
        new List<SpaceStructureDefinition>();

    [JsonIgnore]
    public IReadOnlyList<Technology> RequiresTechnologies { get; private set; } = new List<Technology>();

    [JsonProperty]
    public ResearchLevel RequiresResearchLevel { get; private set; } = ResearchLevel.PreSociety;

    /// <summary>
    ///   How many research points it takes to research this technology
    /// </summary>
    [JsonProperty]
    public float ResearchPoints { get; private set; } = 1;

    [JsonIgnore]
    public Texture2D LoadedLockedIcon => lockedIcon.Value;

    [JsonIgnore]
    public Texture2D LoadedUnlockedIcon => unlockedIcon.Value;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(LockedIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing locked icon");

        if (string.IsNullOrEmpty(UnlockedIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing unlocked icon");
    }

    public void Resolve(SimulationParameters parameters)
    {
        RequiresTechnologies = requiresTechnologies.Select(parameters.GetTechnology).ToList();
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return "Technology " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
    private Texture2D LoadLockedIcon()
    {
        return GD.Load<Texture2D>(LockedIcon);
    }

    private Texture2D LoadUnlockedIcon()
    {
        return GD.Load<Texture2D>(UnlockedIcon);
    }
}
