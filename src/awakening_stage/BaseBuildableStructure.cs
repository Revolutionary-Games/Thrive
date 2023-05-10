using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base class for placeable and then constructable structures
/// </summary>
public class BaseBuildableStructure : IRegistryType
{
#pragma warning disable 169,649 // Used through reflection

    // This has to be protected for the translation operations to find this field through reflection
    protected string? untranslatedName;
#pragma warning restore 169,649

    private readonly Lazy<PackedScene> worldRepresentation;
    private readonly Lazy<PackedScene> ghostRepresentation;
    private readonly Lazy<PackedScene> scaffoldingScene;
    private readonly Lazy<Texture> icon;

    [JsonConstructor]
    public BaseBuildableStructure(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
        ghostRepresentation = new Lazy<PackedScene>(LoadGhostScene);
        scaffoldingScene = new Lazy<PackedScene>(LoadScaffoldingScene);
        icon = new Lazy<Texture>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; protected set; }

    [JsonProperty]
    public string WorldRepresentationScene { get; private set; } = string.Empty;

    [JsonProperty]
    public string GhostScenePath { get; private set; } = string.Empty;

    [JsonProperty]
    public string ScaffoldingScenePath { get; private set; } = string.Empty;

    [JsonProperty]
    public string BuildingIcon { get; private set; } = string.Empty;

    [JsonProperty]
    public Vector3 WorldSize { get; private set; }

    /// <summary>
    ///   The cost to finish this structure once scaffolding is placed
    /// </summary>
    [JsonProperty]
    public Dictionary<WorldResource, int> RequiredResources { get; private set; } = new();

    /// <summary>
    ///   The cost of placing this structure after which <see cref="RequiredResources"/> need to be added to finish
    ///   the construction
    /// </summary>
    [JsonProperty]
    public Dictionary<WorldResource, int> ScaffoldingCost { get; private set; } = new();

    /// <summary>
    ///   The total resource cost of building this structure
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<WorldResource, int> TotalCost { get; private set; } =
        new Dictionary<WorldResource, int>();

    [JsonIgnore]
    public PackedScene WorldRepresentation => worldRepresentation.Value;

    [JsonIgnore]
    public PackedScene GhostScene => ghostRepresentation.Value;

    [JsonIgnore]
    public PackedScene ScaffoldingScene => scaffoldingScene.Value;

    [JsonIgnore]
    public Texture Icon => icon.Value;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public WorldResource? CanStart(IReadOnlyDictionary<WorldResource, int> availableMaterials)
    {
        return ResourceAmountHelpers.CalculateMissingResource(availableMaterials, ScaffoldingCost);
    }

    public WorldResource? CanFullyBuild(IReadOnlyDictionary<WorldResource, int> availableMaterials)
    {
        return ResourceAmountHelpers.CalculateMissingResource(availableMaterials, TotalCost);
    }

    public bool HasEnoughResourceToStart(WorldResource resource, int availableAmount)
    {
        return ResourceAmountHelpers.HasEnoughResource(resource, availableAmount, ScaffoldingCost);
    }

    public bool HasEnoughResourceToFullyBuild(WorldResource resource, int availableAmount)
    {
        return ResourceAmountHelpers.HasEnoughResource(resource, availableAmount, TotalCost);
    }

    public bool TakeResourcesToStartIfPossible(IResourceContainer resourceContainer)
    {
        return resourceContainer.TakeResourcesIfPossible(ScaffoldingCost);
    }

    public bool TakeCompletionResourcesIfPossible(IResourceContainer resourceContainer)
    {
        return resourceContainer.TakeResourcesIfPossible(RequiredResources);
    }

    public virtual void Check(string name)
    {
        using var file = new File();

        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (string.IsNullOrEmpty(WorldRepresentationScene) || !file.FileExists(WorldRepresentationScene))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing world representation scene");

        if (string.IsNullOrEmpty(GhostScenePath) || !file.FileExists(GhostScenePath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing ghost scene");

        if (string.IsNullOrEmpty(ScaffoldingScenePath) || !file.FileExists(ScaffoldingScenePath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing scaffolding scene");

        if (string.IsNullOrEmpty(BuildingIcon))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing icon");

        if (RequiredResources.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty required resources");

        if (RequiredResources.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required resource amount");

        if (ScaffoldingCost.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Empty scaffolding cost");

        if (ScaffoldingCost.Any(t => t.Value < 1))
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad required scaffolding resource amount");

        if (WorldSize.x <= 0 || WorldSize.y <= 0 || WorldSize.z <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Bad world size");
    }

    public void Resolve()
    {
        TotalCost = ScaffoldingCost.AsMerged(RequiredResources);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    private PackedScene LoadWorldScene()
    {
        return GD.Load<PackedScene>(WorldRepresentationScene);
    }

    private PackedScene LoadGhostScene()
    {
        return GD.Load<PackedScene>(GhostScenePath);
    }

    private PackedScene LoadScaffoldingScene()
    {
        return GD.Load<PackedScene>(ScaffoldingScenePath);
    }

    private Texture LoadIcon()
    {
        return GD.Load<Texture>(BuildingIcon);
    }
}
