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

    // Child components
    private AudioStreamPlayer3D engulfAudio;
    private AudioStreamPlayer3D otherAudio;
    private AudioStreamPlayer3D movementAudio;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    private OrganelleLayout<PlacedOrganelle> organelles;

    private bool processesDirty = true;
    private List<TweakedProcess> processes;

    private bool cachedHexCountDirty = true;
    private int cachedHexCount;

    private Vector3 queuedMovementForce;

    // variables for engulfing
    private bool engulfMode = false;
    private bool isBeingEngulfed = false;
    private Microbe hostileEngulfer = null;
    private bool wasBeingEngulfed = false;

    // private bool isCurrentlyEngulfing = false;

    private float hitpoints = Constants.DEFAULT_HEALTH;
    private float maxHitpoints = Constants.DEFAULT_HEALTH;

    private float lastCheckedATPDamage = 0.0f;

    /// <summary>
    ///   The microbe stores here the sum of capacity of all the
    ///   current organelles. This is here to prevent anyone from
    ///   messing with this value if we used the Capacity from the
    ///   CompoundBag for the calculations that use this.
    /// </summary>
    private float organellesCapacity = 0.0f;

    /// <summary>
    ///   The number of agent vacuoles. Determines the time between
    ///   toxin shots.
    /// </summary>
    private int agentVacuoleCount = 0;

    /// <summary>
    ///   Multiplied on the movement speed of the microbe.
    /// </summary>
    private float movementFactor = 1.0f;

    // private float compoundCollectionTimer = EXCESS_COMPOUND_COLLECTION_INTERVAL;

    private float escapeInterval = 0;
    private bool hasEscaped = false;

    /// <summary>
    ///   Controls for how long the flashColour is held before going
    ///   back to species colour.
    /// </summary>
    private float flashDuration = 0;
    private Color flashColour = new Color(0, 0, 0, 0);

    private bool allOrganellesDivided = false;

    /// <summary>
    ///   The membrane of this Microbe. Used for grabbing radius / points from this.
    /// </summary>
    public Membrane Membrane { get; private set; }

    /// <summary>
    ///   The species of this microbe
    /// </summary>
    public MicrobeSpecies Species { get; private set; }

    /// <summary>
    ///    True when this is the player's microbe
    /// </summary>
    public bool IsPlayerMicrobe { get; private set; }

    /// <summary>
    ///   True only when this has been deleted to let know things
    ///   being engulfed by us that we are dead.
    /// </summary>
    public bool Dead { get; private set; } = false;

    /// <summary>
    ///   If true cell is in engulf mode.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Prefer setting this instead of directly setting the private variable.
    ///   </para>
    /// </remarks>
    public bool EngulfMode
    {
        get
        {
            return engulfMode;
        }
        set
        {
            engulfMode = value;
        }
    }

    public int HexCount
    {
        get
        {
            if (cachedHexCountDirty)
                CountHexes();

            return cachedHexCount;
        }
    }

    public float Radius
    {
        get
        {
            var radius = Membrane.EncompassingCircleRadius;

            if (Species.IsBacteria)
                radius *= 0.5f;

            return radius;
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
    public List<TweakedProcess> ActiveProcesses
    {
        get
        {
            if (processesDirty)
                RefreshProcesses();
            return processes;
        }
    }

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
    public void Init(CompoundCloudSystem cloudSystem, bool isPlayer)
    {
        this.cloudSystem = cloudSystem;
        IsPlayerMicrobe = isPlayer;

        if (IsPlayerMicrobe)
            GD.Print("Player Microbe spawned");
    }

    public override void _Ready()
    {
        if (cloudSystem == null)
        {
            throw new Exception("Microbe not initialized");
        }

        Membrane = GetNode<Membrane>("Membrane");
        engulfAudio = GetNode<AudioStreamPlayer3D>("EngulfAudio");
        otherAudio = GetNode<AudioStreamPlayer3D>("OtherAudio");
        movementAudio = GetNode<AudioStreamPlayer3D>("MovementAudio");
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

        // TODO: set membrane type on the membrane

        Membrane.Tint = Species.Colour;
    }

    /// <summary>
    ///   Resets the organelles in this microbe to match the species definition
    /// </summary>
    public void ResetOrganelleLayout()
    {
        // TODO: It would be much better if only organelles that need
        // to be removed where removed, instead of everything

        if (organelles == null)
        {
            organelles = new OrganelleLayout<PlacedOrganelle>(this.OnOrganelleAdded,
                this.OnOrganelleRemoved);
        }
        else
        {
            // Just clear the existing ones
            organelles.RemoveAll();
        }

        foreach (var entry in Species.Organelles.Organelles)
        {
            var placed = new PlacedOrganelle
            {
                Definition = entry.Definition,
                Position = entry.Position,
                Orientation = entry.Orientation,
            };

            organelles.Add(placed);
        }

        SendOrganellePositionsToMembrane();

        // Reproduction progress is lost
        allOrganellesDivided = false;
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
            SpawnHelpers.LoadAgentScene(), this);
    }

    /// <summary>
    ///   Flashes the membrane a specific colour for duration. A new
    ///   flash is not started if currently flashing.
    /// </summary>
    /// <returns>True when a new flash was started, false if already flashing</returns>
    public bool Flash(float duration, Color colour)
    {
        if (flashDuration > 0)
            return false;

        flashDuration = duration;
        flashColour = colour;
        return true;
    }

    /// <summary>
    ///   Applies damage to this cell
    /// </summary>
    public void Damage(float amount, string source)
    {
        // TODO: fix
    }

    /// <summary>
    ///   Called from movement organelles to add movement force
    /// </summary>
    public void AddMovementForce(Vector3 force)
    {
        queuedMovementForce += force;
    }

    /// <summary>
    ///   Resets the compounds to be the ones this species spawns with. Called by spawn helpers
    /// </summary>
    public void SetInitialCompounds()
    {
        Compounds.ClearCompounds();

        foreach (var entry in Species.InitialCompounds)
        {
            Compounds.AddCompound(entry.Key, entry.Value);
        }
    }

    /// <summary>
    ///   Triggers reproduction on this cell (even if not ready)
    /// </summary>
    public void Divide()
    {
        // Separate the two cells.
        var separation = new Vector3(Radius, 0, 0);

        // Create the one daughter cell.
        var copyEntity = SpawnHelpers.SpawnMicrobe(Species, Translation + separation,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem);

        // Remove the compounds from the created cell
        copyEntity.Compounds.ClearCompounds();

        var keys = new List<string>(Compounds.Compounds.Keys);

        // Split the compounds evenly between the two cells.
        foreach (var compound in keys)
        {
            var amount = Compounds.GetCompoundAmount(compound);

            if (amount > 0)
            {
                Compounds.TakeCompound(compound, amount * 0.5f);

                var didntFit = copyEntity.Compounds.AddCompound(compound, amount * 0.5f);

                if (didntFit > 0)
                {
                    // TODO: handle the excess compound that didn't fit in the other cell
                }
            }
        }

        // Play the split sound
        var sound = GD.Load<AudioStream>(
            "res://assets/sounds/soundeffects/reproduction.ogg");

        otherAudio.Stream = sound;
        otherAudio.Play();
    }

    public override void _Process(float delta)
    {
        // TODO: make this take elapsed time into account
        HandleCompoundAbsorbing();

        movementFactor = 1.0f;
        queuedMovementForce = new Vector3(0, 0, 0);

        // Reduce agent emission cooldown
        AgentEmissionCooldown -= delta;
        if (AgentEmissionCooldown < 0)
            AgentEmissionCooldown = 0;

        HandleFlashing(delta);
        HandleHitpointsRegeneration(delta);
        HandleReproduction(delta);
        HandleEngulfing(delta);
        HandleOsmoregulation(delta);

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles.Organelles)
        {
            organelle.Update(delta);
        }

        ApplyCustomDrag(delta);

        // Movement
        if (MovementDirection != new Vector3(0, 0, 0) ||
            queuedMovementForce != new Vector3(0, 0, 0))
        {
            // Movement direction should not be normalized to allow different speeds
            Vector3 totalMovement = new Vector3(0, 0, 0);

            if (MovementDirection != new Vector3(0, 0, 0))
            {
                totalMovement += DoBaseMovementForce(delta);
            }

            totalMovement += queuedMovementForce;

            ApplyMovementImpulse(totalMovement, delta);
        }

        // Rotation is applied in the physics force callback as that's
        // the place where the body rotation can be directly set
        // without problems

        HandleCompoundVenting(delta);

        lastCheckedATPDamage += delta;

        while (lastCheckedATPDamage >= Constants.ATP_DAMAGE_CHECK_INTERVAL)
        {
            lastCheckedATPDamage -= Constants.ATP_DAMAGE_CHECK_INTERVAL;
            ApplyATPDamage();
        }

        Membrane.HealthFraction = hitpoints / maxHitpoints;

        if (hitpoints <= 0)
        {
            HandleDeath();
        }
        else
        {
            // TODO: fix
            // // As long as the player has been alive they can go to the editor in freebuild
            // if(IsPlayerMicrobe && GetThriveGame().playerData().isFreeBuilding())
            // {
            //     showReproductionDialog(world);
            // }
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
    }

    private void HandleCompoundAbsorbing()
    {
        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem.AbsorbCompounds(Translation, grabRadius, Compounds,
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

    /// <summary>
    ///   Flashes the membrane colour when Flash has been called
    /// </summary>
    private void HandleFlashing(float delta)
    {
        // Flash membrane if something happens.
        if (flashDuration > 0 && flashColour != new Color(0, 0, 0, 0))
        {
            flashDuration -= delta;

            // How frequent it flashes, would be nice to update
            // the flash void to have this variable{
            if ((flashDuration % 0.6f) < 0.3f)
            {
                Membrane.Tint = flashColour;
            }
            else
            {
                // Restore colour
                Membrane.Tint = Species.Colour;
            }

            // Flashing ended
            if (flashDuration <= 0)
            {
                flashDuration = 0;

                // Restore colour
                Membrane.Tint = Species.Colour;
            }
        }
    }

    /// <summary>
    ///   Regenerate hitpoints while the cell has atp
    /// </summary>
    private void HandleHitpointsRegeneration(float delta)
    {
        if (hitpoints < maxHitpoints)
        {
            if (Compounds.GetCompoundAmount("atp") >= 1.0f)
            {
                hitpoints += Constants.REGENERATION_RATE * delta;
                if (hitpoints > maxHitpoints)
                {
                    hitpoints = maxHitpoints;
                }
            }
        }
    }

    /// <summary>
    ///   Handles feeding the organelles in this microbe in order for
    ///   them to split. After all are split this is ready to
    ///   reproduce.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     AI cells will immediately reproduce when they can. On the
    ///     player cell the editor is unlocked when reproducing is
    ///     possible.
    ///   </para>
    /// </remarks>
    private void HandleReproduction(float delta)
    {
        if (allOrganellesDivided)
        {
            // Ready to reproduce already. Only the player gets here
            // as other cells split and reset automatically
            return;
        }

        bool reproductionStageComplete = true;

        // Organelles that are ready to split
        var organellesToAdd = new List<PlacedOrganelle>();

        // Grow all the organelles, except the nucleus which is given compounds last
        foreach (var organelle in organelles.Organelles)
        {
            // Check if already done
            if (organelle.WasSplit)
                continue;

            // We are in G1 phase of the cell cycle, duplicate all organelles.

            // Except the nucleus
            if (organelle.Definition.InternalName == "nucleus")
                continue;

            // If Give it some compounds to make it larger.
            organelle.GrowOrganelle(Compounds);

            if (organelle.GrowthValue >= 1.0f)
            {
                // Queue this organelle for splitting after the loop.
                organellesToAdd.Add(organelle);
            }
            else
            {
                // Needs more stuff
                reproductionStageComplete = false;
            }
        }

        // Splitting the queued organelles.
        foreach (var organelle in organellesToAdd)
        {
            // Mark this organelle as done and return to its normal size.
            organelle.ResetGrowth();
            organelle.WasSplit = true;

            // Create a second organelle.
            var organelle2 = SplitOrganelle(organelle);
            organelle2.WasSplit = true;
            organelle2.IsDuplicate = true;
            organelle2.SisterOrganelle = organelle;
        }

        if (organellesToAdd.Count > 0)
        {
            // Redo the cell membrane.
            SendOrganellePositionsToMembrane();

            // Process list is automatically marked dirty when the split organelle is added
        }

        if (reproductionStageComplete)
        {
            // All organelles have split. Now give the nucleus compounds

            foreach (var organelle in organelles.Organelles)
            {
                // Check if already done
                if (organelle.WasSplit)
                    continue;

                // In the S phase, the nucleus grows as chromatin is duplicated.
                if (organelle.Definition.InternalName != "nucleus")
                    continue;

                // The nucleus hasn't finished replicating
                // its DNA, give it some compounds.
                organelle.GrowOrganelle(Compounds);

                if (organelle.GrowthValue < 1.0f)
                {
                    // Nucleus needs more compounds
                    reproductionStageComplete = false;
                }
            }
        }

        if (reproductionStageComplete)
        {
            // Nucleus is also now ready to reproduce
            allOrganellesDivided = true;

            // For NPC cells this immediately splits them and the
            // allOrganellesDivided flag is reset
            ReadyToReproduce();
        }
    }

    private PlacedOrganelle SplitOrganelle(PlacedOrganelle organelle)
    {
        var q = organelle.Position.Q;
        var r = organelle.Position.R;

        var newOrganelle = new PlacedOrganelle();
        newOrganelle.Definition = organelle.Definition;

        // Spiral search for space for the organelle
        int radius = 1;
        while (true)
        {
            // Moves into the ring of radius "radius" and center the old organelle
            var radiusOffset = Hex.HexNeighbourOffset[Hex.HEX_SIDE.BOTTOM_LEFT];
            q = q + radiusOffset.Q;
            r = r + radiusOffset.R;

            // Iterates in the ring
            for (int side = 1; side <= 6; ++side)
            {
                var offset = Hex.HexNeighbourOffset[(Hex.HEX_SIDE)side];

                // Moves "radius" times into each direction
                for (int i = 1; i <= radius; ++i)
                {
                    q = q + offset.Q;
                    r = r + offset.R;

                    // Checks every possible rotation value.
                    for (int j = 0; j <= 5; ++j)
                    {
                        newOrganelle.Position = new Hex(q, r);

                        // TODO: in the old code this was always i *
                        // 60 so this didn't actually do what it meant
                        // to do. But perhaps that was right? This is
                        // now fixed to actually try the different
                        // rotations.
                        newOrganelle.Orientation = j;
                        if (organelles.CanPlace(newOrganelle))
                        {
                            organelles.Add(newOrganelle);
                            return newOrganelle;
                        }
                    }
                }
            }

            ++radius;
        }
    }

    /// <summary>
    ///   Copies this microbe (if this isn't the player). The new
    ///   microbe will not have the stored compounds of this one.
    /// </summary>
    private void ReadyToReproduce()
    {
        if (IsPlayerMicrobe)
        {
            // The player doesn't split automatically
            allOrganellesDivided = true;

            // TODO: fix
            // showReproductionDialog(world);
        }
        else
        {
            // Return the first cell to its normal, non duplicated cell arrangement.
            if (!Species.PlayerSpecies)
            {
                // TODO: fix
                // MicrobeOperations::alterSpeciesPopulation(species,
                //     CREATURE_REPRODUCE_POPULATION_GAIN, "reproduced");
            }

            ResetOrganelleLayout();

            Divide();
        }
    }

    /// <summary>
    ///   Handles things related to engulfing. Works together with the physics callbacks
    /// </summary>
    private void HandleEngulfing(float delta)
    {
        if (EngulfMode)
        {
            // Drain atp
            var cost = Constants.ENGULFING_ATP_COST_SECOND * delta;

            if (Compounds.TakeCompound("atp", cost) < cost - 0.001f)
            {
                EngulfMode = false;
            }
        }

        // Play sound
        if (EngulfMode)
        {
            if (!engulfAudio.Playing)
                engulfAudio.Play();

            // Flash the membrane blue.
            Flash(1, new Color(0.2f, 0.5f, 1.0f, 0.5f));
        }
        else
        {
            if (engulfAudio.Playing)
                engulfAudio.Stop();
        }

        // Movement modifier
        if (EngulfMode)
        {
            movementFactor /= Constants.ENGULFING_MOVEMENT_DIVISION;
        }

        if (isBeingEngulfed)
        {
            movementFactor /= Constants.ENGULFED_MOVEMENT_DIVISION;

            Damage(Constants.ENGULF_DAMAGE * delta, "isBeingEngulfed");
            wasBeingEngulfed = true;
        }
        else if (wasBeingEngulfed && !isBeingEngulfed)
        {
            // Else If we were but are no longer, being engulfed
            wasBeingEngulfed = false;

            if (!IsPlayerMicrobe && Species.PlayerSpecies)
            {
                hasEscaped = true;
                escapeInterval = 0;
            }

            RemoveEngulfedEffect();
        }

        // Still considered to be chased for CREATURE_ESCAPE_INTERVAL milliseconds
        if (hasEscaped)
        {
            escapeInterval += delta;
            if (escapeInterval >= Constants.CREATURE_ESCAPE_INTERVAL)
            {
                hasEscaped = false;
                escapeInterval = 0;

                // TODO: apply escape population gain
                // MicrobeOperations::alterSpeciesPopulation(species,
                //     Constants.CREATURE_ESCAPE_POPULATION_GAIN, "escape engulfing");
            }
        }

        // Check whether we should not be being engulfed anymore
        if (hostileEngulfer != null)
        {
            Vector3 predatorPosition = new Vector3(0, 0, 0);

            var ourPosition = Translation;

            float circleRad = 0.0f;

            if (!hostileEngulfer.Dead)
            {
                predatorPosition = hostileEngulfer.Translation;
                circleRad = hostileEngulfer.Radius;
            }

            if (!hostileEngulfer.EngulfMode || hostileEngulfer.Dead ||
                (ourPosition - predatorPosition).LengthSquared() >= circleRad)
            {
                hostileEngulfer = null;
                isBeingEngulfed = false;
            }
        }
        else
        {
            isBeingEngulfed = false;
        }
    }

    private void RemoveEngulfedEffect()
    {
        // TODO: fix
    }

    private void HandleOsmoregulation(float delta)
    {
        var osmoCost = (HexCount * Species.MembraneType.OsmoregulationFactor *
            Constants.ATP_COST_FOR_OSMOREGULATION) * delta;

        Compounds.TakeCompound("atp", osmoCost);
    }

    /// <summary>
    ///   Damage the microbe if its too low on ATP.
    /// </summary>
    private void ApplyATPDamage()
    {
        if (Compounds.GetCompoundAmount("atp") <= 0.0f)
        {
            // TODO: put this on a GUI notification.
            // if(microbeComponent.isPlayerMicrobe and not this.playerAlreadyShownAtpDamage){
            //     this.playerAlreadyShownAtpDamage = true
            //     showMessage("No ATP hurts you!")
            // }

            Damage(maxHitpoints * Constants.NO_ATP_DAMAGE_FRACTION, "atpDamage");
        }
    }

    /// <summary>
    ///   Handles the death of this microbe. This queues this object
    ///   for deletion and handles some pre-death actions.
    /// </summary>
    private void HandleDeath()
    {
        QueueFree();
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

        return Transform.basis.Xform(MovementDirection * force) * movementFactor *
            (Species.MembraneType.MovementFactor -
                (Species.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
    }

    /// <summary>
    ///   Applies some custom drag logic on cells to make their
    ///   movement better. TODO: determine if this is still needed.
    /// </summary>
    private void ApplyCustomDrag(float delta)
    {
        // const Float3 velocity = physics.Body.GetVelocity();

        // // There should be no Y velocity so it should be zero
        // const Float3 drag(velocity.X * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
        //             microbeComponent.totalHexCountCache)),
        //     velocity.Y * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
        //             microbeComponent.totalHexCountCache)),
        //     velocity.Z * (CELL_DRAG_MULTIPLIER + (CELL_SIZE_DRAG_MULTIPLIER *
        //             microbeComponent.totalHexCountCache)));

        // // Only add drag if it is over CELL_REQUIRED_DRAG_BEFORE_APPLY
        // if(abs(drag.X) >= CELL_REQUIRED_DRAG_BEFORE_APPLY){
        //     microbeComponent.queuedMovementForce.X += drag.X;
        // }
        // else if (abs(velocity.X) >  .001){
        //     microbeComponent.queuedMovementForce.X += -velocity.X;
        // }

        // if(abs(drag.Z) >= CELL_REQUIRED_DRAG_BEFORE_APPLY){
        //     microbeComponent.queuedMovementForce.Z += drag.Z;
        // }
        // else if (abs(velocity.Z) >  .001){
        //     microbeComponent.queuedMovementForce.Z += -velocity.Z;
        // }
    }

    private void ApplyMovementImpulse(Vector3 movement, float delta)
    {
        if (movement.x == 0.0f && movement.z == 0.0f)
            return;

        // Scale movement by delta time (not by framerate). We aren't Fallout 4
        ApplyCentralImpulse(movement * delta);
    }

    /// <summary>
    ///   Just slerps towards a fixed amount the target point
    /// </summary>
    private Transform GetNewPhysicsRotation(Transform transform)
    {
        var target = transform.LookingAt(LookAtPoint, new Vector3(0, 1, 0));

        // Need to manually normalize everything, otherwise the slerp fails
        Quat slerped = transform.basis.Quat().Normalized().Slerp(
            target.basis.Quat().Normalized(), 0.2f);

        return new Transform(new Basis(slerped), transform.origin);
    }

    private void OnOrganelleAdded(PlacedOrganelle organelle)
    {
        organelle.OnAddedToMicrobe(this);
        processesDirty = true;
        cachedHexCountDirty = true;

        if (organelle.IsAgentVacuole)
            agentVacuoleCount += 1;

        // This is calculated here as it would be a bit difficult to
        // hook up computing this when the StorageBag needs this info.
        organellesCapacity += organelle.StorageCapacity;
        Compounds.Capacity = organellesCapacity;
    }

    private void OnOrganelleRemoved(PlacedOrganelle organelle)
    {
        organellesCapacity -= organelle.StorageCapacity;
        if (organelle.IsAgentVacuole)
            agentVacuoleCount -= 1;
        organelle.OnRemovedFromMicrobe();

        // The organelle only detaches but doesn't delete itself, so we delete it here
        organelle.QueueFree();

        processesDirty = true;
        cachedHexCountDirty = true;

        Compounds.Capacity = organellesCapacity;
    }

    /// <summary>
    ///   Updates the list of processes organelles do
    /// </summary>
    private void RefreshProcesses()
    {
        processes = new List<TweakedProcess>();
        processesDirty = false;

        if (organelles == null)
            return;

        foreach (var entry in organelles.Organelles)
        {
            processes.AddRange(entry.Definition.RunnableProcesses);
        }
    }

    private void CountHexes()
    {
        cachedHexCount = 0;

        if (organelles == null)
            return;

        foreach (var entry in organelles.Organelles)
        {
            cachedHexCount += entry.Definition.Hexes.Count;
        }

        cachedHexCountDirty = false;
    }

    private void SendOrganellePositionsToMembrane()
    {
        var organellePositions = new List<Vector2>();

        foreach (var entry in organelles.Organelles)
        {
            var cartesian = Hex.AxialToCartesian(entry.Position);
            organellePositions.Add(new Vector2(cartesian.x, cartesian.z));
        }

        Membrane.OrganellePositions = organellePositions;
        Membrane.Dirty = true;
    }
}
