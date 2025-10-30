using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

public class Technology : IRegistryType
{
    private readonly Lazy<Texture2D> lockedIcon;
    private readonly Lazy<Texture2D> unlockedIcon;

    private readonly List<CraftingRecipe> grantsRecipes = new();
    private readonly List<StructureDefinition> grantsStructures = new();
    private readonly List<UnitType> grantsUnits = new();
    private readonly List<SpaceStructureDefinition> grantsSpaceStructures = new();
    private readonly List<Technology> requiresTechnologies = new();

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;

    [JsonProperty(nameof(GrantsRecipes))]
    private HashSet<string>? grantsRecipesRaw;

    [JsonProperty(nameof(GrantsStructures))]
    private HashSet<string>? grantsStructuresRaw;

    [JsonProperty(nameof(GrantsUnits))]
    private HashSet<string>? grantsUnitsRaw;

    [JsonProperty(nameof(GrantsSpaceStructures))]
    private HashSet<string>? grantsSpaceStructuresRaw;

    [JsonProperty("RequiresEarlierTechnology")]
    private HashSet<string> requiresTechnologiesRaw = new();
#pragma warning restore 169,649

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

    [JsonIgnore]
    public IReadOnlyList<CraftingRecipe> GrantsRecipes => grantsRecipes;

    [JsonIgnore]
    public IReadOnlyList<StructureDefinition> GrantsStructures => grantsStructures;

    /// <summary>
    ///   Units are industrial and later "proper" units
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<UnitType> GrantsUnits => grantsUnits;

    [JsonIgnore]
    public IReadOnlyList<SpaceStructureDefinition> GrantsSpaceStructures => grantsSpaceStructures;

    [JsonIgnore]
    public IReadOnlyList<Technology> RequiresTechnologies => requiresTechnologies;

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
        if (grantsRecipesRaw != null)
        {
            foreach (var entry in grantsRecipesRaw)
            {
                grantsRecipes.Add(parameters.GetCraftingRecipe(entry));
            }
        }

        if (grantsStructuresRaw != null)
        {
            foreach (var entry in grantsStructuresRaw)
            {
                grantsStructures.Add(parameters.GetStructure(entry));
            }
        }

        if (grantsUnitsRaw != null)
        {
            foreach (var entry in grantsUnitsRaw)
            {
                grantsUnits.Add(parameters.GetUnitType(entry));
            }
        }

        if (grantsSpaceStructuresRaw != null)
        {
            foreach (var entry in grantsSpaceStructuresRaw)
            {
                grantsSpaceStructures.Add(parameters.GetSpaceStructure(entry));
            }
        }

        foreach (var entry in requiresTechnologiesRaw)
        {
            requiresTechnologies.Add(parameters.GetTechnology(entry));
        }
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
