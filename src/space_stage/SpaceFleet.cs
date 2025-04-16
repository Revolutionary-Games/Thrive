using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   A fleet of ships (or just one ship) out in space.
/// </summary>
public partial class SpaceFleet : Node3D, IEntityWithNameLabel, IStrategicUnit
{
    private static readonly Lazy<PackedScene> LabelScene =
        new(() => GD.Load<PackedScene>("res://src/space_stage/gui/FleetNameLabel.tscn"));

#pragma warning disable CA2213
    [Export]
    private Node3D visualsParent = null!;
#pragma warning restore CA2213

    private bool nodeReferencesResolved;

    [JsonProperty]
    private List<UnitType>? ships;

    private bool movementEffectState;

    /// <summary>
    ///   Emitted when this fleet is selected by the player
    /// </summary>
    [Signal]
    public delegate void OnSelectedEventHandler(SpaceFleet fleet);

    // TODO: more interesting name generation / include AI empire names by default
    [JsonProperty]
    public string UnitName { get; private set; } = null!;

    [JsonIgnore]
    public string UnitScreenTitle => Localization.Translate("NAME_LABEL_FLEET").FormatSafe(UnitName, CombatPower);

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
    public float Speed { get; private set; } = 2.0f;

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
    public Node3D EntityNode => this;

    /// <summary>
    ///   Set to true when this fleet is moving. This controls whether engine visuals are enabled or not.
    /// </summary>
    public bool Moving
    {
        get => movementEffectState;
        set
        {
            if (movementEffectState == value)
                return;

            movementEffectState = value;
            ApplyEngineVisuals();
        }
    }

    public override void _Ready()
    {
        ResolveNodeReferences();

        if (string.IsNullOrEmpty(UnitName))
        {
            UnitName = Localization.Translate("FLEET_NAME_FROM_PLACE").FormatSafe(
                SimulationParameters.Instance.PatchMapNameGenerator.Next(null).RegionName);
        }

        visualsParent.Scale = new Vector3(Constants.SPACE_FLEET_MODEL_SCALE, Constants.SPACE_FLEET_MODEL_SCALE,
            Constants.SPACE_FLEET_MODEL_SCALE);

        ApplyEngineVisuals();
    }

    public void ResolveNodeReferences()
    {
        if (nodeReferencesResolved)
            return;

        nodeReferencesResolved = true;
    }

    public void Init(UnitType ships, bool playerFleet)
    {
        ResolveNodeReferences();

        SetShips(ships);
        IsPlayerFleet = playerFleet;
    }

    public override void _Process(double delta)
    {
        this.ProcessOrderQueue((float)delta);
    }

    public void AddShip(UnitType unit)
    {
        if (ships == null)
            throw new InvalidOperationException("Not initialized");

        ships.Add(unit);

        // TODO: see the visuals info in SetShips

        var newVisuals = unit.WorldRepresentationSpace.Instantiate<Node3D>();
        visualsParent.AddChild(newVisuals);
        newVisuals.Position = ships.Count * new Vector3(9.0f, 0, 0);
    }

    public void OnSelectedThroughLabel()
    {
        EmitSignal(SignalName.OnSelected, this);
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Sets the content of this fleet to the given ship(s)
    /// </summary>
    private void SetShips(UnitType ship)
    {
        ships = new List<UnitType> { ship };

        // TODO: proper positioning and scaling and rotation for multiple ships
        visualsParent.AddChild(ship.WorldRepresentationSpace.Instantiate());
    }

    private void ApplyEngineVisuals()
    {
        if (ships == null || ships.Count == 0)
            return;

        bool moving = Moving;

        int count = ships.Count;
        for (int i = 0; i < count; ++i)
        {
            // TODO: should maybe cache the references for performance of big fleets in the future
            var emitters = ships[i].EngineParticlesPathSpace;

            if (emitters == null)
                continue;

            var node = visualsParent.GetChild(i);

            if (node != null)
            {
                try
                {
                    foreach (var emitterPath in emitters)
                    {
                        var particles = node.GetNode<GpuParticles3D>(emitterPath);
                        particles.Emitting = moving;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("Failed to apply fleet movement particles: ", e);
                }
            }
            else
            {
                GD.PrintErr("Couldn't get ship visuals to apply engine particles");
            }
        }
    }
}
