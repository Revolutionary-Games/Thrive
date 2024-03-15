using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Planet that is placed in the space stage scene. For now uses just placeholder graphics
/// </summary>
[DeserializedCallbackTarget]
public partial class PlacedPlanet : Node3D, IEntityWithNameLabel
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/industrial_stage/gui/CityNameLabel.tscn"));

    private WorldResource foodResource = null!;
    private WorldResource rockResource = null!;

    [JsonProperty]
    private TechWeb? availableTechnology;

    /// <summary>
    ///   Emitted when this planet is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelectedEventHandler(PlacedPlanet planet);

    public enum ColonizationState
    {
        NotColonized,
        ColonyBuilding,
        Colonized,
    }

    // TODO: automatically take a name from one of the planet's cities (probably the player's capital would be good?)
    [JsonProperty]
    public string PlanetName { get; private set; } = null!;

    [JsonProperty]
    public int Population { get; set; }

    // TODO: implement planet colonization
    public ColonizationState ColonyStatus => ColonizationState.Colonized;

    [JsonProperty]
    public bool IsPlayerOwned { get; private set; }

    // TODO: calculate this somehow
    [JsonIgnore]
    public float TotalStorageSpace => 10000;

    [JsonIgnore]
    public Vector3 LabelOffset => new(0, 8, 0);

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

        if (string.IsNullOrEmpty(PlanetName))
        {
            var generatedPart = SimulationParameters.Instance.PatchMapNameGenerator.Next(new Random()).ContinentName;
            PlanetName = $"{generatedPart} {StringUtils.NameIndexSuffix(0)}";
        }

        foodResource = SimulationParameters.Instance.GetWorldResource("food");
        rockResource = SimulationParameters.Instance.GetWorldResource("rock");
    }

    public void Init(bool playerOwned, TechWeb cityAvailableTechnologies)
    {
        IsPlayerOwned = playerOwned;
        availableTechnology = cityAvailableTechnologies;
    }

    public override void _Process(double delta)
    {
    }

    public void ProcessSpace(float elapsed, IResourceContainer globalResourceHack)
    {
        // TODO: city specific storage, for now this is just done to have this code done quickly

        HandleProduction(elapsed, globalResourceHack);
        HandleResourceConsumption(elapsed, globalResourceHack);
        HandlePopulationGrowth(globalResourceHack);

        // TODO: building stuff on planets
        // ProcessBuildQueue();
    }

    public void ProcessResearch(float elapsed, ISocietyStructureDataAccess dataAccess)
    {
        // TODO: speed and technology level from buildings
        // TODO: scifi level should be reserved for deep space lbs or something like that
        ResearchComponent.HandleResearchProgressAdding(elapsed, this, 5, ResearchLevel.Scifi, dataAccess);

        // TODO: store research to show in the planet screen
    }

    public float CalculateFoodProduction()
    {
        // Base food growing
        // TODO: convert this to come from the buildings
        return Mathf.Log(Math.Max(Population, 1)) * 50 + 40;
    }

    public float CalculateFoodConsumption()
    {
        // TODO: scale food need based on species
        return Population * 2;
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(SignalName.OnSelected, this);
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
        globalResourceHack.Add(rockResource, (10 + Mathf.Log(Math.Max(Population, 1)) * 3) * elapsed);
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
        var housing = 10000;

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
}
