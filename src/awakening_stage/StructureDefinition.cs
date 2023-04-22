using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;

[TypeConverter(typeof(StructureStringConverter))]
public class StructureDefinition : IRegistryType
{
    private readonly Lazy<PackedScene> worldRepresentation;
    private readonly Lazy<PackedScene> ghostRepresentation;
    private readonly Lazy<PackedScene> scaffoldingScene;
    private readonly Lazy<Texture> icon;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    [JsonConstructor]
    public StructureDefinition(string name)
    {
        Name = name;

        worldRepresentation = new Lazy<PackedScene>(LoadWorldScene);
        ghostRepresentation = new Lazy<PackedScene>(LoadGhostScene);
        scaffoldingScene = new Lazy<PackedScene>(LoadScaffoldingScene);
        icon = new Lazy<Texture>(LoadIcon);
    }

    [JsonProperty]
    [TranslateFrom(nameof(untranslatedName))]
    public string Name { get; private set; }

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

    [JsonProperty]
    public Vector3 InteractOffset { get; private set; }

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
    ///   The component factories which placed structures of this type should use
    /// </summary>
    [JsonProperty]
    public StructureComponentFactoryInfo Components { get; private set; } = new();

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

    public void Check(string name)
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

        Components.Check(name);
    }

    public void Resolve()
    {
        TotalCost = ScaffoldingCost.AsMerged(RequiredResources);
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    /// <summary>
    ///   Checks if this structure contains a component of a given type
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     As the definition has the factories only this needs to be given the factory type that will create the
    ///     actual component, for example <see cref="HousingComponentFactory"/>
    ///   </para>
    /// </remarks>
    /// <typeparam name="T">The type of component factory to check for</typeparam>
    /// <returns>True if this has the specified component</returns>
    public bool HasComponentFactory<T>()
        where T : IStructureComponentFactory
    {
        foreach (var component in Components.Factories)
        {
            if (component is T)
                return true;
        }

        return false;
    }

    public override string ToString()
    {
        return "Structure type " + Name;
    }

    // TODO: a proper resource manager where these can be unloaded when
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

    public class StructureComponentFactoryInfo
    {
        private readonly List<IStructureComponentFactory> allFactories = new();

#pragma warning disable CS0649 // set from JSON
        [JsonProperty]
        private SocietyCenterComponentFactory? societyCenter;

        [JsonProperty]
        private WoodGathererFactory? woodGatherer;

        [JsonProperty]
        private RockGathererFactory? rockGatherer;

        [JsonProperty]
        private FoodGathererFactory? foodGatherer;

        [JsonProperty]
        private HousingComponentFactory? housing;

        [JsonProperty]
        private StructureStorageComponentFactory? storage;

        [JsonProperty]
        private ResearchComponentFactory? research;

        [JsonProperty]
        private FactoryComponentFactory? factory;
#pragma warning restore CS0649

        [JsonIgnore]
        public IReadOnlyList<IStructureComponentFactory> Factories => allFactories;

        /// <summary>
        ///   Checks and initializes the factory data
        /// </summary>
        public void Check(string name)
        {
            if (societyCenter != null)
                allFactories.Add(societyCenter);

            if (woodGatherer != null)
                allFactories.Add(woodGatherer);

            if (rockGatherer != null)
                allFactories.Add(rockGatherer);

            if (foodGatherer != null)
                allFactories.Add(foodGatherer);

            if (housing != null)
                allFactories.Add(housing);

            if (storage != null)
                allFactories.Add(storage);

            if (research != null)
                allFactories.Add(research);

            if (factory != null)
                allFactories.Add(factory);

            foreach (var componentFactory in allFactories)
            {
                componentFactory.Check(name);
            }
        }
    }
}
