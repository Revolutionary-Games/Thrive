using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Main script on each cell in the game
/// </summary>
public class Microbe : RigidBody, ISpawned, IProcessable, IMicrobeAI
{
    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    public readonly CompoundBag Compounds = new CompoundBag(0.0f);

    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    private CompoundCloudSystem cloudSystem;
    private Membrane membrane;

    /// <summary>
    ///   The species of this microbe
    /// </summary>
    public MicrobeSpecies Species { get; private set; }

    public int HexCount
    {
        get
        {
            // TODO: add computation and caching for this
            return 1;
        }
    }

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

    // TODO: implement process list
    public List<TweakedProcess> ActiveProcesses { get; private set; } =
        new List<TweakedProcess>();

    public CompoundBag ProcessCompoundStorage
    {
        get { return Compounds; }
    }

    public float TimeUntilNextAIUpdate { get; set; } = 0;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds
    /// </summary>
    public Dictionary<string, float> TotalAbsorbedCompounds { get; set; } =
        new Dictionary<string, float>();

    public float AgentEmissionCooldown { get; private set; } = 0.0f;

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(CompoundCloudSystem cloudSystem)
    {
        this.cloudSystem = cloudSystem;
    }

    public override void _Ready()
    {
        if (cloudSystem == null)
        {
            throw new Exception("Microbe not initialized");
        }

        membrane = GetNode<Membrane>("Membrane");

        // TODO: reimplement capacity calculation
        Compounds.Capacity = 50.0f;
    }

    /// <summary>
    ///   Applies the species for this cell. Called when spawned
    /// </summary>
    public void ApplySpecies(Species species)
    {
        Species = (MicrobeSpecies)species;

        float scale = 1.0f;

        // Bacteria are 50% the size of other cells
        if (Species.IsBacteria)
            scale = 0.5f;

        Scale = new Vector3(scale, scale, scale);

        ResetOrganelleLayout();
        SetInitialCompounds();
    }

    public void ResetOrganelleLayout()
    {
        // Send organelles to membrane
        var organellePositions = new List<Vector2>();
        organellePositions.Add(new Vector2(0, 0));

        // TODO: finish
        membrane.OrganellePositions = organellePositions;
        membrane.Dirty = true;
    }

    /// <summary>
    ///   Tries to fire a toxin if possible
    /// </summary>
    public void EmitToxin()
    {
        if (AgentEmissionCooldown > 0)
            return;

        // TODO: port over the proper logic

        AgentEmissionCooldown = 1.0f;

        var props = new AgentProperties();
        props.Compound = SimulationParameters.Instance.GetCompound("oxytoxy");
        props.Species = Species;

        // Find the direction the microbe is facing
        var vec = LookAtPoint - Translation;
        var direction = vec.Normalized();

        SpawnHelpers.SpawnAgent(props, 10.0f, 5.0f, Translation, direction, GetParent(),
            SpawnHelpers.LoadAgentScene());
    }

    /// <summary>
    ///   Resets the compounds to be the ones this species spawns with
    /// </summary>
    public void SetInitialCompounds()
    {
        Compounds.ClearCompounds();

        foreach (var entry in Species.InitialCompounds)
        {
            Compounds.AddCompound(entry.Key, entry.Value);
        }
    }

    public override void _Process(float delta)
    {
        if (MovementDirection != new Vector3(0, 0, 0))
        {
            // Movement direction should not be normalized to allow different speeds
            Vector3 totalMovement = new Vector3(0, 0, 0);

            totalMovement += DoBaseMovementForce(delta);

            ApplyMovementImpulse(totalMovement, delta);
        }

        // ApplyRotation();

        // TODO: make this take elapsed time into account
        HandleCompoundAbsorbing();

        HandleCompoundVenting(delta);

        // Reduce agent emission cooldown
        AgentEmissionCooldown -= delta;
        if (AgentEmissionCooldown < 0)
            AgentEmissionCooldown = 0;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
    }

    private void HandleCompoundAbsorbing()
    {
        float scale = 1.0f;

        if (Species.IsBacteria)
            scale = 0.5f;

        // This grab radius version is used for world coordinate calculations
        // TODO: switch back to using the radius from membrane
        float grabRadius = 3.0f;

        // // max here buffs compound absorbing for the smallest cells
        // const auto grabRadius =
        //     std::max(membrane.calculateEncompassingCircleRadius(), 3.0f);

        cloudSystem.AbsorbCompounds(Translation, grabRadius * scale, Compounds,
            TotalAbsorbedCompounds);
    }

    /// <summary>
    ///   Vents (throws out) non-useful compounds from this cell
    /// </summary>
    private void HandleCompoundVenting(float delta)
    {
        // Skip if process system has not run yet
        if (!Compounds.HasAnyBeenSetUseful())
            return;

        // float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND;
    }

    private Vector3 DoBaseMovementForce(float delta)
    {
        var cost = (Constants.BASE_MOVEMENT_ATP_COST * HexCount) * delta;

        var got = Compounds.TakeCompound("atp", cost);

        float force = Constants.CELL_BASE_THRUST;

        // Halve speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;
        }

        return Transform.basis.Xform(MovementDirection * force);

        // * microbeComponent.movementFactor *
        // (SimulationParameters::membraneRegistry().getTypeData(
        // microbeComponent.species.membraneType).movementFactor -
        //     microbeComponent.species.membraneRigidity *
        // MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
    }

    private void ApplyMovementImpulse(Vector3 movement, float delta)
    {
        if (movement.x == 0.0f && movement.z == 0.0f)
            return;

        ApplyCentralImpulse(movement * delta);
    }

    /// <summary>
    ///   Just slerps towards a fixed amount the target point
    /// </summary
    private Transform GetNewPhysicsRotation(Transform transform)
    {
        var target = transform.LookingAt(LookAtPoint, new Vector3(0, 1, 0));

        // Need to manually normalize everything, otherwise the slerp fails
        Quat slerped = transform.basis.Quat().Normalized().Slerp(
            target.basis.Quat().Normalized(), 0.2f);

        return new Transform(new Basis(slerped), transform.origin);
    }
}
