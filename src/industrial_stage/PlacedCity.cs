using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A city that has been placed in the world
/// </summary>
public class PlacedCity : Spatial, IEntityWithNameLabel
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/industrial_stage/gui/CityNameLabel.tscn"));

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

    public void Init()
    {
        // TODO: make this class actually do something
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(nameof(OnSelected));
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
