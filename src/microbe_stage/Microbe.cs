using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Init, _Ready, _Process,
///   Processes, Species, Audio, Movement
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/Microbe.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public partial class Microbe : RigidBody, ISpawned, IProcessable, IMicrobeAI, ISaveLoadedTracked
{
    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    private AudioStreamPlayer3D engulfAudio;
    private AudioStreamPlayer3D bindingAudio;
    private AudioStreamPlayer3D movementAudio;
    private List<AudioStreamPlayer3D> otherAudioPlayers = new List<AudioStreamPlayer3D>();
    private List<AudioStreamPlayer> nonPositionalAudioPlayers = new List<AudioStreamPlayer>();

    /// <summary>
    ///   Init can call _Ready if it hasn't been called yet
    /// </summary>
    private bool onReadyCalled;

    private bool processesDirty = true;
    private List<TweakedProcess> processes;

    private bool cachedHexCountDirty = true;
    private int cachedHexCount;

    private Vector3 collisionForce;

    private Vector3 queuedMovementForce;

    [JsonProperty]
    private MicrobeAI ai;

    /// <summary>
    ///   3d audio listener attached to this microbe if it is the player owned one.
    /// </summary>
    private Listener listener;

    /// <summary>
    ///   The species of this microbe
    /// </summary>
    [JsonProperty]
    public MicrobeSpecies Species { get; private set; }

    /// <summary>
    ///    True when this is the player's microbe
    /// </summary>
    [JsonProperty]
    public bool IsPlayerMicrobe { get; private set; }

    [JsonIgnore]
    public bool IsHoveredOver { get; set; }

    /// <summary>
    ///   Multiplied on the movement speed of the microbe.
    /// </summary>
    [JsonProperty]
    public float MovementFactor { get; private set; } = 1.0f;

    [JsonIgnore]
    public int HexCount
    {
        get
        {
            if (cachedHexCountDirty)
                CountHexes();

            return cachedHexCount;
        }
    }

    [JsonIgnore]
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

    /// <summary>
    ///   Returns a squared value of <see cref="Radius"/>.
    /// </summary>
    [JsonIgnore]
    public float RadiusSquared => Radius * Radius;

    [JsonProperty]
    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   If true this shifts the purpose of this cell for visualizations-only
    ///   (stops the normal functioning of the cell).
    /// </summary>
    [JsonIgnore]
    public bool IsForPreviewOnly { get; set; }

    [JsonIgnore]
    public Node EntityNode => this;

    [JsonIgnore]
    public List<TweakedProcess> ActiveProcesses
    {
        get
        {
            if (processesDirty)
                RefreshProcesses();
            return processes;
        }
    }

    /// <summary>
    ///   Process running statistics for this cell. For now only computed for the player cell
    /// </summary>
    [JsonIgnore]
    public ProcessStatistics ProcessStatistics { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; }

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    public bool IsLoadedFromSave { get; set; }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(CompoundCloudSystem cloudSystem, GameProperties currentGame, bool isPlayer)
    {
        this.cloudSystem = cloudSystem;
        CurrentGame = currentGame;
        IsPlayerMicrobe = isPlayer;

        if (!isPlayer)
            ai = new MicrobeAI(this);

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Ready()
    {
        if (cloudSystem == null && !IsForPreviewOnly)
        {
            throw new Exception("Microbe not initialized");
        }

        if (onReadyCalled)
            return;

        Membrane = GetNode<Membrane>("Membrane");
        OrganelleParent = GetNode<Spatial>("OrganelleParent");
        engulfAudio = GetNode<AudioStreamPlayer3D>("EngulfAudio");
        bindingAudio = GetNode<AudioStreamPlayer3D>("BindingAudio");
        movementAudio = GetNode<AudioStreamPlayer3D>("MovementAudio");

        cellBurstEffectScene = GD.Load<PackedScene>("res://src/microbe_stage/particles/CellBurstEffect.tscn");

        if (IsPlayerMicrobe)
        {
            // Creates and activates the audio listener for the player microbe. Positional sound will be
            // received by it instead of the main camera.
            listener = new Listener();
            AddChild(listener);
            listener.MakeCurrent();

            // Setup tracking running processes
            ProcessStatistics = new ProcessStatistics();

            GD.Print("Player Microbe spawned");
        }

        // Setup physics callback stuff
        var engulfDetector = GetNode<Area>("EngulfDetector");
        engulfShape = (SphereShape)engulfDetector.GetNode<CollisionShape>("EngulfShape").Shape;

        engulfDetector.Connect("body_entered", this, nameof(OnBodyEnteredEngulfArea));
        engulfDetector.Connect("body_exited", this, nameof(OnBodyExitedEngulfArea));

        ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
        Connect("body_shape_entered", this, nameof(OnContactBegin));
        Connect("body_shape_exited", this, nameof(OnContactEnd));

        Mass = Constants.MICROBE_BASE_MASS;

        if (IsLoadedFromSave)
        {
            // Fix the tree of colonies
            if (ColonyChildren != null)
            {
                foreach (var child in ColonyChildren)
                {
                    AddChild(child);
                }
            }

            // Need to re-attach our organelles
            foreach (var organelle in organelles)
                OrganelleParent.AddChild(organelle);

            // Colony children shapes need re-parenting to their master
            // The shapes have to be re-parented to their original microbe then to the master again, maybe engine bug
            // Also re-add to the collision exception and change the mode to static as it should be
            if (Colony != null && this != Colony.Master)
            {
                ReParentShapes(this, Vector3.Zero);
                ReParentShapes(Colony.Master, GetOffsetRelativeToMaster());
                Colony.Master.AddCollisionExceptionWith(this);
                AddCollisionExceptionWith(Colony.Master);
                Mode = ModeEnum.Static;
            }

            // And recompute storage
            RecomputeOrganelleCapacity();

            // Do species setup that we need on load
            SetScaleFromSpecies();
            SetMembraneFromSpecies();
        }

        onReadyCalled = true;
    }

    /// <summary>
    ///   Applies the species for this cell. Called when spawned
    /// </summary>
    public void ApplySpecies(Species species)
    {
        Species = (MicrobeSpecies)species;

        if (Species.Organelles.Count < 1)
            throw new ArgumentException("Species with no organelles is not valid");

        SetScaleFromSpecies();

        ResetOrganelleLayout();

        SetMembraneFromSpecies();

        if (Membrane.Type.CellWall)
        {
            // Reset engulf mode if the new membrane doesn't allow it
            if (State == MicrobeState.Engulf)
                State = MicrobeState.Normal;
        }

        SetupMicrobeHitpoints();
    }

    /// <summary>
    ///   Gets the actually hit microbe (potentially in a colony)
    /// </summary>
    /// <param name="bodyShape">The shape that was hit</param>
    /// <returns>The actual microbe that was hit or null if the bodyShape was not found</returns>
    public Microbe GetMicrobeFromShape(int bodyShape)
    {
        if (Colony == null)
            return this;

        var touchedOwnerId = ShapeFindOwner(bodyShape);

        // Not found
        if (touchedOwnerId == uint.MaxValue)
            return null;

        return GetColonyMemberWithShapeOwner(touchedOwnerId, Colony);
    }

    /// <summary>
    ///   Called from movement organelles to add movement force
    /// </summary>
    public void AddMovementForce(Vector3 force)
    {
        queuedMovementForce += force;
    }

    public void PlaySoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        var sound = GD.Load<AudioStream>(effect);

        // Find a player not in use or create a new one if none are available.
        var player = otherAudioPlayers.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (otherAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer3D();
            player.UnitDb = GD.Linear2Db(volume);
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.Stream = sound;
        player.Play();
    }

    public void PlayNonPositionalSoundEffect(string effect, float volume = 1.0f)
    {
        // TODO: make these sound objects only be loaded once
        var sound = GD.Load<AudioStream>(effect);

        // Find a player not in use or create a new one if none are available.
        var player = nonPositionalAudioPlayers.Find(nextPlayer => !nextPlayer.Playing);

        if (player == null)
        {
            // If we hit the player limit just return and ignore the sound.
            if (nonPositionalAudioPlayers.Count >= Constants.MAX_CONCURRENT_SOUNDS_PER_ENTITY)
                return;

            player = new AudioStreamPlayer();
            player.VolumeDb = GD.Linear2Db(volume);
            player.Bus = "SFX";

            AddChild(player);
            nonPositionalAudioPlayers.Add(player);
        }

        player.Stream = sound;
        player.Play();
    }

    public override void _Process(float delta)
    {
        // Updates the listener if this is the player owned microbe.
        if (listener != null)
        {
            // Listener is directional and since it is a child of the microbe it will have the same forward
            // vector as the parent. Since we want sound to come from the side of the screen relative to the
            // camera rather than the microbe we need to force the listener to face up every frame.
            Transform transform = GlobalTransform;
            transform.basis = new Basis(new Vector3(0.0f, 0.0f, -1.0f));
            listener.GlobalTransform = transform;
        }

        if (membraneOrganellePositionsAreDirty)
        {
            // Redo the cell membrane.
            SendOrganellePositionsToMembrane();

            if (IsForPreviewOnly)
            {
                // Update once for the positioning of external organelles
                foreach (var organelle in organelles.Organelles)
                    organelle.Update(delta);
            }
        }

        // The code below starting from here is not needed for a display-only cell
        if (IsForPreviewOnly)
            return;

        CheckEngulfShapeSize();

        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        HandleCompoundAbsorbing(delta);

        // Movement factor is reset here. HandleEngulfing will set the right value
        MovementFactor = 1.0f;
        queuedMovementForce = new Vector3(0, 0, 0);

        // Reduce agent emission cooldown
        AgentEmissionCooldown -= delta;
        if (AgentEmissionCooldown < 0)
            AgentEmissionCooldown = 0;

        // Fire queued agents
        if (queuedToxinToEmit != null)
        {
            EmitToxin(queuedToxinToEmit);
            queuedToxinToEmit = null;
        }

        HandleFlashing(delta);
        HandleHitpointsRegeneration(delta);
        HandleReproduction(delta);

        // Handles engulfing related stuff as well as modifies the
        // movement factor. This needs to be done before Update is
        // called on organelles as movement organelles will use
        // MovementFactor.
        HandleEngulfing(delta);

        // Handles binding related stuff
        HandleBinding(delta);
        HandleUnbinding();

        HandleOsmoregulation(delta);

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles.Organelles)
        {
            organelle.Update(delta);
        }

        // Movement
        if (ColonyParent == null)
        {
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

                // Play movement sound if one isn't already playing.
                if (!movementAudio.Playing)
                    movementAudio.Play();
            }
        }

        // Rotation is applied in the physics force callback as that's
        // the place where the body rotation can be directly set
        // without problems

        HandleCompoundVenting(delta);

        if (Colony != null && Colony.Master == this)
            Colony.Process(delta);

        lastCheckedATPDamage += delta;

        while (lastCheckedATPDamage >= Constants.ATP_DAMAGE_CHECK_INTERVAL)
        {
            lastCheckedATPDamage -= Constants.ATP_DAMAGE_CHECK_INTERVAL;
            ApplyATPDamage();
        }

        Membrane.HealthFraction = Hitpoints / MaxHitpoints;

        if (Hitpoints <= 0 || Dead)
        {
            HandleDeath(delta);
        }
        else
        {
            // As long as the player has been alive they can go to the editor in freebuild
            if (OnReproductionStatus != null && CurrentGame.FreeBuild)
            {
                OnReproductionStatus(this, true);
            }
        }
    }

    public override void _EnterTree()
    {
        if (IsPlayerMicrobe)
            CheatManager.OnPlayerDuplicationCheatUsed += OnPlayerDuplicationCheat;
    }

    public override void _ExitTree()
    {
        if (IsPlayerMicrobe)
            CheatManager.OnPlayerDuplicationCheatUsed -= OnPlayerDuplicationCheat;

        base._ExitTree();
    }

    public void AIThink(float delta, Random random, MicrobeAICommonData data)
    {
        if (IsPlayerMicrobe)
            throw new InvalidOperationException("AI can't run on the player microbe");

        if (Dead)
            return;

        try
        {
            ai.Think(delta, random, data);
        }
#pragma warning disable CA1031 // AI needs to be boxed good
        catch (Exception e)
#pragma warning restore CA1031
        {
            GD.PrintErr("Microbe AI failure! " + e);
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState physicsState)
    {
        if (ColonyParent != null)
            return;

        // TODO: should movement also be applied here?

        physicsState.Transform = GetNewPhysicsRotation(physicsState.Transform);

        collisionForce = Vector3.Zero;

        for (var i = 0; i < physicsState.GetContactCount(); ++i)
        {
            collisionForce += physicsState.GetContactImpulse(i) * physicsState.GetContactLocalNormal(i);
        }
    }

    /// <summary>
    ///   This method calculates the relative rotation and translation this microbe should have to its microbe parent.
    ///   <a href="https://randomthrivefiles.b-cdn.net/documentation/fixed_colony_rotation_explanation_image.png">
    ///     Visual explanation
    ///   </a>
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Storing the old global translation and rotation, re-parenting and then reapplying the stored values is
    ///     worse than this code because this code utilizes GetVectorTowardsNearestPointOfMembrane. This reduces the
    ///     visual gap between the microbes in a colony.
    ///   </para>
    /// </remarks>
    /// <returns>Returns relative translation and rotation</returns>
    private (Vector3 Translation, Vector3 Rotation) GetNewRelativeTransform()
    {
        // Gets the global rotation of the parent
        var globalParentRotation = ColonyParent.GlobalTransform.basis.GetEuler();

        // A vector from the parent to me
        var vectorFromParent = GlobalTransform.origin - ColonyParent.GlobalTransform.origin;

        // A vector from me to the parent
        var vectorToParent = -vectorFromParent;

        // TODO: using quaternions here instead of assuming that rotating about the up/down axis is right would be nice
        // This vector represents the vectorToParent as if I had no rotation.
        // This works by rotating vectorToParent by the negative value (therefore Down) of my current rotation
        // This is important, because GetVectorTowardsNearestPointOfMembrane only works with non-rotated microbes
        var vectorToParentWithoutRotation = vectorToParent.Rotated(Vector3.Down, Rotation.y);

        // This vector represents the vectorFromParent as if the parent had no rotation.
        var vectorFromParentWithoutRotation = vectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

        // Calculates the vector from the center of the parent's membrane towards me with canceled out rotation.
        // This gets added to the vector calculated one call before.
        var correctedVectorFromParent = ColonyParent.Membrane
            .GetVectorTowardsNearestPointOfMembrane(vectorFromParentWithoutRotation.x,
                vectorFromParentWithoutRotation.z).Rotated(Vector3.Up, globalParentRotation.y);

        // Calculates the vector from my center to my membrane towards the parent.
        // This vector gets rotated back to cancel out the rotation applied two calls above.
        // -= to negate the vector, so that the two membrane vectors amplify
        correctedVectorFromParent -= Membrane
            .GetVectorTowardsNearestPointOfMembrane(vectorToParentWithoutRotation.x, vectorToParentWithoutRotation.z)
            .Rotated(Vector3.Up, Rotation.y);

        // Rotated because the rotational scope is different.
        var newTranslation = correctedVectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

        return (newTranslation, Rotation - globalParentRotation);
    }

    private void SetScaleFromSpecies()
    {
        var scale = new Vector3(1.0f, 1.0f, 1.0f);

        // Bacteria are 50% the size of other cells
        if (Species.IsBacteria)
            scale = new Vector3(0.5f, 0.5f, 0.5f);

        // Scale only the graphics parts to not have physics affected
        Membrane.Scale = scale;
        OrganelleParent.Scale = scale;
    }

    private Node GetStageAsParent()
    {
        if (Colony == null)
            return GetParent();

        return Colony.Master.GetParent();
    }

    private Vector3 DoBaseMovementForce(float delta)
    {
        var cost = (Constants.BASE_MOVEMENT_ATP_COST * HexCount) * delta;

        var got = Compounds.TakeCompound(atp, cost);

        float force = Constants.CELL_BASE_THRUST;

        // Halve speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;
        }

        if (IsPlayerMicrobe)
            force *= CheatManager.Speed;

        return Transform.basis.Xform(MovementDirection * force) * MovementFactor *
            (Species.MembraneType.MovementFactor -
                (Species.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER));
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

    /// <summary>
    ///   Updates the list of processes organelles do
    /// </summary>
    private void RefreshProcesses()
    {
        if (processes == null)
        {
            processes = new List<TweakedProcess>();
        }
        else
        {
            processes.Clear();
        }

        if (organelles == null)
            return;

        foreach (var entry in organelles.Organelles)
        {
            // Duplicate processes need to be combined into a single TweakedProcess
            foreach (var process in entry.Definition.RunnableProcesses)
            {
                bool found = false;

                foreach (var existing in processes)
                {
                    if (existing.Process == process.Process)
                    {
                        existing.Rate += process.Rate;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Because we modify the process, we must duplicate the object for each microbe
                    processes.Add((TweakedProcess)process.Clone());
                }
            }
        }

        processesDirty = false;
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
}
