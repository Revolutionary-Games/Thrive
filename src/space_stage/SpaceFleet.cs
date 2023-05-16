using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   A fleet (or just one) ship out in space.
/// </summary>
public class SpaceFleet : Spatial, IEntityWithNameLabel, IStrategicUnit
{
    [Export]
    public NodePath? VisualsParentPath;

    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/space_stage/gui/FleetNameLabel.tscn"));

#pragma warning disable CA2213
    private Spatial visualsParent = null!;
#pragma warning restore CA2213

    private bool nodeReferencesResolved;

    [JsonProperty]
    private List<UnitType>? ships;

    /// <summary>
    ///   Emitted when this planet is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelected();

    // TODO: more interesting name generation / include AI empire names by default
    [JsonProperty]
    public string UnitName { get; private set; } = null!;

    [JsonIgnore]
    public string UnitScreenTitle => TranslationServer.Translate("NAME_LABEL_FLEET").FormatSafe(UnitName, CombatPower);

    [JsonProperty]
    public Deque<IUnitOrder> QueuedOrders { get; private set; } = new();

    // TODO: fleet colour to show empire colour on the name labels

    [JsonIgnore]
    public IReadOnlyList<UnitType> Ships => ships ?? throw new InvalidOperationException("Not initialized");

    // TODO: implement this check properly
    [JsonIgnore]
    public bool HasConstructionShip => true;

    // TODO: implement this
    [JsonIgnore]
    public float CombatPower => 1;

    /// <summary>
    ///   Flying speed of the fleet
    /// </summary>
    [JsonProperty]
    public float Speed { get; private set; } = 1;

    [JsonProperty]
    public bool IsPlayerFleet { get; private set; }

    [JsonIgnore]
    public Vector3 LabelOffset => new(0, 5, 0);

    [JsonIgnore]
    public Type NameLabelType => typeof(FleetNameLabel);

    [JsonIgnore]
    public PackedScene NameLabelScene => LabelScene.Value;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Spatial EntityNode => this;

    public override void _Ready()
    {
        ResolveNodeReferences();

        if (string.IsNullOrEmpty(UnitName))
        {
            UnitName = TranslationServer.Translate("FLEET_NAME_FROM_PLACE").FormatSafe(
                SimulationParameters.Instance.PatchMapNameGenerator.Next(new Random()).RegionName);
        }

        visualsParent.Scale = new Vector3(Constants.SPACE_FLEET_MODEL_SCALE, Constants.SPACE_FLEET_MODEL_SCALE,
            Constants.SPACE_FLEET_MODEL_SCALE);
    }

    public void ResolveNodeReferences()
    {
        if (nodeReferencesResolved)
            return;

        visualsParent = GetNode<Spatial>(VisualsParentPath);

        nodeReferencesResolved = true;
    }

    public void Init(UnitType ships, bool playerFleet)
    {
        ResolveNodeReferences();

        SetShips(ships);
        IsPlayerFleet = playerFleet;
    }

    public override void _Process(float delta)
    {
        this.ProcessOrderQueue(delta);
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(nameof(OnSelected));
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            VisualsParentPath?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Sets the content of this fleet to the given ship(s)
    /// </summary>
    private void SetShips(UnitType ship)
    {
        ships = new List<UnitType> { ship };

        // TODO: proper positioning and scaling for multiple ships
        visualsParent.AddChild(ship.WorldRepresentation.Instance());

        // TODO: fleet model rotations
    }
}
