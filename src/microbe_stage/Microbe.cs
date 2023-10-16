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
public partial class Microbe : RigidBody, ISaveLoadedTracked, IEngulfable
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

    private void CountHexes()
    {
        throw new NotImplementedException();
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

    private bool CheckHasSignalingAgent()
    {
        throw new NotImplementedException();
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

            throw new NotImplementedException();

            // if (Colony != null)
            // {
            //     // Only colony lead cells have the extra entity weight from the colony added
            //     // As the colony reads this property on the other members, we do not throw here
            //     if (Colony.Master == this)
            //         weight += Colony.EntityWeight;
            // }

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

    private void ApplyRenderPriority()
    {
        throw new NotImplementedException();
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

    private void RefreshProcesses()
    {
        throw new NotImplementedException();
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

    private void RefreshEnzymes()
    {
        throw new NotImplementedException();
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

        OrganelleParent = GetNode<Spatial>("OrganelleParent");

        if (IsForPreviewOnly)
        {
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

        Mass = Constants.MICROBE_BASE_MASS;

        if (IsLoadedFromSave)
        {
            if (organelles == null)
                throw new JsonException($"Loaded microbe is missing {nameof(organelles)} property");

            throw new NotImplementedException();
            /*// Fix base reproduction cost if we we were loaded from an older save
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
            }*/
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
            throw new NotImplementedException();

            // Colony?.Master.AddMovementForce(queuedMovementForce);
        }

        lastLinearVelocity = LinearVelocity;
        lastLinearAcceleration = linearAcceleration;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState physicsState)
    {
        if (ColonyParent != null)
            return;

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
            throw new NotImplementedException();

            // SetupRequiredBaseReproductionCompounds();
        }

        FinishSpeciesSetup();
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
        // Movement factor is reset here. HandleEngulfing will set the right value
        MovementFactor = 1.0f;
        queuedMovementForce = new Vector3(0, 0, 0);

        throw new NotImplementedException();

        // // Let organelles do stuff (this for example gets the movement force from flagella)
        // foreach (var organelle in organelles!.Organelles)
        // {
        //     organelle.UpdateAsync(delta);
        // }
        //
        // HandleInvulnerabilityDecay(delta);
    }

    public void ProcessSync(float delta)
    {
        if (membraneOrganellesWereUpdatedThisFrame && IsForPreviewOnly)
        {
            if (organelles == null)
                throw new InvalidOperationException("Preview microbe was not initialized with organelles list");

            // Update once for the positioning of external organelles
            foreach (var organelle in organelles.Organelles)
            {
                throw new NotImplementedException();

                // organelle.UpdateAsync(delta);
                // organelle.UpdateSync();
            }
        }

        // The code below starting from here is not needed for a display-only cell
        if (IsForPreviewOnly)
            return;

        throw new NotImplementedException();

        /*CheckEngulfShape();

        // If we didn't have our membrane ready yet in the async process we need to do these now
        if (absorptionSkippedEarly)
        {
            HandleCompoundAbsorbing(delta);
            HandleCompoundVenting(delta);
            absorptionSkippedEarly = false;
        }

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles!.Organelles)
        {
            organelle.UpdateSync();
        }

        // Rotation is applied in the physics force callback as that's the place where the body rotation
        // can be directly set without problems

        HandleChemoreceptorLines(delta);*/

        if (Hitpoints <= 0 || Dead)
        {
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

    // TODO: this is needed for a purely visual microbe handling world
    public void OverrideScaleForPreview(float scale)
    {
        if (!IsForPreviewOnly)
            throw new InvalidOperationException("Scale can only be overridden for preview microbes");

        throw new NotImplementedException();

        // ApplyScale(new Vector3(scale, scale, scale));
    }

    private void FinishSpeciesSetup()
    {
        if (CellTypeProperties.Organelles.Count < 1)
            throw new ArgumentException("Species with no organelles is not valid");

        // SetScaleFromSpecies();

        ResetOrganelleLayout();

        throw new NotImplementedException();
        // SetMembraneFromSpecies();

        if (!CanEngulf)
        {
            // Reset engulf mode if the new membrane doesn't allow it
            if (State == MicrobeState.Engulf)
                State = MicrobeState.Normal;
        }

        SetupMicrobeHitpoints();
    }
}
