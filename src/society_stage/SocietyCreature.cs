using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A type of creature in the society stage, mostly just moves around to try to look busy to the player
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/society_stage/SocietyCreature.tscn", UsesEarlyResolve = false)]
public partial class SocietyCreature : Node3D, IEntity
{
#pragma warning disable CA2213
    private MulticellularMetaballDisplayer metaballDisplayer = null!;
#pragma warning restore CA2213

    private Vector3? movementTarget;

    /// <summary>
    ///   The species of this creature. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public LateMulticellularSpecies Species { get; private set; } = null!;

    /// <summary>
    ///   When true stops the normal citizen wander around behaviour
    /// </summary>
    public bool ExternallyControlled { get; set; }

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    [JsonIgnore]
    public Node3D EntityNode => this;

    public override void _Ready()
    {
        base._Ready();

        metaballDisplayer = GetNode<MulticellularMetaballDisplayer>("MetaballDisplayer");

        // TODO: determine if it would be better to have this have physics collisions or if overlap avoidance makes
        // more sense to do with another approach
    }

    /// <summary>
    ///   Must be called when spawned to set this up
    /// </summary>
    public void Init()
    {
        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(double delta)
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (movementTarget != null)
        {
            // Move towards the target
            // TODO: nicer looking movement (and rotation)
            var vectorToTarget = movementTarget.Value - GlobalPosition;

            var distance = vectorToTarget.Length();

            // We've reached the target once close enough
            if (distance < 0.4f)
            {
                movementTarget = null;
                return;
            }

            // Normalize with the already calculated distance
            vectorToTarget /= distance;

            // TODO: speed from species
            float speed = 10;

            vectorToTarget *= speed * (float)delta;

            GlobalPosition += vectorToTarget;
        }
    }

    public void ApplySpecies(Species species)
    {
        if (species is not LateMulticellularSpecies lateSpecies)
            throw new ArgumentException("Unsupported type of species");

        Species = lateSpecies;

        // Setup graphics
        // TODO: handle lateSpecies.Scale
        metaballDisplayer.DisplayFromList(lateSpecies.BodyLayout);
    }

    public bool HasReachedGoal()
    {
        return movementTarget == null;
    }

    public void SetNewDestination(Vector3 destination)
    {
        movementTarget = destination;
    }

    public void OnDestroyed()
    {
        AliveMarker.Alive = false;
    }
}
