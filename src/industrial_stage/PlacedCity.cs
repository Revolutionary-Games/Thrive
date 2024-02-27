using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A city that has been placed in the world
/// </summary>
[DeserializedCallbackTarget]
public partial class PlacedCity : Node3D, IEntityWithNameLabel
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/industrial_stage/gui/CityNameLabel.tscn"));

    [JsonProperty]
    private readonly List<BuildQueueItemBase> buildQueue = new();

    // TODO: switch to a unit instance representation class here
    [JsonProperty]
    private readonly List<UnitType> garrisonedUnits = new();

    private WorldResource foodResource = null!;
    private WorldResource rockResource = null!;

    [JsonProperty]
    private TechWeb? availableTechnology;

    // TODO: see the comment about why this is a hack in ProcessIndustrial
    private IResourceContainer? storedResourceHack;

    /// <summary>
    ///   Emitted when this city is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelectedEventHandler();

    // TODO: automatically take a name from one of the planet's patches
    [JsonProperty]
    public string CityName { get; } =
        SimulationParameters.Instance.PatchMapNameGenerator.Next(new Random()).ContinentName;

    [JsonProperty]
    public int Population { get; set; } = 1;

    [JsonIgnore]
    public IReadOnlyCollection<UnitType> GarrisonedUnits => garrisonedUnits;

    // TODO: implement city building
    public bool Completed => true;

    [JsonProperty]
    public bool IsPlayerCity { get; private set; }

    // TODO: calculate this somehow
    [JsonIgnore]
    public float TotalStorageSpace => 1000;

    [JsonIgnore]
    public Vector3 LabelOffset => new(0, 5, 0);

    [JsonIgnore]
    public Type NameLabelType => typeof(CityNameLabel);

    [JsonIgnore]
    public PackedScene NameLabelScene => LabelScene.Value;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Node3D EntityNode => this;

    public override void _Ready()
    {
        base._Ready();

        foodResource = SimulationParameters.Instance.GetWorldResource("food");
        rockResource = SimulationParameters.Instance.GetWorldResource("rock");
    }

    public void Init(bool playerCity, TechWeb cityAvailableTechnologies)
    {
        IsPlayerCity = playerCity;
        availableTechnology = cityAvailableTechnologies;
    }

    public void ProcessIndustrial(float elapsed, IResourceContainer globalResourceHack)
    {
        // TODO: city specific storage, for now this is just done to have this code done quickly

        HandleProduction(elapsed, globalResourceHack);
        HandleResourceConsumption(elapsed, globalResourceHack);
        HandlePopulationGrowth(globalResourceHack);
        ProcessBuildQueue();

        storedResourceHack = globalResourceHack;
    }

    public void ProcessResearch(float elapsed, ISocietyStructureDataAccess dataAccess)
    {
        // TODO: speed and technology level from buildings
        ResearchComponent.HandleResearchProgressAdding(elapsed, this, 5, ResearchLevel.SpaceAge, dataAccess);

        // TODO: store research to show in the city screen
    }

    /// <summary>
    ///   Updates the progress values for the build queue. Needs to be called more often than
    ///   <see cref="ProcessIndustrial"/> for smooth value updates
    /// </summary>
    /// <param name="delta">Delta</param>
    public void ProcessBuildQueueProgressValues(float delta)
    {
        if (buildQueue.Count < 1)
            return;

        buildQueue[0].ElapseTime(delta);
    }

    public IEnumerable<ICityConstructionProject> GetAvailableConstructionProjects()
    {
        if (availableTechnology == null)
        {
            GD.PrintErr("City doesn't have available technologies set");
            yield break;
        }

        // TODO: buildings

        // Units
        foreach (var unit in availableTechnology.GetAvailableUnits())
        {
            yield return unit;
        }
    }

    public IEnumerable<IBuildQueueProgressItem> GetBuildQueue()
    {
        return buildQueue;
    }

    /// <summary>
    ///   Checks if necessary resources etc. are fulfilled for a construction project
    /// </summary>
    /// <param name="constructionProject">Project to check requirements for</param>
    /// <returns>True when can build</returns>
    /// <remarks>
    ///   <para>
    ///     Note that this doesn't check the build queue length so <see cref="StartConstruction"/> can still fail if
    ///     this return true
    ///   </para>
    /// </remarks>
    public bool CanStartConstruction(ICityConstructionProject constructionProject)
    {
        if (storedResourceHack == null)
        {
            GD.PrintErr("Stored resource hack not setup yet");
            return false;
        }

        // Check required resources first before more complicated and type specific checks
        if (storedResourceHack.CalculateMissingResource(constructionProject.ConstructionCost) != null)
            return false;

        // TODO: unique buildings and other restrictions

        if (constructionProject is UnitType)
        {
            // If no space to garrison the built unit, fail
            if (!HasSpaceToGarrison(1))
                return false;
        }

        return true;
    }

    /// <summary>
    ///   Checks if the city can fit the given number of units
    /// </summary>
    /// <param name="units">How many units to check, quite often 1</param>
    /// <returns>True if can fit</returns>
    public bool HasSpaceToGarrison(int units)
    {
        int existingUnits = garrisonedUnits.Count;

        // Take build queue into account
        foreach (var buildQueueItem in buildQueue)
        {
            if (buildQueueItem is UnitBuildQueueItem)
                ++existingUnits;
        }

        return existingUnits + units < Constants.CITY_MAX_GARRISONED_UNITS;
    }

    /// <summary>
    ///   Notifies the city to remove a garrisoned unit. This doesn't perform what needs to be done to un-garrison, the
    ///   calling code needs to handle that, this just removes the unit from the garrison data.
    /// </summary>
    /// <param name="unit">The unit that left</param>
    /// <returns>True on success, false if the unit didn't exist</returns>
    public bool OnUnitUnGarrisoned(UnitType unit)
    {
        return garrisonedUnits.Remove(unit);
    }

    public bool StartConstruction(ICityConstructionProject constructionProject)
    {
        if (!CanStartConstruction(constructionProject))
            return false;

        if (buildQueue.Count >= Constants.CITY_MAX_BUILD_QUEUE_LENGTH)
        {
            GD.Print("Can't start construction due to build queue being full");
            return false;
        }

        if (!TakeStartingResources(constructionProject))
        {
            GD.PrintErr("Starting resource take failed, it shouldn't be able to fail here");
            return false;
        }

        switch (constructionProject)
        {
            case UnitType unitType:
                buildQueue.Add(new UnitBuildQueueItem(unitType, OnUnitFinished));
                break;
            default:
                throw new InvalidOperationException(
                    $"Unhandled construction project type for start: {constructionProject.GetType()}");
        }

        return true;
    }

    public float CalculateFoodProduction()
    {
        // Base food growing
        // TODO: convert this to come from the buildings
        return Mathf.Log(Population) * 10 + 4;
    }

    public float CalculateFoodConsumption()
    {
        // TODO: scale food need based on species
        return Population * 2;
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(SignalName.OnSelected);
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    private void HandleProduction(float elapsed, IResourceContainer globalResourceHack)
    {
        globalResourceHack.Add(foodResource, CalculateFoodProduction() * elapsed);

        // TODO: production from buildings

        // Placeholder some resource production
        globalResourceHack.Add(rockResource, 1 * elapsed);
    }

    private void HandleResourceConsumption(float elapsed, IResourceContainer globalResourceHack)
    {
        float neededFood = CalculateFoodConsumption() * elapsed;

        if (globalResourceHack.Take(foodResource, neededFood, true) < neededFood)
        {
            // TODO: starvation if food deficit is too much
        }
    }

    private void HandlePopulationGrowth(IResourceContainer globalResourceHack)
    {
        // TODO: housing calculation
        var housing = 1000;

        // TODO: adjust food need and reproduction rate based on species properties
        float requiredForNewMember = 1;

        // TODO: calculate how much food cities need to grow as a multiplier based on the species, city size and other
        // properties
        requiredForNewMember *= 10;

        // Don't grow if not enough housing
        if (Population >= housing)
            return;

        if (globalResourceHack.Take(foodResource, requiredForNewMember) > 0)
        {
            // Took some food to grow
            ++Population;
        }
    }

    private bool TakeStartingResources(ICityConstructionProject constructionProject)
    {
        if (storedResourceHack == null)
        {
            GD.PrintErr("Stored resource hack not setup yet");
            return false;
        }

        return storedResourceHack.TakeResourcesIfPossible(constructionProject.ConstructionCost);
    }

    /// <summary>
    ///   Removes finished items. <see cref="ProcessBuildQueueProgressValues"/> updates the
    ///   item progress values.
    /// </summary>
    private void ProcessBuildQueue()
    {
        if (buildQueue.Count < 1)
            return;

        if (buildQueue[0].CheckAndProcessFinishedStatus())
        {
            buildQueue.RemoveAt(0);
        }
    }

    [DeserializedCallbackAllowed]
    private void OnUnitFinished(UnitType type)
    {
        // TODO: proper unit type instances, for now just directly the object is put in the garrison
        garrisonedUnits.Add(type);
    }
}
