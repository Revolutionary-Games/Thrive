﻿using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A city that has been placed in the world
/// </summary>
public class PlacedCity : Spatial, IEntityWithNameLabel
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/industrial_stage/gui/CityNameLabel.tscn"));

    private WorldResource foodResource = null!;

    /// <summary>
    ///   Emitted when this city is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelected();

    // TODO: automatically take a name from one of the planet's patches
    [JsonProperty]
    public string CityName { get; } =
        SimulationParameters.Instance.PatchMapNameGenerator.Next(new Random()).ContinentName;

    [JsonProperty]
    public int Population { get; set; } = 1;

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
    public Spatial EntityNode => this;

    public override void _Ready()
    {
        base._Ready();

        foodResource = SimulationParameters.Instance.GetWorldResource("food");
    }

    public void Init(bool playerCity)
    {
        IsPlayerCity = playerCity;
    }

    public void ProcessIndustrial(float elapsed, IResourceContainer globalResourceHack)
    {
        // TODO: city specific storage, for now this is just done to have this code done quickly

        HandleProduction(elapsed, globalResourceHack);
        HandleResourceConsumption(elapsed, globalResourceHack);
        HandlePopulationGrowth(globalResourceHack);
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(nameof(OnSelected));
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    private void HandleProduction(float elapsed, IResourceContainer globalResourceHack)
    {
        // Base food growing
        // TODO: also change the math for this
        globalResourceHack.Add(foodResource, (Mathf.Log(Population) * 10 + 4) * elapsed);

        // TODO: production from buildings
    }

    private void HandleResourceConsumption(float elapsed, IResourceContainer globalResourceHack)
    {
        // TODO: scale food need based on species
        float neededFood = Population * 2 * elapsed;

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
}
