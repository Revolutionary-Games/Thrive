using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class Technology : IRegistryType
{
    private readonly Lazy<Texture> lockedIcon;
    private readonly Lazy<Texture> unlockedIcon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonProperty]
    private HashSet<string> requiresTechnologies = new();

    [JsonConstructor]
    public Technology(string name)
    {
        Name = name;

        lockedIcon = new Lazy<Texture>(LoadLockedIcon);
        unlockedIcon = new Lazy<Texture>(LoadUnlockedIcon);
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

    [JsonIgnore]
    public IReadOnlyList<Technology> RequiresTechnologies { get; private set; } = new List<Technology>();

    [JsonIgnore]
    public Texture LoadedLockedIcon => lockedIcon.Value;

    [JsonIgnore]
    public Texture LoadedUnlockedIcon => unlockedIcon.Value;

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
    private Texture LoadLockedIcon()
    {
        return GD.Load<Texture>(LockedIcon);
    }

    private Texture LoadUnlockedIcon()
    {
        return GD.Load<Texture>(UnlockedIcon);
    }
}
