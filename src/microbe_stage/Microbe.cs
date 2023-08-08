using System;
using System.Collections.Generic;
using System.Linq;
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
public partial class Microbe : RigidBody, ISaveLoadedTracked, IEngulfable, IInspectableEntity
{
    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new(0, 0, 0);

#pragma warning disable CA2213
    private HybridAudioPlayer engulfAudio = null!;
    private HybridAudioPlayer bindingAudio = null!;
    private HybridAudioPlayer movementAudio = null!;
#pragma warning restore CA2213

    private List<AudioStreamPlayer3D> otherAudioPlayers = new();
    private List<AudioStreamPlayer> nonPositionalAudioPlayers = new();

    /// <summary>
    ///   Init can call _Ready if it hasn't been called yet
    /// </summary>
    private bool onReadyCalled;

    /// <summary>
    ///   We need to know when we should process ourselves or we are ran through <see cref="MicrobeSystem"/>.
    ///   This is this way as microbes can be used for editor previews and also in <see cref="PhotoStudio"/>.
    /// </summary>
    private bool usesExternalProcess;

    private bool absorptionSkippedEarly;

    private bool processesDirty = true;
    private List<TweakedProcess> processes = new();

    private bool cachedHexCountDirty = true;
    private int cachedHexCount;

    private float? cachedRotationSpeed;

    private float? cachedColonyRotationMultiplier;

    private float collisionForce;

    private Vector3 queuedMovementForce;

    private Vector3 lastLinearVelocity;
    private Vector3 lastLinearAcceleration;
    private Vector3 linearAcceleration;

    private float movementSoundCooldownTimer;

    /// <summary>
    ///   Whether this microbe is currently being slowed by environmental slime
    /// </summary>
    private bool slowedBySlime;

    [JsonProperty]
    private int renderPriority = 18;

    private Random random = new();

    private HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>
        activeCompoundDetections = new();

    private HashSet<(Species Species, float Range, Color Colour)>
        activeSpeciesDetections = new();

    private bool? hasSignalingAgent;

    [JsonProperty]
    private MicrobeSignalCommand command = MicrobeSignalCommand.None;

#pragma warning disable CA2213

    /// <summary>
    ///   3d audio listener attached to this microbe if it is the player owned one.
    /// </summary>
    private Listener? listener;
#pragma warning restore CA2213

    private MicrobeSpecies? cachedMicrobeSpecies;
    private EarlyMulticellularSpecies? cachedMulticellularSpecies;

    /// <summary>
    ///   The species of this microbe. It's mandatory to initialize this with <see cref="ApplySpecies"/> otherwise
    ///   random stuff in this instance won't work
    /// </summary>
    [JsonProperty]
    public Species Species { get; private set; } = null!;

    [JsonProperty]
    public CellType? MulticellularCellType { get; private set; }

    /// <summary>
    ///   True when this is the player's microbe
    /// </summary>
    [JsonProperty]
    public bool IsPlayerMicrobe { get; private set; }

    [JsonIgnore]
    public string ReadableName => Species.FormattedName;

    [JsonIgnore]
    public bool IsHoveredOver { get; set; }

    /// <summary>
    ///   Multiplied on the movement speed of the microbe.
    /// </summary>
    [JsonProperty]
    public float MovementFactor { get; private set; } = 1.0f;

    [JsonIgnore]
    public bool IsMulticellular => MulticellularCellType != null;

    [JsonIgnore]
    public ICellProperties CellTypeProperties
    {
        get
        {
            if (MulticellularCellType != null)
            {
                return MulticellularCellType;
            }

            return CastedMicrobeSpecies;
        }
    }

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

            if (CellTypeProperties.IsBacteria)
                radius *= 0.5f;

            return radius;
        }
    }

    [JsonIgnore]
    public float RotationSpeed => cachedRotationSpeed ??=
        MicrobeInternalCalculations.CalculateRotationSpeed(organelles ??
            throw new InvalidOperationException("Organelles not initialized yet"));

    [JsonIgnore]
    public float MassFromOrganelles => organelles?.Sum(o => o.Definition.Mass) ??
        throw new InvalidOperationException("organelles not initialized");

    [JsonIgnore]
    public bool HasSignalingAgent
    {
        get
        {
            if (hasSignalingAgent != null)
                return hasSignalingAgent.Value;

            return CheckHasSignalingAgent();
        }
    }

    [JsonIgnore]
    public MicrobeSignalCommand SignalCommand
    {
        get
        {
            if (!CheckHasSignalingAgent() || Dead)
                return MicrobeSignalCommand.None;

            return command;
        }
    }

    /// <summary>
    ///   Because AI is ran in parallel thread, if it wants to change the signaling, it needs to do it through this
    /// </summary>
    [JsonProperty]
    public MicrobeSignalCommand? QueuedSignalingCommand { get; set; }

    /// <summary>
    ///   Returns a squared value of <see cref="Radius"/>.
    /// </summary>
    [JsonIgnore]
    public float RadiusSquared => Radius * Radius;

    [JsonProperty]
    public int DespawnRadiusSquared { get; set; }

    /// <summary>
    ///   Entity weight for microbes counts all organelles with a scaling factor.
    /// </summary>
    [JsonIgnore]
    public float EntityWeight
    {
        get
        {
            var weight = organelles?.Count * Constants.ORGANELLE_ENTITY_WEIGHT ??
                throw new InvalidOperationException("Organelles not initialised on microbe spawn");

            if (Colony != null)
            {
                // Only colony lead cells have the extra entity weight from the colony added
                // As the colony reads this property on the other members, we do not throw here
                if (Colony.Master == this)
                    weight += Colony.EntityWeight;
            }

            return weight;
        }
    }

    /// <summary>
    ///   If true this shifts the purpose of this cell for visualizations-only
    ///   (Completely stops the normal functioning of the cell).
    /// </summary>
    [JsonIgnore]
    public bool IsForPreviewOnly { get; set; }

    [JsonIgnore]
    public Spatial EntityNode => this;

    [JsonIgnore]
    public GeometryInstance EntityGraphics => Membrane;

    [JsonIgnore]
    public int RenderPriority
    {
        get => renderPriority;
        set
        {
            renderPriority = value;

            if (onReadyCalled)
                ApplyRenderPriority();
        }
    }

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

    [JsonIgnore]
    public Dictionary<Enzyme, int> Enzymes
    {
        get
        {
            if (enzymesDirty)
                RefreshEnzymes();
            return enzymes;
        }
    }

    /// <summary>
    ///   Process running statistics for this cell. For now only computed for the player cell
    /// </summary>
    [JsonIgnore]
    public ProcessStatistics? ProcessStatistics { get; private set; }

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    [JsonProperty]
    public GameProperties CurrentGame { get; private set; } = null!;

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    [JsonIgnore]
    public GameWorld GameWorld => CurrentGame.GameWorld;

    [JsonProperty]
    public float TimeUntilNextAIUpdate { get; set; }

    public bool IsLoadedFromSave { get; set; }

    protected MicrobeSpecies CastedMicrobeSpecies
    {
        get
        {
            if (cachedMicrobeSpecies != null)
                return cachedMicrobeSpecies;

            cachedMicrobeSpecies = (MicrobeSpecies)Species;
            return cachedMicrobeSpecies;
        }
    }

    protected EarlyMulticellularSpecies CastedMulticellularSpecies
    {
        get
        {
            if (cachedMulticellularSpecies != null)
                return cachedMulticellularSpecies;

            cachedMulticellularSpecies = (EarlyMulticellularSpecies)Species;
            return cachedMulticellularSpecies;
        }
    }

    public override void _Ready()
    {
        if (cloudSystem == null && !IsForPreviewOnly)
            throw new InvalidOperationException("Microbe not initialized");

        if (onReadyCalled)
            return;

        Membrane = GetNode<Membrane>("Membrane");
        OrganelleParent = GetNode<Spatial>("OrganelleParent");

        if (IsForPreviewOnly)
        {
            // Disable our physics to not cause issues with multiple preview cells bumping into each other
            Mode = ModeEnum.Kinematic;
            return;
        }

        atp = SimulationParameters.Instance.GetCompound("atp");
        glucose = SimulationParameters.Instance.GetCompound("glucose");
        mucilage = SimulationParameters.Instance.GetCompound("mucilage");
        lipase = SimulationParameters.Instance.GetEnzyme("lipase");

        engulfAudio = GetNode<HybridAudioPlayer>("EngulfAudio");
        bindingAudio = GetNode<HybridAudioPlayer>("BindingAudio");
        movementAudio = GetNode<HybridAudioPlayer>("MovementAudio");

        cellBurstEffectScene = GD.Load<PackedScene>("res://src/microbe_stage/particles/CellBurstEffect.tscn");
        endosomeScene = GD.Load<PackedScene>("res://src/microbe_stage/Endosome.tscn");

        engulfAudio.Positional = movementAudio.Positional = bindingAudio.Positional = !IsPlayerMicrobe;

        // You may notice that there are two separate ways that an audio is played in this class:
        // using pre-existing audio node e.g "bindingAudio", "movementAudio" and through method e.g "PlaySoundEffect",
        // "PlayNonPositionalSoundEffect". The former is approach best used to play looping sounds with more control
        // to the audio player while the latter is more convenient for dynamic and various short one-time sound effects
        // in expense of lesser audio player control.

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

        // pseudopodTarget = GetNode<MeshInstance>("PseudopodTarget");
        // var pseudopodRange = GetNode<Area>("PseudopodRange");
        // pseudopodRangeSphereShape = (SphereShape)pseudopodRange.GetNode<CollisionShape>("SphereShape").Shape;

        // pseudopodRange.Connect("body_entered", this, nameof(OnBodyEnteredPseudopodRange));
        // pseudopodRange.Connect("body_exited", this, nameof(OnBodyExitedPseudopodRange));

        // Setup physics callback stuff
        ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
        Connect("body_shape_entered", this, nameof(OnContactBegin));
        Connect("body_shape_exited", this, nameof(OnContactEnd));

        Mass = Constants.MICROBE_BASE_MASS;

        if (IsLoadedFromSave)
        {
            if (organelles == null)
                throw new JsonException($"Loaded microbe is missing {nameof(organelles)} property");

            // Fix base reproduction cost if we we were loaded from an older save
            if (requiredCompoundsForBaseReproduction.Count < 1)
                SetupRequiredBaseReproductionCompounds();

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
            // And add remake mass for colony master
            if (Colony != null && this != Colony.Master)
            {
                ReParentShapes(this, Vector3.Zero);
                ReParentShapes(Colony.Master, GetOffsetRelativeToMaster());
                Colony.Master.AddCollisionExceptionWith(this);
                AddCollisionExceptionWith(Colony.Master);
                Mode = ModeEnum.Static;
                Colony.Master.Mass += Mass;
            }

            // And recompute storage
            RecomputeOrganelleCapacity();

            // Do species setup that we need on load
            SetScaleFromSpecies();
            SetMembraneFromSpecies();

            // Re-attach engulfed objects
            foreach (var engulfed in engulfedObjects)
            {
                var engulfable = engulfed.Object.Value;
                if (engulfable == null)
                    continue;

                // Some engulfables were already parented to the world, in their case they don't need to be reattached
                // here since the world node already does that.
                // TODO: find out why some engulfables in engulfedObject are not parented to the engulfer?
                if (!engulfable.EntityNode.IsInsideTree())
                    AddChild(engulfable.EntityNode);

                if (engulfed.Phagosome.Value != null)
                {
                    // Defer call to avoid a state where EntityGraphics is still null.
                    // NOTE: My reasoning to why this can happen is due to some IEngulfables implementing
                    // EntityGraphics in a way that it's initialized on _Ready and the problem occurs probably when
                    // that IEngulfable is not yet inside the tree. - Kasterisk
                    Invoke.Instance.Queue(() => engulfable.EntityGraphics.AddChild(engulfed.Phagosome.Value));
                }
            }
        }

        ApplyRenderPriority();

        onReadyCalled = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        if (IsPlayerMicrobe)
            CheatManager.OnPlayerDuplicationCheatUsed += OnPlayerDuplicationCheat;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (IsPlayerMicrobe)
            CheatManager.OnPlayerDuplicationCheatUsed -= OnPlayerDuplicationCheat;
    }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(CompoundCloudSystem cloudSystem, ISpawnSystem spawnSystem, GameProperties currentGame,
        bool isPlayer)
    {
        this.cloudSystem = cloudSystem;
        this.spawnSystem = spawnSystem;
        CurrentGame = currentGame;
        IsPlayerMicrobe = isPlayer;

        if (!isPlayer)
        {
            // TODO: the spawner should set the AI component
            throw new NotImplementedException();
        }

        // Needed for immediately applying the species
        _Ready();
    }

    public override void _Process(float delta)
    {
        if (usesExternalProcess)
        {
            GD.PrintErr("_Process was called for microbe that uses external processing");
            return;
        }

        ProcessEarlyAsync(delta);
        ProcessSync(delta);
    }

    public override void _PhysicsProcess(float delta)
    {
        linearAcceleration = (LinearVelocity - lastLinearVelocity) / delta;

        // Movement
        if (ColonyParent == null && !IsForPreviewOnly)
        {
            HandleMovement(delta);
        }
        else
        {
            Colony?.Master.AddMovementForce(queuedMovementForce);
        }

        lastLinearVelocity = LinearVelocity;
        lastLinearAcceleration = linearAcceleration;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState physicsState)
    {
        if (ColonyParent != null)
            return;

        // TODO: should movement also be applied here?

        physicsState.Transform = GetNewPhysicsRotation(physicsState.Transform);

        // Reset total sum from previous collisions
        collisionForce = 0.0f;

        // Sum impulses from all contact points
        for (var i = 0; i < physicsState.GetContactCount(); ++i)
        {
            // TODO: Godot currently does not provide a convenient way to access a collision impulse, this
            // for example is luckily available only in Bullet which makes things a bit easier. Would need
            // proper handling for this in the future.
            collisionForce += physicsState.GetContactImpulse(i);
        }
    }

    /// <summary>
    ///   Applies the species for this cell. Called when spawned
    /// </summary>
    public void ApplySpecies(Species species)
    {
        cachedMicrobeSpecies = null;
        cachedMulticellularSpecies = null;

        Species = species;

        if (species is MicrobeSpecies microbeSpecies)
        {
            // We might as well store this here as we already casted it. This property is not saved to make working
            // with earlier saves easier
            cachedMicrobeSpecies = microbeSpecies;
        }
        else if (species is EarlyMulticellularSpecies earlyMulticellularSpecies)
        {
            // The first cell of a species is the first cell of the multicellular species, others are created with
            // ApplyMulticellularNonFirstCellSpecies
            MulticellularCellType = earlyMulticellularSpecies.Cells[0].CellType;

            cachedMulticellularSpecies = earlyMulticellularSpecies;
        }
        else
        {
            throw new ArgumentException("Microbe can only be a microbe or early multicellular species");
        }

        cachedRotationSpeed = CellTypeProperties.BaseRotationSpeed;

        if (!IsForPreviewOnly)
        {
            SetupRequiredBaseReproductionCompounds();
        }

        FinishSpeciesSetup();
    }

    /// <summary>
    ///   Gets the actually hit microbe (potentially in a colony)
    /// </summary>
    /// <param name="bodyShape">The shape that was hit</param>
    /// <returns>The actual microbe that was hit or null if the bodyShape was not found</returns>
    public Microbe? GetMicrobeFromShape(int bodyShape)
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

    public void ReportActiveCompoundChemoreceptor(Compound compound, float range, float minAmount, Color colour)
    {
        activeCompoundDetections.Add((compound, range, minAmount, colour));
    }

    public void ReportActiveSpeciesChemoreceptor(Species species, float range, float minAmount, Color colour)
    {
        activeSpeciesDetections.Add((species, range, colour));
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
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.UnitDb = GD.Linear2Db(volume);
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
            player.Bus = "SFX";

            AddChild(player);
            nonPositionalAudioPlayers.Add(player);
        }

        player.VolumeDb = GD.Linear2Db(volume);
        player.Stream = sound;
        player.Play();
    }

    public void NotifyExternalProcessingIsUsed()
    {
        if (usesExternalProcess)
            return;

        usesExternalProcess = true;
        SetProcess(false);
    }

    /// <summary>
    ///   Async part of microbe processing
    /// </summary>
    /// <param name="delta">Time since the last call</param>
    /// <remarks>
    ///   <para>
    ///     TODO: microbe processing needs more refactoring in the individual operation methods to really allow more
    ///     work to be put in this asynchronous processing method
    ///   </para>
    /// </remarks>
    public void ProcessEarlyAsync(float delta)
    {
        if (membraneOrganellePositionsAreDirty)
        {
            // Redo the cell membrane.
            SendOrganellePositionsToMembrane();

            membraneOrganellesWereUpdatedThisFrame = true;
        }
        else
        {
            membraneOrganellesWereUpdatedThisFrame = false;
        }

        // The code below starting from here is not needed for a display-only cell
        if (IsForPreviewOnly)
            return;

        // Movement factor is reset here. HandleEngulfing will set the right value
        MovementFactor = 1.0f;
        queuedMovementForce = new Vector3(0, 0, 0);

        // Reduce agent emission cooldown
        AgentEmissionCooldown -= delta;
        if (AgentEmissionCooldown < 0)
            AgentEmissionCooldown = 0;

        slimeSecretionCooldown -= delta;
        if (slimeSecretionCooldown < 0)
            slimeSecretionCooldown = 0;

        lastCheckedATPDamage += delta;

        if (!Membrane.Dirty)
        {
            HandleCompoundAbsorbing(delta);
        }
        else
        {
            absorptionSkippedEarly = true;
        }

        // Colony members have their movement update before organelle update,
        // so that the movement organelles see the direction
        // The colony master should be already updated as the movement direction is either set by the player input or
        // microbe AI, neither of which will happen concurrently, so this should always get the up to date value
        if (Colony != null && Colony.Master != this)
            MovementDirection = Colony.Master.MovementDirection;

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles!.Organelles)
        {
            organelle.UpdateAsync(delta);
        }

        HandleHitpointsRegeneration(delta);

        HandleInvulnerabilityDecay(delta);

        HandleOsmoregulation(delta);

        if (!Membrane.Dirty)
            HandleCompoundVenting(delta);
    }

    public void ProcessSync(float delta)
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

        if (membraneOrganellesWereUpdatedThisFrame && IsForPreviewOnly)
        {
            if (organelles == null)
                throw new InvalidOperationException("Preview microbe was not initialized with organelles list");

            // Update once for the positioning of external organelles
            foreach (var organelle in organelles.Organelles)
            {
                organelle.UpdateAsync(delta);
                organelle.UpdateSync();
            }
        }

        // The code below starting from here is not needed for a display-only cell
        if (IsForPreviewOnly)
            return;

        CheckEngulfShape();

        // Fire queued agents
        if (queuedToxinToEmit != null)
        {
            EmitToxin(queuedToxinToEmit);
            queuedToxinToEmit = null;
        }

        HandleSlimeSecretion(delta);

        // If we didn't have our membrane ready yet in the async process we need to do these now
        if (absorptionSkippedEarly)
        {
            HandleCompoundAbsorbing(delta);
            HandleCompoundVenting(delta);
            absorptionSkippedEarly = false;
        }

        HandleFlashing(delta);

        HandleReproduction(delta);

        // Handles engulfing related stuff as well as modifies the movement factor.
        // This needs to be done before Update is called on organelles as movement organelles will use MovementFactor.
        HandleEngulfing(delta);

        HandleDigestion(delta);

        // Handles binding related stuff
        HandleBinding(delta);
        HandleUnbinding();

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles!.Organelles)
        {
            organelle.UpdateSync();
        }

        if (QueuedSignalingCommand != null)
        {
            command = QueuedSignalingCommand.Value;
            QueuedSignalingCommand = null;
        }

        // Rotation is applied in the physics force callback as that's the place where the body rotation
        // can be directly set without problems

        HandleChemoreceptorLines(delta);

        if (Colony != null && Colony.Master == this)
            Colony.Process(delta);

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

    /// <summary>
    ///   Returns a list of tuples, representing all possible compound targets. These are not all clouds that the
    ///   microbe can smell; only the best candidate of each compound type.
    /// </summary>
    /// <param name="clouds">CompoundCloudSystem to scan</param>
    /// <returns>
    ///   A list of tuples. Each tuple contains the type of compound, the color of the line (if any needs to be drawn),
    ///   and the location where the compound is located.
    /// </returns>
    public List<(Compound Compound, Color Colour, Vector3 Target)> GetDetectedCompounds(CompoundCloudSystem clouds)
    {
        HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)> collectedUniqueCompoundDetections;

        // Colony lead cell uses all the chemoreceptors in the colony to make them all work
        if (Colony != null && Colony.Master == this)
        {
            collectedUniqueCompoundDetections =
                new HashSet<(Compound Compound, float Range, float MinAmount, Color Colour)>();

            foreach (var colonyMicrobe in Colony.ColonyMembers)
            {
                collectedUniqueCompoundDetections.UnionWith(colonyMicrobe.activeCompoundDetections);
            }
        }
        else
        {
            collectedUniqueCompoundDetections = activeCompoundDetections;
        }

        var detections = new List<(Compound Compound, Color Colour, Vector3 Target)>();
        var position = GlobalTranslation;

        foreach (var (compound, range, minAmount, colour) in collectedUniqueCompoundDetections)
        {
            var detectedCompound = clouds.FindCompoundNearPoint(position, compound, range, minAmount);

            if (detectedCompound != null)
            {
                detections.Add((compound, colour, detectedCompound.Value));
            }
        }

        return detections;
    }

    /// <summary>
    ///   Returns a list of tuples, representing all possible microbe targets. These are not all the
    ///   other microbes that the microbe can smell; only the best candidate of each species.
    /// </summary>
    /// <param name="microbeSystem">MicrobeSystem to scan</param>
    /// <returns>
    ///   A list of tuples. Each tuple contains the type of species, the color of the line (if any needs to be drawn),
    ///   and the location where the microbe is located.
    /// </returns>
    public List<(Microbe Microbe, Color Colour, Vector3 Target)> GetDetectedSpecies(MicrobeSystem microbeSystem)
    {
        HashSet<(Species Species, float Range, Color Colour)> collectedUniqueSpeciesDetections;

        // Colony lead cell uses all the chemoreceptors in the colony to make them all work
        if (Colony != null && Colony.Master == this)
        {
            collectedUniqueSpeciesDetections =
                new HashSet<(Species Species, float Range, Color Colour)>();

            foreach (var colonyMicrobe in Colony.ColonyMembers)
            {
                collectedUniqueSpeciesDetections.UnionWith(colonyMicrobe.activeSpeciesDetections);
            }
        }
        else
        {
            collectedUniqueSpeciesDetections = activeSpeciesDetections;
        }

        var detections = new List<(Microbe Microbe, Color Colour, Vector3 Target)>();
        var position = GlobalTranslation;

        foreach (var (species, range, colour) in collectedUniqueSpeciesDetections)
        {
            var tuple = microbeSystem.FindSpeciesNearPoint(position, species, range);

            if (tuple != null)
            {
                detections.Add((tuple.Value.Microbe, colour, tuple.Value.Position));
            }
        }

        return detections;
    }

    /// <summary>
    ///   Tries to find an engulfable entity as close to this microbe as possible.
    /// </summary>
    /// <param name="engulfables">List of all engulfable entities in the world</param>
    /// <param name="searchRadius">How wide to search around the point</param>
    /// <returns>The nearest found point for the engulfable entity or null</returns>
    public Vector3? FindNearestEngulfable(List<IEngulfable> engulfables, float searchRadius = 200)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        // If the microbe cannot absorb, no need for this
        if (!CanEngulf)
            return null;

        Vector3? nearestPoint = null;
        float nearestDistanceSquared = float.MaxValue;
        var searchRadiusSquared = searchRadius * searchRadius;

        // Retrieve nearest potential entities
        foreach (var entity in engulfables)
        {
            if (entity.Compounds.Compounds.Count <= 0 || entity.PhagocytosisStep != PhagocytosisPhase.None)
                continue;

            var spatial = entity.EntityNode;

            // Skip entities that are out of range
            if ((spatial.Translation - Translation).LengthSquared() > searchRadiusSquared)
                continue;

            // Skip non-engulfable entities
            if (CanEngulfObject(entity) != EngulfCheckResult.Ok)
                continue;

            // Skip entities that have no useful compounds
            if (!entity.Compounds.Compounds.Any(x => Compounds.IsUseful(x.Key)))
                continue;

            var distance = (spatial.Translation - Translation).LengthSquared();

            if (nearestPoint == null || distance < nearestDistanceSquared)
            {
                nearestPoint = spatial.Translation;
                nearestDistanceSquared = distance;
            }
        }

        return nearestPoint;
    }

    public void OverrideScaleForPreview(float scale)
    {
        if (!IsForPreviewOnly)
            throw new InvalidOperationException("Scale can only be overridden for preview microbes");

        ApplyScale(new Vector3(scale, scale, scale));
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
        if (ColonyParent == null)
            throw new InvalidOperationException("This microbe doesn't have colony parent set");

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

    private void FinishSpeciesSetup()
    {
        if (CellTypeProperties.Organelles.Count < 1)
            throw new ArgumentException("Species with no organelles is not valid");

        SetScaleFromSpecies();

        ResetOrganelleLayout();

        SetMembraneFromSpecies();

        if (!CanEngulf)
        {
            // Reset engulf mode if the new membrane doesn't allow it
            if (State == MicrobeState.Engulf)
                State = MicrobeState.Normal;
        }

        SetupMicrobeHitpoints();
    }

    private void SetScaleFromSpecies()
    {
        var scale = new Vector3(1.0f, 1.0f, 1.0f);

        // Bacteria are 50% the size of other cells
        if (CellTypeProperties.IsBacteria)
            scale = new Vector3(0.5f, 0.5f, 0.5f);

        ApplyScale(scale);
    }

    private void ApplyScale(Vector3 scale)
    {
        // Scale only the graphics parts to not have physics affected
        Membrane.Scale = scale;
        OrganelleParent.Scale = scale;
    }

    private void ApplyRenderPriority()
    {
        var material = Membrane.MaterialToEdit;

        if (material != null)
            material.RenderPriority = RenderPriority;
    }

    private Node GetStageAsParent()
    {
        if (HostileEngulfer.Value != null)
            return HostileEngulfer.Value.GetStageAsParent();

        if (Colony == null)
            return GetParent();

        // If the colony leader is engulfed, the colony children, when the colony is disbanded, need to access the
        // stage through the engulfer. Because at that point the colony leader is already re-parented to the engulfer,
        // so its parent is no longer the stage here.
        if (Colony.Master.HostileEngulfer.Value != null)
            return Colony.Master.HostileEngulfer.Value.GetStageAsParent();

        return Colony.Master.GetParent();
    }

    private Vector3 DoBaseMovementForce(float delta)
    {
        var cost = (Constants.BASE_MOVEMENT_ATP_COST * HexCount) * delta;

        var got = Compounds.TakeCompound(atp, cost);

        float force = Constants.CELL_BASE_THRUST;
        float appliedFactor = MovementFactor;
        if (Colony != null && Colony.Master == this)
        {
            // Multiplies the movement factor as if the colony has the normal microbe speed
            // Then it subtracts movement speed from 100% up to 75%(soft cap),
            // using a series that converges to 1 , value = (1/2 + 1/4 + 1/8 +.....) = 1 - 1/2^n
            // when specialized cells become a reality the cap could be lowered to encourage cell specialization
            appliedFactor *= Colony.ColonyMembers.Count;
            var seriesValue = 1 - 1 / (float)Math.Pow(2, Colony.ColonyMembers.Count - 1);
            appliedFactor -= (appliedFactor * 0.15f) * seriesValue;
        }

        // Halve speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;
        }

        if (slowedBySlime)
            force /= Constants.MUCILAGE_IMPEDE_FACTOR;

        if (IsPlayerMicrobe && CheatManager.Speed > 1)
            force *= Mass * CheatManager.Speed;

        return Transform.basis.Xform(MovementDirection * force) * appliedFactor *
            (CellTypeProperties.MembraneType.MovementFactor -
                (CellTypeProperties.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER));
    }

    private void ApplyMovementImpulse(Vector3 movement, float delta)
    {
        if (movement.x == 0.0f && movement.z == 0.0f)
            return;

        // Scale movement by delta time (not by framerate). We aren't Fallout 4
        // TODO: it seems that at low framerate (below 20 or so) cells get a speed boost for some reason
        ApplyCentralImpulse(movement * delta);
    }

    /// <summary>
    ///   Just slerps towards the target point with the amount being defined by the cell rotation speed.
    ///   For now, eventually we want to use physics forces to turn
    /// </summary>
    private Transform GetNewPhysicsRotation(Transform transform)
    {
        var target = transform.LookingAt(LookAtPoint, new Vector3(0, 1, 0));

        float speed = RotationSpeed;

        if (IsPlayerMicrobe && CheatManager.Speed > 1)
            speed *= CheatManager.Speed;

        var ownRotation = RotationSpeed;

        if (Colony != null && ColonyParent == null)
        {
            // Calculate help and extra inertia caused by the colony member cells
            if (cachedColonyRotationMultiplier == null)
            {
                // TODO: move this to MicrobeInternalCalculations once this is needed to be shown in the multicellular
                // editor
                float colonyInertia = 0.1f;
                float colonyRotationHelp = 0;

                foreach (var colonyMember in Colony.ColonyMembers)
                {
                    if (colonyMember == this)
                        continue;

                    var distance = colonyMember.Transform.origin.LengthSquared();

                    if (distance < MathUtils.EPSILON)
                        continue;

                    colonyInertia += distance * colonyMember.MassFromOrganelles *
                        Constants.CELL_MOMENT_OF_INERTIA_DISTANCE_MULTIPLIER;

                    // TODO: should this use the member rotation speed (which is dependent on its size and how many
                    // cilia there are that far away) or just a count of cilia and the distance
                    colonyRotationHelp += colonyMember.RotationSpeed *
                        Constants.CELL_COLONY_MEMBER_ROTATION_FACTOR_MULTIPLIER * Mathf.Sqrt(distance);
                }

                var multiplier = colonyRotationHelp / colonyInertia;

                cachedColonyRotationMultiplier = Mathf.Clamp(multiplier, Constants.CELL_COLONY_MIN_ROTATION_MULTIPLIER,
                    Constants.CELL_COLONY_MAX_ROTATION_MULTIPLIER);
            }

            speed *= cachedColonyRotationMultiplier.Value;

            speed = Mathf.Clamp(speed, Constants.CELL_MIN_ROTATION,
                Math.Min(ownRotation * Constants.CELL_COLONY_MAX_ROTATION_HELP, Constants.CELL_MAX_ROTATION));
        }

        // Need to manually normalize everything, otherwise the slerp fails
        // Delta is not used here as the physics frames occur at a fixed number of times per second
        Quat slerped = transform.basis.Quat().Normalized().Slerp(target.basis.Quat().Normalized(), speed);

        return new Transform(new Basis(slerped), transform.origin);
    }

    /// <summary>
    ///   Updates the list of processes organelles do
    /// </summary>
    private void RefreshProcesses()
    {
        processes.Clear();

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

    private void RefreshEnzymes()
    {
        enzymes.Clear();

        if (organelles == null)
            return;

        // Cells have a minimum of at least one unit of lipase enzyme
        enzymes[lipase] = 1;

        foreach (var organelle in organelles.Organelles)
        {
            foreach (var enzyme in organelle.StoredEnzymes)
            {
                // Filter out invalid enzyme values
                if (enzyme.Value <= 0)
                    continue;

                enzymes.TryGetValue(enzyme.Key, out int existing);
                enzymes[enzyme.Key] = existing + enzyme.Value;
            }
        }

        enzymesDirty = false;
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
