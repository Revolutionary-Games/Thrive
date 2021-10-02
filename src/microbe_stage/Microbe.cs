using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game
/// </summary>
[JsonObject(IsReference = true)]
[JSONAlwaysDynamicType]
[SceneLoadedClass("res://src/microbe_stage/Microbe.tscn", UsesEarlyResolve = false)]
[DeserializedCallbackTarget]
public class Microbe : RigidBody, ISpawned, IProcessable, IMicrobeAI, ISaveLoadedTracked
{
    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    [JsonProperty]
    public readonly CompoundBag Compounds = new CompoundBag(0.0f);

    /// <summary>
    ///   The point towards which the microbe will move to point to
    /// </summary>
    public Vector3 LookAtPoint = new Vector3(0, 0, -1);

    /// <summary>
    ///   The direction the microbe wants to move. Doesn't need to be normalized
    /// </summary>
    public Vector3 MovementDirection = new Vector3(0, 0, 0);

    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    [JsonProperty]
    private CompoundCloudSystem cloudSystem;

    [JsonProperty]
    private Compound queuedToxinToEmit;

    // Child components
    private AudioStreamPlayer3D engulfAudio;
    private AudioStreamPlayer3D bindingAudio;
    private AudioStreamPlayer3D movementAudio;
    private List<AudioStreamPlayer3D> otherAudioPlayers = new List<AudioStreamPlayer3D>();
    private SphereShape engulfShape;

    /// <summary>
    ///   Init can call _Ready if it hasn't been called yet
    /// </summary>
    private bool onReadyCalled;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<PlacedOrganelle> organelles;

    /// <summary>
    ///   Contains the pili this microbe has for collision checking
    /// </summary>
    private HashSet<uint> pilusPhysicsShapes = new HashSet<uint>();

    private bool processesDirty = true;
    private List<TweakedProcess> processes;

    private bool cachedHexCountDirty = true;
    private int cachedHexCount;

    private bool membraneOrganellePositionsAreDirty = true;

    private Vector3 queuedMovementForce;

    // variables for engulfing
    [JsonProperty]
    private bool previousEngulfMode;

    [JsonProperty]
    private EntityReference<Microbe> hostileEngulfer = new EntityReference<Microbe>();

    [JsonProperty]
    private bool wasBeingEngulfed;

    /// <summary>
    ///   Tracks other Microbes that are within the engulf area and are ignoring collisions with this body.
    /// </summary>
    private HashSet<Microbe> otherMicrobesInEngulfRange = new HashSet<Microbe>();

    /// <summary>
    ///   Tracks microbes this is touching, for beginning engulfing
    /// </summary>
    private HashSet<Microbe> touchedMicrobes = new HashSet<Microbe>();

    /// <summary>
    ///   Microbes that this cell is actively trying to engulf
    /// </summary>
    private HashSet<Microbe> attemptingToEngulf = new HashSet<Microbe>();

    [JsonProperty]
    private float lastCheckedATPDamage;

    /// <summary>
    ///   The microbe stores here the sum of capacity of all the
    ///   current organelles. This is here to prevent anyone from
    ///   messing with this value if we used the Capacity from the
    ///   CompoundBag for the calculations that use this.
    /// </summary>
    private float organellesCapacity;

    [JsonProperty]
    private float escapeInterval;

    [JsonProperty]
    private bool hasEscaped;

    /// <summary>
    ///   Controls for how long the flashColour is held before going
    ///   back to species colour.
    /// </summary>
    [JsonProperty]
    private float flashDuration;

    [JsonProperty]
    private Color flashColour = new Color(0, 0, 0, 0);

    /// <summary>
    ///   This determines how important the current flashing action is. This allows higher priority flash colours to
    ///   take over.
    /// </summary>
    [JsonProperty]
    private int flashPriority;

    /// <summary>
    ///   True once all organelles are divided to not continuously run code that is triggered
    ///   when a cell is ready to reproduce.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is not saved so that the player cell can enable the editor when loading a save
    ///     where the player is ready to reproduce. If more code is added to be ran just once based
    ///     on this flag, it needs to be made sure that that code re-running after loading a save is
    ///     not a problem.
    ///   </para>
    /// </remarks>
    private bool allOrganellesDivided;

    [JsonProperty]
    private MicrobeAI ai;

    private PackedScene cellBurstEffectScene;

    [JsonProperty]
    private bool deathParticlesSpawned;

    /// <summary>
    ///   3d audio listener attached to this microbe if it is the player owned one.
    /// </summary>
    private Listener listener;

    [JsonProperty]
    private MicrobeState state;

    public enum MicrobeState
    {
        /// <summary>
        ///   Not in any special state
        /// </summary>
        Normal,

        /// <summary>
        ///   The microbe is currently in binding mode
        /// </summary>
        Binding,

        /// <summary>
        ///   The microbe is currently in unbinding mode and cannot move
        /// </summary>
        Unbinding,

        /// <summary>
        ///   The microbe is currently in engulf mode
        /// </summary>
        Engulf,
    }

    /// <summary>
    ///   The colony this microbe is currently in
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Order = 1 due to colony values requiring this to be fully initialized.
    ///   </para>
    /// </remarks>
    [JsonProperty(Order = 1)]
    public MicrobeColony Colony { get; set; }

    [JsonProperty]
    public Microbe ColonyParent { get; set; }

    [JsonProperty]
    public List<Microbe> ColonyChildren { get; set; }

    /// <summary>
    ///   The membrane of this Microbe. Used for grabbing radius / points from this.
    /// </summary>
    [JsonIgnore]
    public Membrane Membrane { get; private set; }

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

    /// <summary>
    ///   True only when this cell has been killed to let know things
    ///   being engulfed by us that we are dead.
    /// </summary>
    [JsonProperty]
    public bool Dead { get; private set; }

    [JsonProperty]
    public float Hitpoints { get; private set; } = Constants.DEFAULT_HEALTH;

    [JsonProperty]
    public float MaxHitpoints { get; private set; } = Constants.DEFAULT_HEALTH;

    [JsonIgnore]
    public bool IsHoveredOver { get; set; }

    /// <summary>
    ///   The number of agent vacuoles. Determines the time between
    ///   toxin shots.
    /// </summary>
    [JsonProperty]
    public int AgentVacuoleCount { get; private set; }

    [JsonProperty]
    public bool IsBeingEngulfed { get; private set; }

    /// <summary>
    ///   Multiplied on the movement speed of the microbe.
    /// </summary>
    [JsonProperty]
    public float MovementFactor { get; private set; } = 1.0f;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new AliveMarker();

    /// <summary>
    ///   The current state of the microbe. Shared across the colony
    /// </summary>
    [JsonIgnore]
    public MicrobeState State
    {
        get => Colony?.State ?? state;
        set
        {
            if (state == value)
                return;

            // Engulfing is not legal for microbes will cell walls
            if (value == MicrobeState.Engulf && Membrane.Type.CellWall)
            {
                GD.PrintErr("Illegal Action: microbe attempting to engulf with a membrane that does not allow it!");
                return;
            }

            state = value;
            if (Colony != null)
                Colony.State = value;

            if (value == MicrobeState.Unbinding && IsPlayerMicrobe)
                OnUnbindEnabled?.Invoke(this);
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

    /// <summary>
    ///   The size this microbe is for engulfing calculations
    /// </summary>
    [JsonIgnore]
    public float EngulfSize
    {
        get
        {
            if (Species.IsBacteria)
            {
                return HexCount * 0.5f;
            }

            return HexCount;
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

    /// <summary>
    ///   Returns true when this microbe can enable binding mode
    /// </summary>
    public bool CanBind => organelles.Any(p => p.IsBindingAgent) || Colony != null;

    /// <summary>
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleParent { get; private set; }

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

    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => Compounds;

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

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds. Also used by player cell for tutorials
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> TotalAbsorbedCompounds { get; set; } = new Dictionary<Compound, float>();

    [JsonProperty]
    public float AgentEmissionCooldown { get; private set; }

    /// <summary>
    ///   Called when this Microbe dies
    /// </summary>
    [JsonProperty]
    public Action<Microbe> OnDeath { get; set; }

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    [JsonProperty]
    public Action<Microbe, bool> OnReproductionStatus { get; set; }

    [JsonProperty]
    public Action<Microbe> OnUnbindEnabled { get; set; }

    [JsonProperty]
    public Action<Microbe> OnUnbound { get; set; }

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
                    AddChild(child);
            }

            // Need to re-attach our organelles
            foreach (var organelle in organelles)
                OrganelleParent.AddChild(organelle);

            // Colony children shapes need re-parenting to their master
            // The shapes have to be re-parented to their original microbe then to the master again
            // maybe engine bug
            if (Colony != null && this != Colony.Master)
            {
                ReParentShapes(this, Vector3.Zero, ColonyParent.Rotation, Rotation);
                ReParentShapes(Colony.Master, GetOffsetRelativeToMaster(), ColonyParent.Rotation, Rotation);
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
    ///   Resets the organelles in this microbe to match the species definition
    /// </summary>
    public void ResetOrganelleLayout()
    {
        // TODO: It would be much better if only organelles that need
        // to be removed where removed, instead of everything.
        // When doing that all organelles will need to be re-added anyway if this turned from a prokaryote to eukaryote

        if (organelles == null)
        {
            organelles = new OrganelleLayout<PlacedOrganelle>(OnOrganelleAdded,
                OnOrganelleRemoved);
        }
        else
        {
            // Just clear the existing ones
            organelles.Clear();
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

        // Reproduction progress is lost
        allOrganellesDivided = false;

        // Unbind if a colony's master cell removed its binding agent.
        if (Colony != null && Colony.Master == this && !organelles.Any(p => p.IsBindingAgent))
            Colony.RemoveFromColony(this);
    }

    /// <summary>
    ///   Applies the set species' color to all of this microbe's organelles
    /// </summary>
    public void ApplyPreviewOrganelleColours()
    {
        if (!IsForPreviewOnly)
            throw new InvalidOperationException("Microbe must be a preview-only type");

        foreach (var entry in organelles.Organelles)
        {
            entry.Colour = Species.Colour;
            entry.Update(0);
        }
    }

    /// <summary>
    ///   Updates the intensity of wigglyness of this cell's membrane based on membrane type, taking
    ///   membrane rigidity into account.
    /// </summary>
    public void ApplyMembraneWigglyness()
    {
        Membrane.WigglyNess = Membrane.Type.BaseWigglyness - (Species.MembraneRigidity /
            Membrane.Type.BaseWigglyness) * 0.2f;
        Membrane.MovementWigglyNess = Membrane.Type.MovementWigglyness - (Species.MembraneRigidity /
            Membrane.Type.MovementWigglyness) * 0.2f;
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
        if (touchedOwnerId == 0)
            return null;

        return GetColonyMemberWithShapeOwner(touchedOwnerId, Colony);
    }

    /// <summary>
    ///   Tries to fire a toxin if possible
    /// </summary>
    public void EmitToxin(Compound agentType = null)
    {
        if (AgentEmissionCooldown > 0)
            return;

        // Only shoot if you have an agent vacuole.
        if (AgentVacuoleCount < 1)
            return;

        agentType ??= SimulationParameters.Instance.GetCompound("oxytoxy");

        float amountAvailable = Compounds.GetCompoundAmount(agentType);

        if (amountAvailable < Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
            return;

        // The cooldown time is inversely proportional to the amount of agent vacuoles.
        AgentEmissionCooldown = Constants.AGENT_EMISSION_COOLDOWN / AgentVacuoleCount;

        Compounds.TakeCompound(agentType, Constants.MINIMUM_AGENT_EMISSION_AMOUNT);

        float ejectionDistance = Membrane.EncompassingCircleRadius +
            Constants.AGENT_EMISSION_DISTANCE_OFFSET;

        if (Species.IsBacteria)
            ejectionDistance *= 0.5f;

        var props = new AgentProperties();
        props.Compound = agentType;
        props.Species = Species;

        // Find the direction the microbe is facing
        var direction = (LookAtPoint - Translation).Normalized();

        var position = Translation + (direction * ejectionDistance);

        SpawnHelpers.SpawnAgent(props, 10.0f, Constants.EMITTED_AGENT_LIFETIME,
            position, direction, GetStageAsParent(),
            SpawnHelpers.LoadAgentScene(), this);

        PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
    }

    /// <summary>
    ///   Makes this Microbe fire a toxin on the next update. Used by the AI from a background thread.
    ///   Only one can be queued at once
    /// </summary>
    /// <param name="toxinCompound">The toxin type to emit</param>
    public void QueueEmitToxin(Compound toxinCompound)
    {
        queuedToxinToEmit = toxinCompound;
    }

    /// <summary>
    ///   Flashes the membrane a specific colour for duration. A new
    ///   flash is not started if currently flashing and priority is lower than the current flash priority.
    /// </summary>
    /// <returns>True when a new flash was started, false if already flashing</returns>
    public bool Flash(float duration, Color colour, int priority = 0)
    {
        if (colour != flashColour && (priority > flashPriority || flashDuration <= 0))
        {
            AbortFlash();
        }
        else if (flashDuration > 0)
        {
            return false;
        }

        flashDuration = duration;
        flashColour = colour;
        flashPriority = priority;
        return true;
    }

    public void AbortFlash()
    {
        flashDuration = 0;
        flashColour = new Color(0, 0, 0, 0);
        flashPriority = 0;
        Membrane.Tint = Species.Colour;
    }

    /// <summary>
    ///   Applies damage to this cell. killing it if its hitpoints drop low enough
    /// </summary>
    public void Damage(float amount, string source)
    {
        if (IsPlayerMicrobe && CheatManager.GodMode)
            return;

        if (amount == 0 || Dead)
            return;

        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("damage type is empty");

        // This seems to be triggered sometimes, even though our logic for damage seems right everywhere.
        // One possible explanation is that delta is negative sometimes? So we just print an error and do nothing
        // else here
        if (amount < 0)
        {
            GD.PrintErr("Trying to deal negative damage");
            return;
        }

        if (source == "toxin" || source == "oxytoxy")
        {
            // TODO: Replace this take damage sound with a more appropriate one.

            // Play the toxin sound
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");

            // Divide damage by toxin resistance
            amount /= Species.MembraneType.ToxinResistance;
        }
        else if (source == "pilus")
        {
            // Play the pilus sound
            PlaySoundEffect("res://assets/sounds/soundeffects/pilus_puncture_stab.ogg");

            // TODO: this may get triggered a lot more than the toxin
            // so this might need to be rate limited or something
            // Divide damage by physical resistance
            amount /= Species.MembraneType.PhysicalResistance;
        }
        else if (source == "chunk")
        {
            // TODO: Replace this take damage sound with a more appropriate one.

            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");

            // Divide damage by physical resistance
            amount /= Species.MembraneType.PhysicalResistance;
        }
        else if (source == "atpDamage")
        {
            // TODO: Replace this take damage sound with a more appropriate one.

            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
        }

        Hitpoints -= amount;

        // Flash the microbe red
        Flash(1.0f, new Color(1, 0, 0, 0.5f), 1);

        // Kill if ran out of health
        if (Hitpoints <= 0.0f)
        {
            Hitpoints = 0.0f;
            Kill();
        }
    }

    /// <summary>
    ///   Returns true when this microbe can engulf the target
    /// </summary>
    public bool CanEngulf(Microbe target)
    {
        // Disallow cannibalism
        if (target.Species == Species)
            return false;

        // Membranes with Cell Wall cannot engulf
        if (Membrane.Type.CellWall)
            return false;

        // Needs to be big enough to engulf
        return EngulfSize >= target.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ;
    }

    /// <summary>
    ///   Called from movement organelles to add movement force
    /// </summary>
    public void AddMovementForce(Vector3 force)
    {
        queuedMovementForce += force;
    }

    /// <summary>
    ///   Report that a pilus shape was added to this microbe. Called by PilusComponent
    /// </summary>
    public bool AddPilus(uint shapeOwner)
    {
        return pilusPhysicsShapes.Add(shapeOwner);
    }

    public bool RemovePilus(uint shapeOwner)
    {
        return pilusPhysicsShapes.Remove(shapeOwner);
    }

    public bool IsPilus(uint shape)
    {
        return pilusPhysicsShapes.Contains(shape);
    }

    /// <summary>
    ///   Instantly kills this microbe and queues this entity to be destroyed
    /// </summary>
    public void Kill()
    {
        if (Dead)
            return;

        Dead = true;

        OnDeath?.Invoke(this);
        OnDestroyed();

        // Reset some stuff
        State = MicrobeState.Normal;
        MovementDirection = new Vector3(0, 0, 0);
        LinearVelocity = new Vector3(0, 0, 0);
        allOrganellesDivided = false;

        var random = new Random();

        // Releasing all the agents.
        // To not completely deadlock in this there is a maximum limit
        int createdAgents = 0;

        if (AgentVacuoleCount > 0)
        {
            var oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

            var amount = Compounds.GetCompoundAmount(oxytoxy);

            var props = new AgentProperties();
            props.Compound = oxytoxy;
            props.Species = Species;

            var agentScene = SpawnHelpers.LoadAgentScene();

            while (amount > Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
            {
                var direction = new Vector3(random.Next(0.0f, 1.0f) * 2 - 1,
                    0, random.Next(0.0f, 1.0f) * 2 - 1);

                SpawnHelpers.SpawnAgent(props, 10.0f, Constants.EMITTED_AGENT_LIFETIME,
                    Translation, direction, GetStageAsParent(),
                    agentScene, this);

                amount -= Constants.MINIMUM_AGENT_EMISSION_AMOUNT;
                ++createdAgents;

                if (createdAgents >= Constants.MAX_EMITTED_AGENTS_ON_DEATH)
                    break;
            }
        }

        // Eject the compounds that was in the microbe
        var compoundsToRelease = new Dictionary<Compound, float>();

        foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
        {
            var amount = Compounds.GetCompoundAmount(type) *
                Constants.COMPOUND_RELEASE_PERCENTAGE;

            compoundsToRelease[type] = amount;
        }

        // Eject some part of the build cost of all the organelles
        foreach (var organelle in organelles)
        {
            foreach (var entry in organelle.Definition.InitialComposition)
            {
                float existing = 0;

                if (compoundsToRelease.ContainsKey(entry.Key))
                    existing = compoundsToRelease[entry.Key];

                compoundsToRelease[entry.Key] = existing + (entry.Value *
                    Constants.COMPOUND_MAKEUP_RELEASE_PERCENTAGE);
            }
        }

        int chunksToSpawn = Math.Max(1, HexCount / Constants.CORPSE_CHUNK_DIVISOR);

        var chunkScene = SpawnHelpers.LoadChunkScene();

        for (int i = 0; i < chunksToSpawn; ++i)
        {
            // Amount of compound in one chunk
            float amount = HexCount / Constants.CORPSE_CHUNK_AMOUNT_DIVISOR;

            var positionAdded = new Vector3(random.Next(-2.0f, 2.0f), 0,
                random.Next(-2.0f, 2.0f));

            var chunkType = new ChunkConfiguration
            {
                ChunkScale = 1.0f,
                Dissolves = true,
                Mass = 1.0f,
                Radius = 1.0f,
                Size = 3.0f,
                VentAmount = 0.1f,

                // Add compounds
                Compounds = new Dictionary<Compound, ChunkConfiguration.ChunkCompound>(),
            };

            // They were added in order already so looping through this other thing is fine
            foreach (var entry in compoundsToRelease)
            {
                var compoundValue = new ChunkConfiguration.ChunkCompound
                {
                    // Randomize compound amount a bit so things "rot away"
                    Amount = (entry.Value / random.Next(amount / 3.0f, amount)) *
                        Constants.CORPSE_COMPOUND_COMPENSATION,
                };

                chunkType.Compounds[entry.Key] = compoundValue;
            }

            chunkType.Meshes = new List<ChunkConfiguration.ChunkScene>();

            var sceneToUse = new ChunkConfiguration.ChunkScene();

            // Try all organelles in random order and use the first one with a scene for model
            foreach (var organelle in organelles.OrderBy(_ => random.Next()))
            {
                if (!string.IsNullOrEmpty(organelle.Definition.CorpseChunkScene))
                {
                    sceneToUse.LoadedScene = organelle.Definition.LoadedCorpseChunkScene;
                    break;
                }

                if (!string.IsNullOrEmpty(organelle.Definition.DisplayScene))
                {
                    sceneToUse.LoadedScene = organelle.Definition.LoadedScene;
                    sceneToUse.SceneModelPath = organelle.Definition.DisplaySceneModelPath;
                    break;
                }
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable once HeuristicUnreachableCode
            if (sceneToUse == null)
                throw new Exception("sceneToUse is null");

            chunkType.Meshes.Add(sceneToUse);

            // Finally spawn a chunk with the settings
            SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetStageAsParent(),
                chunkScene, cloudSystem, random);
        }

        // Subtract population
        if (!IsPlayerMicrobe && !Species.PlayerSpecies)
        {
            GameWorld.AlterSpeciesPopulation(Species,
                Constants.CREATURE_DEATH_POPULATION_LOSS, TranslationServer.Translate("DEATH"));
        }

        if (IsPlayerMicrobe)
        {
            // If you died before entering the editor disable that
            OnReproductionStatus?.Invoke(this, false);
        }

        PlaySoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg");

        // Disable collisions
        CollisionLayer = 0;
        CollisionMask = 0;

        // Some pre-death actions are going to be run now
    }

    public void PlaySoundEffect(string effect)
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
            player.UnitDb = 50.0f;
            player.MaxDistance = 100.0f;
            player.Bus = "SFX";

            AddChild(player);
            otherAudioPlayers.Add(player);
        }

        player.Stream = sound;
        player.Play();
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
    /// <exception cref="NotSupportedException">Thrown when this microbe is in a colony</exception>
    public void Divide()
    {
        if (Colony != null)
            throw new NotSupportedException("Cannot divide a microbe while in a colony");

        ForceDivide();
    }

    /// <summary>
    ///   Triggers reproduction on this cell (even if not ready)
    ///   Ignores security checks. If you want those checks, use <see cref="Divide"/>
    /// </summary>
    public void ForceDivide()
    {
        // Separate the two cells.
        var separation = new Vector3(Radius, 0, 0);

        // Create the one daughter cell.
        var copyEntity = SpawnHelpers.SpawnMicrobe(Species, Translation + separation,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem, CurrentGame);

        // Make it despawn like normal
        SpawnSystem.AddEntityToTrack(copyEntity);

        // Remove the compounds from the created cell
        copyEntity.Compounds.ClearCompounds();

        var keys = new List<Compound>(Compounds.Compounds.Keys);
        var reproductionCompounds = copyEntity.CalculateTotalCompounds();

        // Split the compounds between the two cells.
        foreach (var compound in keys)
        {
            var amount = Compounds.GetCompoundAmount(compound);

            if (amount <= 0)
                continue;

            // If the compound is for reproduction we give player and NPC microbes different amounts.
            if (reproductionCompounds.TryGetValue(compound, out float divideAmount))
            {
                // The amount taken away from the parent cell depends on if it is a player or NPC. Player
                // cells always have 50% of the compounds they divided with taken away.
                float amountToTake = amount * 0.5f;

                if (!IsPlayerMicrobe)
                {
                    // NPC parent cells have at least 50% taken away, or more if it would leave them
                    // with more than 90% of the compound it would take to immediately divide again.
                    amountToTake = Math.Max(amountToTake, amount - (divideAmount * 0.9f));
                }

                Compounds.TakeCompound(compound, amountToTake);

                // Since the child cell is always an NPC they are given either 50% of the compound from the
                // parent, or 90% of the amount required to immediately divide again, whichever is smaller.
                float amountToGive = Math.Min(amount * 0.5f, divideAmount * 0.9f);
                var addedCompound = copyEntity.Compounds.AddCompound(compound, amountToGive);

                if (addedCompound < amountToGive)
                {
                    // TODO: handle the excess compound that didn't fit in the other cell
                }
            }
            else
            {
                // Non-reproductive compounds just always get split evenly to both cells.
                Compounds.TakeCompound(compound, amount * 0.5f);

                var amountAdded = copyEntity.Compounds.AddCompound(compound, amount * 0.5f);

                if (amountAdded < amount)
                {
                    // TODO: handle the excess compound that didn't fit in the other cell
                }
            }
        }

        // Play the split sound
        PlaySoundEffect("res://assets/sounds/soundeffects/reproduction.ogg");
    }

    /// <summary>
    ///   Throws some compound out of this Microbe, up to maxAmount
    /// </summary>
    public float EjectCompound(Compound compound, float maxAmount)
    {
        float amount = Compounds.TakeCompound(compound, maxAmount);

        SpawnEjectedCompound(compound, amount);
        return amount;
    }

    /// <summary>
    ///   Calculates the reproduction progress for a cell, used to
    ///   show how close the player is getting to the editor.
    /// </summary>
    public float CalculateReproductionProgress(out Dictionary<Compound, float> gatheredCompounds,
        out Dictionary<Compound, float> totalCompounds)
    {
        // Calculate total compounds needed to split all organelles
        totalCompounds = CalculateTotalCompounds();

        // Calculate how many compounds the cell already has absorbed to grow
        gatheredCompounds = CalculateAlreadyAbsorbedCompounds();

        // Add the currently held compounds
        var keys = new List<Compound>(gatheredCompounds.Keys);

        foreach (var key in keys)
        {
            float value = Math.Max(0.0f, Compounds.GetCompoundAmount(key) -
                Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST);

            if (value > 0)
            {
                float existing = gatheredCompounds[key];

                // Only up to the total needed
                float total = totalCompounds[key];

                gatheredCompounds[key] = Math.Min(total, existing + value);
            }
        }

        float totalFraction = 0;

        foreach (var entry in totalCompounds)
        {
            float gathered = 0;

            if (gatheredCompounds.ContainsKey(entry.Key))
                gathered = gatheredCompounds[entry.Key];

            totalFraction += gathered / entry.Value;
        }

        return totalFraction / totalCompounds.Count;
    }

    /// <summary>
    ///   Calculates total compounds needed for a cell to reproduce,
    /// used by calculateReproductionProgress to calculate the
    /// fraction done.  </summary>
    public Dictionary<Compound, float> CalculateTotalCompounds()
    {
        var result = new Dictionary<Compound, float>();

        foreach (var organelle in organelles)
        {
            if (organelle.IsDuplicate)
                continue;

            result.Merge(organelle.Definition.InitialComposition);
        }

        return result;
    }

    /// <summary>
    ///   Calculates how much compounds organelles have already absorbed
    /// </summary>
    public Dictionary<Compound, float> CalculateAlreadyAbsorbedCompounds()
    {
        var result = new Dictionary<Compound, float>();

        foreach (var organelle in organelles)
        {
            if (organelle.IsDuplicate)
                continue;

            if (organelle.WasSplit)
            {
                // Organelles are reset on split, so we use the full
                // cost as the gathered amount
                result.Merge(organelle.Definition.InitialComposition);
                continue;
            }

            organelle.CalculateAbsorbedCompounds(result);
        }

        return result;
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

    public void OnDestroyed()
    {
        Colony?.RemoveFromColony(this);

        AliveMarker.Alive = false;
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
        // TODO: should movement also be applied here?

        physicsState.Transform = GetNewPhysicsRotation(physicsState.Transform);
    }

    /// <summary>
    ///   Removes this cell and child cells from the colony.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If this is the colony master, this disbands the whole colony
    ///   </para>
    /// </remarks>
    public void UnbindAll()
    {
        if (State == MicrobeState.Unbinding || State == MicrobeState.Binding)
            State = MicrobeState.Normal;

        // TODO: once the colony leader can leave without the entire colony disbanding this perhaps should keep the
        // disband entire colony functionality
        Colony?.RemoveFromColony(this);
    }

    internal void OnColonyMemberRemoved(Microbe microbe)
    {
        if (microbe == this)
        {
            OnUnbound?.Invoke(this);

            RevertNodeParent();
            ai?.ResetAI();

            Mode = ModeEnum.Rigid;

            return;
        }

        if (hostileEngulfer != microbe)
            microbe.RemoveCollisionExceptionWith(this);
        if (microbe.hostileEngulfer != this)
            RemoveCollisionExceptionWith(microbe);
    }

    internal void ReParentShapes(Microbe to, Vector3 offset, Vector3 masterRotation, Vector3 microbeRotation)
    {
        // TODO: if microbeRotation is the rotation of *this* instance we should use the variable here directly
        // An object doesn't need to be told its own member variable in a method...
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        foreach (var organelle in organelles)
            organelle.ReParentShapes(to, offset, masterRotation, microbeRotation);
    }

    internal void OnColonyMemberAdded(Microbe microbe)
    {
        if (microbe == this)
        {
            OnIGotAddedToColony();

            var parent = this;
            if (Colony.Master != this)
            {
                Mode = ModeEnum.Static;
                parent = ColonyParent;
            }

            ReParentShapes(Colony.Master, GetOffsetRelativeToMaster(), parent.Rotation, Rotation);
        }
        else
        {
            AddCollisionExceptionWith(microbe);
            microbe.AddCollisionExceptionWith(this);
        }
    }

    internal void SuccessfulScavenge()
    {
        GameWorld.AlterSpeciesPopulation(Species,
            Constants.CREATURE_SCAVENGE_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_SCAVENGE"));
    }

    internal void SuccessfulKill()
    {
        GameWorld.AlterSpeciesPopulation(Species,
            Constants.CREATURE_KILL_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_KILL"));
    }

    private Microbe GetColonyMemberWithShapeOwner(uint ownerID, MicrobeColony colony)
    {
        foreach (var microbe in colony.ColonyMembers)
        {
            if (microbe.organelles.Any(o => o.HasShape(ownerID)) || microbe.IsPilus(ownerID))
                return microbe;
        }

        // TODO: I really hope there is no way to hit this. I would really hate to reduce the game stability due to
        // possibly bogus ownerID values that sometimes seem to come from Godot
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        throw new InvalidOperationException();
    }

    private Vector3 GetOffsetRelativeToMaster()
    {
        return (GlobalTransform.origin - Colony.Master.GlobalTransform.origin).Rotated(Vector3.Down,
            Colony.Master.Rotation.y);
    }

    private void OnIGotAddedToColony()
    {
        State = MicrobeState.Normal;
        UnreadyToReproduce();

        if (ColonyParent == null)
            return;

        var newTransform = GetNewRelativeTransform();

        Rotation = newTransform.rotation;
        Translation = newTransform.translation;

        ChangeNodeParent(ColonyParent);
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
    private (Vector3 translation, Vector3 rotation) GetNewRelativeTransform()
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

    private void SetMembraneFromSpecies()
    {
        Membrane.Type = Species.MembraneType;
        Membrane.Tint = Species.Colour;
        Membrane.Dirty = true;
        ApplyMembraneWigglyness();
    }

    private void HandleCompoundAbsorbing(float delta)
    {
        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem.AbsorbCompounds(GlobalTransform.origin, grabRadius, Compounds,
            TotalAbsorbedCompounds, delta, Membrane.Type.ResourceAbsorptionFactor);

        if (IsPlayerMicrobe && CheatManager.InfiniteCompounds)
        {
            var usefulCompounds = SimulationParameters.Instance.GetCloudCompounds().Where(Compounds.IsUseful);
            foreach (var usefulCompound in usefulCompounds)
            {
                Compounds.AddCompound(usefulCompound,
                    Compounds.BagCapacity - Compounds.GetCompoundAmount(usefulCompound));
            }
        }
    }

    private void CheckEngulfShapeSize()
    {
        var wanted = Radius;
        if (engulfShape.Radius != wanted)
            engulfShape.Radius = wanted;
    }

    /// <summary>
    ///   Vents (throws out) non-useful compounds from this cell
    /// </summary>
    private void HandleCompoundVenting(float delta)
    {
        // TODO: check that this works
        // Skip if process system has not run yet
        if (!Compounds.HasAnyBeenSetUseful())
            return;

        float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta;

        // Cloud types are ones that can be vented
        foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
        {
            // Vent if not useful, or if over float the capacity
            if (!Compounds.IsUseful(type))
            {
                amountToVent -= EjectCompound(type, amountToVent);
            }
            else if (Compounds.GetCompoundAmount(type) > 2 * Compounds.BagCapacity)
            {
                // Vent the part that went over
                float toVent = Compounds.GetCompoundAmount(type) - (2 * Compounds.BagCapacity);

                amountToVent -= EjectCompound(type, Math.Min(toVent, amountToVent));
            }

            if (amountToVent <= 0)
                break;
        }
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
            if (flashDuration % 0.6f < 0.3f)
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
        if (Hitpoints < MaxHitpoints)
        {
            if (Compounds.GetCompoundAmount(atp) >= 1.0f)
            {
                Hitpoints += Constants.REGENERATION_RATE * delta;
                if (Hitpoints > MaxHitpoints)
                {
                    Hitpoints = MaxHitpoints;
                }
            }
        }
    }

    /// <summary>
    ///   Sets up the hitpoints of this microbe based on the Species membrane
    /// </summary>
    private void SetupMicrobeHitpoints()
    {
        float currentHealth = Hitpoints / MaxHitpoints;

        MaxHitpoints = Species.MembraneType.Hitpoints +
            (Species.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);

        Hitpoints = MaxHitpoints * currentHealth;
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
#pragma warning disable CA1801 // TODO: implement handling delta
    private void HandleReproduction(float delta)
    {
#pragma warning restore CA1801

        // Dead cells can't reproduce
        if (Dead)
            return;

        if (allOrganellesDivided)
        {
            // Ready to reproduce already. Only the player gets here
            // as other cells split and reset automatically
            return;
        }

        if (Colony != null)
            return;

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

    private void OnPlayerDuplicationCheat(object sender, EventArgs e)
    {
        allOrganellesDivided = true;

        ForceDivide();
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
            var radiusOffset = Hex.HexNeighbourOffset[Hex.HexSide.BottomLeft];
            q = q + radiusOffset.Q;
            r = r + radiusOffset.R;

            // Iterates in the ring
            for (int side = 1; side <= 6; ++side)
            {
                var offset = Hex.HexNeighbourOffset[(Hex.HexSide)side];

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

            OnReproductionStatus?.Invoke(this, Colony == null);
        }
        else
        {
            // Return the first cell to its normal, non duplicated cell arrangement.
            if (!Species.PlayerSpecies)
            {
                GameWorld.AlterSpeciesPopulation(Species,
                    Constants.CREATURE_REPRODUCE_POPULATION_GAIN, TranslationServer.Translate("REPRODUCED"));
            }

            ResetOrganelleLayout();

            Divide();
        }
    }

    /// <summary>
    ///   Removes the player's ability to go to the editor.
    ///   Does nothing when called by the AI.
    /// </summary>
    private void UnreadyToReproduce()
    {
        // Sets this flag to false to make full recomputation on next reproduction readiness check
        // This notably allows to reactivate editor button upon colony unbinding.
        allOrganellesDivided = false;
        OnReproductionStatus?.Invoke(this, false);
    }

    private Node GetStageAsParent()
    {
        if (Colony == null)
            return GetParent();

        return Colony.Master.GetParent();
    }

    /// <summary>
    ///   Handles things related to binding
    /// </summary>
    private void HandleBinding(float delta)
    {
        if (State != MicrobeState.Binding)
        {
            if (bindingAudio.Playing)
                bindingAudio.Stop();
            return;
        }

        // Drain atp
        var cost = Constants.BINDING_ATP_COST_PER_SECOND * delta;

        if (Compounds.TakeCompound(atp, cost) < cost - 0.001f)
        {
            State = MicrobeState.Normal;
        }

        if (!bindingAudio.Playing)
            bindingAudio.Play();

        Flash(1, new Color(0.2f, 0.5f, 0.0f, 0.5f));
    }

    /// <summary>
    ///   Handles things related to unbinding
    /// </summary>
    private void HandleUnbinding()
    {
        if (State != MicrobeState.Unbinding)
            return;

        if (IsHoveredOver)
        {
            Flash(1, new Color(1.0f, 0.0f, 0.0f, 0.5f));
        }
        else
        {
            Flash(1, new Color(1.0f, 0.5f, 0.2f, 0.5f));
        }
    }

    /// <summary>
    ///   Handles things related to engulfing. Works together with the physics callbacks
    /// </summary>
    private void HandleEngulfing(float delta)
    {
        if (State == MicrobeState.Engulf)
        {
            // Drain atp
            var cost = Constants.ENGULFING_ATP_COST_PER_SECOND * delta;

            if (Compounds.TakeCompound(atp, cost) < cost - 0.001f)
            {
                State = MicrobeState.Normal;
            }
        }

        ProcessPhysicsForEngulfing();

        // Play sound
        if (State == MicrobeState.Engulf)
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
        if (State == MicrobeState.Engulf)
        {
            MovementFactor /= Constants.ENGULFING_MOVEMENT_DIVISION;
        }

        if (IsBeingEngulfed)
        {
            MovementFactor /= Constants.ENGULFED_MOVEMENT_DIVISION;

            Damage(Constants.ENGULF_DAMAGE * delta, "isBeingEngulfed");
            wasBeingEngulfed = true;
        }
        else if (wasBeingEngulfed && !IsBeingEngulfed)
        {
            // Else If we were but are no longer, being engulfed
            wasBeingEngulfed = false;

            if (!IsPlayerMicrobe && !Species.PlayerSpecies)
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

                GameWorld.AlterSpeciesPopulation(Species,
                    Constants.CREATURE_ESCAPE_POPULATION_GAIN,
                    TranslationServer.Translate("ESCAPE_ENGULFING"));
            }
        }

        // Check whether we should not be being engulfed anymore
        var hostile = hostileEngulfer.Value;
        if (hostile != null)
        {
            // Dead things can't engulf us
            if (hostile.Dead)
            {
                hostileEngulfer.Value = null;
                IsBeingEngulfed = false;
            }
        }
        else
        {
            IsBeingEngulfed = false;
        }

        previousEngulfMode = State == MicrobeState.Engulf;
    }

    private void ProcessPhysicsForEngulfing()
    {
        if (State != MicrobeState.Engulf)
        {
            // Reset the engulfing ignores and potential targets
            foreach (var body in attemptingToEngulf)
            {
                StopEngulfingOnTarget(body);
            }

            attemptingToEngulf.Clear();

            return;
        }

        // Check for starting engulfing on things we already touched
        if (!previousEngulfMode)
            CheckStartEngulfingOnCandidates();

        // Apply engulf effect (which will cause damage in their process call) to the cells we are engulfing
        foreach (var microbe in attemptingToEngulf)
        {
            microbe.hostileEngulfer.Value = this;
            microbe.IsBeingEngulfed = true;
        }
    }

    private void RemoveEngulfedEffect()
    {
        // This kept getting doubled for some reason, so i just set it to default
        MovementFactor = 1.0f;
        wasBeingEngulfed = false;
        IsBeingEngulfed = false;

        if (hostileEngulfer.Value != null)
        {
            // Currently unused
            // hostileEngulfer.isCurrentlyEngulfing = false;
        }

        hostileEngulfer.Value = null;
    }

    private void HandleOsmoregulation(float delta)
    {
        var osmoregulationCost = (HexCount * Species.MembraneType.OsmoregulationFactor *
            Constants.ATP_COST_FOR_OSMOREGULATION) * delta;

        Compounds.TakeCompound(atp, osmoregulationCost);
    }

    /// <summary>
    ///   Damage the microbe if its too low on ATP.
    /// </summary>
    private void ApplyATPDamage()
    {
        if (Compounds.GetCompoundAmount(atp) <= 0.0f)
        {
            // TODO: put this on a GUI notification.
            // if(microbeComponent.isPlayerMicrobe and not this.playerAlreadyShownAtpDamage){
            //     this.playerAlreadyShownAtpDamage = true
            //     showMessage("No ATP hurts you!")
            // }

            Damage(MaxHitpoints * Constants.NO_ATP_DAMAGE_FRACTION, "atpDamage");
        }
    }

    /// <summary>
    ///   Handles the death of this microbe. This queues this object
    ///   for deletion and handles some pre-death actions.
    /// </summary>
    private void HandleDeath(float delta)
    {
        // Spawn cell death particles
        if (!deathParticlesSpawned)
        {
            deathParticlesSpawned = true;

            var cellBurstEffectParticles = (CellBurstEffect)cellBurstEffectScene.Instance();
            cellBurstEffectParticles.Translation = Translation;
            cellBurstEffectParticles.Radius = Radius;
            cellBurstEffectParticles.AddToGroup(Constants.TIMED_GROUP);

            GetParent().AddChild(cellBurstEffectParticles);

            // Hide the particles if being engulfed since they are
            // supposed to be already "absorbed" by the engulfing cell
            if (IsBeingEngulfed)
            {
                cellBurstEffectParticles.Hide();
            }
        }

        foreach (var organelle in organelles)
        {
            organelle.Hide();
        }

        Membrane.DissolveEffectValue += delta * Constants.MEMBRANE_DISSOLVE_SPEED;

        if (Membrane.DissolveEffectValue >= 1)
        {
            this.DestroyDetachAndQueueFree();
        }
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

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(PlacedOrganelle organelle)
    {
        organelle.OnAddedToMicrobe(this);
        processesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;

        if (organelle.IsAgentVacuole)
            AgentVacuoleCount += 1;

        // This is calculated here as it would be a bit difficult to
        // hook up computing this when the StorageBag needs this info.
        organellesCapacity += organelle.StorageCapacity;
        Compounds.BagCapacity = organellesCapacity;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(PlacedOrganelle organelle)
    {
        organellesCapacity -= organelle.StorageCapacity;
        if (organelle.IsAgentVacuole)
            AgentVacuoleCount -= 1;
        organelle.OnRemovedFromMicrobe();

        // The organelle only detaches but doesn't delete itself, so we delete it here
        organelle.DetachAndQueueFree();

        processesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;

        Compounds.BagCapacity = organellesCapacity;
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
        membraneOrganellePositionsAreDirty = false;
    }

    /// <summary>
    ///   Recomputes storage from organelles, used after loading a save
    /// </summary>
    private void RecomputeOrganelleCapacity()
    {
        organellesCapacity = organelles.Sum(o => o.StorageCapacity);
        Compounds.BagCapacity = organellesCapacity;
    }

    /// <summary>
    ///   Ejects compounds from the microbes behind position, into the environment
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that the compounds ejected are created in this world
    ///     and not taken from the microbe. This is purely for adding
    ///     the compound to the cloud system at the right position.
    ///   </para>
    /// </remarks>
    private void SpawnEjectedCompound(Compound compound, float amount)
    {
        var amountToEject = amount * Constants.MICROBE_VENT_COMPOUND_MULTIPLIER;

        if (amountToEject <= 0)
            return;

        cloudSystem.AddCloud(compound, amountToEject, CalculateNearbyWorldPosition());
    }

    /// <summary>
    ///   Calculates a world pos for emitting compounds
    /// </summary>
    private Vector3 CalculateNearbyWorldPosition()
    {
        // The back of the microbe
        var exit = Hex.AxialToCartesian(new Hex(0, 1));
        var membraneCoords = Membrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

        // Get the distance to eject the compounds
        var ejectionDistance = Membrane.EncompassingCircleRadius;

        // The membrane radius doesn't take being bacteria into account
        if (Species.IsBacteria)
            ejectionDistance *= 0.5f;

        float angle = 180;

        // Find the direction the microbe is facing
        var yAxis = Transform.basis.y;
        var microbeAngle = Mathf.Atan2(yAxis.x, yAxis.y);
        if (microbeAngle < 0)
        {
            microbeAngle += 2 * Mathf.Pi;
        }

        microbeAngle = microbeAngle * 180 / Mathf.Pi;

        // Take the microbe angle into account so we get world relative degrees
        var finalAngle = (angle + microbeAngle) % 360;

        var s = Mathf.Sin(finalAngle / 180 * Mathf.Pi);
        var c = Mathf.Cos(finalAngle / 180 * Mathf.Pi);

        var ejectionDirection = new Vector3(-membraneCoords.x * c + membraneCoords.z * s, 0,
            membraneCoords.x * s + membraneCoords.z * c);

        return Translation + (ejectionDirection * ejectionDistance);
    }

    private void ChangeNodeParent(Microbe parent)
    {
        // We unset Colony temporarily as otherwise our exit tree callback would remove us from the colony immediately
        // TODO: it would be perhaps a nicer code approach to only set the Colony after this is re-parented
        var savedColony = Colony;
        Colony = null;

        this.ReParent(parent);

        // And restore the colony after completing the re-parenting of this node
        Colony = savedColony;
    }

    private void RevertNodeParent()
    {
        var pos = GlobalTransform;

        if (Colony.Master != this)
        {
            var newParent = GetStageAsParent();

            // See the comment in ChangeNodeParent
            var savedColony = Colony;
            Colony = null;

            this.ReParent(newParent);

            Colony = savedColony;
        }

        GlobalTransform = pos;
    }

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;

        if (body is Microbe colonyLeader)
        {
            var touchedOwnerId = colonyLeader.ShapeFindOwner(bodyShape);
            var thisOwnerId = ShapeFindOwner(localShape);

            var touchedMicrobe = colonyLeader.GetMicrobeFromShape(bodyShape);

            var thisMicrobe = GetMicrobeFromShape(localShape);

            // bodyShape or localShape are invalid. This can happen during re-parenting
            if (touchedMicrobe == null || thisMicrobe == null)
                return;

            // TODO: does this need to check for disposed exception?
            // https://github.com/Revolutionary-Games/Thrive/issues/2504
            if (touchedMicrobe.Dead || (Colony != null && Colony == touchedMicrobe.Colony))
                return;

            bool otherIsPilus = touchedMicrobe.IsPilus(touchedOwnerId);
            bool oursIsPilus = thisMicrobe.IsPilus(thisOwnerId);

            // Pilus logic
            if (otherIsPilus && oursIsPilus)
            {
                // Pilus on pilus doesn't deal damage and you can't engulf
                return;
            }

            if (otherIsPilus || oursIsPilus)
            {
                // Us attacking the other microbe, or it is attacking us

                // Disallow cannibalism
                if (touchedMicrobe.Species == thisMicrobe.Species)
                    return;

                var target = otherIsPilus ? thisMicrobe : touchedMicrobe;

                target.Damage(Constants.PILUS_BASE_DAMAGE, "pilus");
                return;
            }

            // Pili don't stop engulfing
            if (thisMicrobe.touchedMicrobes.Add(touchedMicrobe))
            {
                thisMicrobe.CheckStartEngulfingOnCandidates();
                thisMicrobe.CheckBinding();
            }
        }
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = bodyShape;

        if (body is Microbe microbe)
        {
            // GetMicrobeFromShape returns null when it was provided an invalid shape id.
            // This can happen when re-parenting is in progress.
            // https://github.com/Revolutionary-Games/Thrive/issues/2504
            var hitMicrobe = GetMicrobeFromShape(localShape) ?? this;

            // TODO: should this also check for pilus before removing the collision?
            hitMicrobe.touchedMicrobes.Remove(microbe);
        }
    }

    private void OnBodyEnteredEngulfArea(Node body)
    {
        if (body == this)
            return;

        if (body is Microbe microbe)
        {
            // TODO: does this need to check for disposed exception?
            if (microbe.Dead)
                return;

            if (otherMicrobesInEngulfRange.Add(microbe))
            {
                CheckStartEngulfingOnCandidates();
            }
        }
    }

    private void OnBodyExitedEngulfArea(Node body)
    {
        if (body is Microbe microbe)
        {
            if (otherMicrobesInEngulfRange.Remove(microbe))
            {
                CheckStopEngulfingOnTarget(microbe);
            }
        }
    }

    private bool CanBindToMicrobe(Microbe other)
    {
        // Cannot hijack the player, other species or other colonies (TODO: yet)
        return !other.IsPlayerMicrobe && other.Colony == null && other.Species == Species;
    }

    private void CheckBinding()
    {
        if (State != MicrobeState.Binding)
            return;

        if (!CanBind)
        {
            State = MicrobeState.Normal;
            return;
        }

        var other = touchedMicrobes.FirstOrDefault(CanBindToMicrobe);

        // If there is no touching microbe that can bind, no need to invoke binding.
        if (other == null)
            return;

        // Invoke this on the next frame to avoid crashing when adding a third cell
        Invoke.Instance.Perform(BeginBind);
    }

    private void BeginBind()
    {
        var other = touchedMicrobes.FirstOrDefault(CanBindToMicrobe);

        if (other == null)
        {
            GD.PrintErr("Touched eligible microbe has disappeared before binding could start");
            return;
        }

        touchedMicrobes.Remove(other);
        other.touchedMicrobes.Remove(this);

        other.MovementDirection = Vector3.Zero;

        // Create a colony if there isn't one yet
        if (Colony == null)
        {
            MicrobeColony.CreateColonyForMicrobe(this);

            if (Colony == null)
            {
                GD.PrintErr("An issue occured during colony creation!");
                return;
            }

            GD.Print("Created a new colony");
        }

        // Move out of binding state before adding the colony member to avoid accidental collisions being able to
        // recursively trigger colony attachment
        State = MicrobeState.Normal;
        other.State = MicrobeState.Normal;

        Colony.AddToColony(other, this);
    }

    /// <summary>
    ///   This checks if we can start engulfing
    /// </summary>
    private void CheckStartEngulfingOnCandidates()
    {
        if (State != MicrobeState.Engulf)
            return;

        // In the case that the microbe first comes into engulf range, we don't want to start engulfing yet
        // foreach (var microbe in touchedMicrobes.Concat(otherMicrobesInEngulfRange))
        foreach (var microbe in touchedMicrobes)
        {
            if (!attemptingToEngulf.Contains(microbe) && CanEngulf(microbe))
            {
                StartEngulfingTarget(microbe);
                attemptingToEngulf.Add(microbe);
            }
        }
    }

    private void CheckStopEngulfingOnTarget(Microbe microbe)
    {
        if (touchedMicrobes.Contains(microbe) || otherMicrobesInEngulfRange.Contains(microbe))
            return;

        StopEngulfingOnTarget(microbe);
        attemptingToEngulf.Remove(microbe);
    }

    private void StartEngulfingTarget(Microbe microbe)
    {
        AddCollisionExceptionWith(microbe);
        microbe.hostileEngulfer.Value = this;
        microbe.IsBeingEngulfed = true;
    }

    private void StopEngulfingOnTarget(Microbe microbe)
    {
        if (IsInstanceValid(microbe) && (Colony == null || Colony != microbe.Colony))
            RemoveCollisionExceptionWith(microbe);

        microbe.hostileEngulfer.Value = null;
    }
}
