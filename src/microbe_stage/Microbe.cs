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

    // Child components
    private AudioStreamPlayer3D engulfAudio;
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
    ///   Contains the piluses this microbe has for collision checking
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
    private bool engulfMode;

    [JsonProperty]
    private bool previousEngulfMode;

    [JsonProperty]
    private Microbe hostileEngulfer;

    [JsonProperty]
    private bool wasBeingEngulfed;

    // private bool isCurrentlyEngulfing = false;

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

    [JsonProperty]
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

    /// <summary>
    ///   If true cell is in engulf mode.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Prefer setting this instead of directly setting the private variable.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public bool EngulfMode
    {
        get => engulfMode;
        set
        {
            if (!Membrane.Type.CellWall)
            {
                engulfMode = value;
            }
            else
            {
                engulfMode = false;
            }
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
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleParent { get; private set; }

    [JsonProperty]
    public int DespawnRadiusSqr { get; set; }

    [JsonIgnore]
    public Node SpawnedNode => this;

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
        if (cloudSystem == null)
        {
            throw new Exception("Microbe not initialized");
        }

        if (onReadyCalled)
            return;

        Membrane = GetNode<Membrane>("Membrane");
        OrganelleParent = GetNode<Spatial>("OrganelleParent");
        engulfAudio = GetNode<AudioStreamPlayer3D>("EngulfAudio");
        movementAudio = GetNode<AudioStreamPlayer3D>("MovementAudio");

        cellBurstEffectScene = GD.Load<PackedScene>("res://src/microbe_stage/particles/CellBurst.tscn");

        if (IsPlayerMicrobe)
        {
            // Creates and activates the audio listener for the player microbe. Positional sound will be
            // received by it instead of the main camera.
            listener = new Listener();
            AddChild(listener);
            listener.MakeCurrent();

            GD.Print("Player Microbe spawned");
        }

        // Setup physics callback stuff
        var engulfDetector = GetNode<Area>("EngulfDetector");
        engulfShape = (SphereShape)engulfDetector.GetNode<CollisionShape>("EngulfShape").Shape;

        engulfDetector.Connect("body_entered", this, "OnBodyEnteredEngulfArea");
        engulfDetector.Connect("body_exited", this, "OnBodyExitedEngulfArea");

        ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
        Connect("body_shape_entered", this, "OnContactBegin");
        Connect("body_shape_exited", this, "OnContactEnd");

        Mass = Constants.MICROBE_BASE_MASS;

        if (IsLoadedFromSave)
        {
            // Need to re-attach our organelles
            foreach (var organelle in organelles)
                OrganelleParent.AddChild(organelle);

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
            EngulfMode = false;
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
        {
            return;
        }

        if (agentType == null)
        {
            agentType = SimulationParameters.Instance.GetCompound("oxytoxy");
        }

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
            position, direction, GetParent(),
            SpawnHelpers.LoadAgentScene(), this);

        PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
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
    ///   Applies damage to this cell. killing it if its hitpoints drop low enough
    /// </summary>
    public void Damage(float amount, string source)
    {
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
        Flash(1.0f, new Color(1, 0, 0, 0.5f));

        // Kill if ran out of health
        if (Hitpoints <= 0.0f)
        {
            Hitpoints = 0.0f;
            Kill();
        }
    }

    public void ToggleEngulfMode()
    {
        EngulfMode = !EngulfMode;
    }

    /// <summary>
    ///   Returns true when this microbe can engulf the target
    /// </summary>
    public bool CanEngulf(Microbe target)
    {
        // Disallow cannibalism
        if (target.Species == Species)
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

        // Reset some stuff
        EngulfMode = false;
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
                    Translation, direction, GetParent(),
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

        int chunksToSpawn = Math.Max(1, HexCount / Constants.CORPSE_CHUNK_DIVISER);

        var chunkScene = SpawnHelpers.LoadChunkScene();

        for (int i = 0; i < chunksToSpawn; ++i)
        {
            // Amount of compound in one chunk
            float amount = HexCount / Constants.CORPSE_CHUNK_AMOUNT_DIVISER;

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
                if (!string.IsNullOrEmpty(organelle.Definition.DisplayScene))
                {
                    sceneToUse.LoadedScene = organelle.Definition.LoadedScene;
                    sceneToUse.SceneModelPath = organelle.Definition.DisplaySceneModelPath;
                    break;
                }
            }

            // If no organelles have a scene, use mitochondrion as fallback
            if (sceneToUse.LoadedScene == null)
            {
                sceneToUse.LoadedScene = SimulationParameters.Instance.GetOrganelleType(
                    "mitochondrion").LoadedScene;
                sceneToUse.SceneModelPath = null;
            }

            chunkType.Meshes.Add(sceneToUse);

            // Finally spawn a chunk with the settings
            SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetParent(),
                chunkScene, cloudSystem, random);
        }

        // Subtract population
        if (!IsPlayerMicrobe && !Species.PlayerSpecies)
        {
            GameWorld.AlterSpeciesPopulation(Species,
                Constants.CREATURE_DEATH_POPULATION_LOSS, "death");
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
    public void Divide()
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
                var didntFit = copyEntity.Compounds.AddCompound(compound, amountToGive);

                if (didntFit > 0)
                {
                    // TODO: handle the excess compound that didn't fit in the other cell
                }
            }
            else
            {
                // Non-reproductive compounds just always get split evenly to both cells.
                Compounds.TakeCompound(compound, amount * 0.5f);

                var didntFit = copyEntity.Compounds.AddCompound(compound, amount * 0.5f);

                if (didntFit > 0)
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
        }

        CheckEngulfShapeSize();
        HandleCompoundAbsorbing(delta);

        // Movement factor is reset here. HandleEngulfing will set the right value
        MovementFactor = 1.0f;
        queuedMovementForce = new Vector3(0, 0, 0);

        // Reduce agent emission cooldown
        AgentEmissionCooldown -= delta;
        if (AgentEmissionCooldown < 0)
            AgentEmissionCooldown = 0;

        HandleFlashing(delta);
        HandleHitpointsRegeneration(delta);
        HandleReproduction(delta);

        // Handles engulfing related stuff as well as modifies the
        // movement factor. This needs to be done before Update is
        // called on organelles as movement organelles will use
        // MovementFactor.
        HandleEngulfing(delta);
        HandleOsmoregulation(delta);

        // Let organelles do stuff (this for example gets the movement force from flagella)
        foreach (var organelle in organelles.Organelles)
        {
            organelle.Update(delta);
        }

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

            // Play movement sound if one isn't already playing.
            if (!movementAudio.Playing)
                movementAudio.Play();
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

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
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
    }

    private void HandleCompoundAbsorbing(float delta)
    {
        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem.AbsorbCompounds(Translation, grabRadius, Compounds,
            TotalAbsorbedCompounds, delta, Membrane.Type.ResourceAbsorptionFactor);
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
            else if (Compounds.GetCompoundAmount(type) > 2 * Compounds.Capacity)
            {
                // Vent the part that went over
                float toVent = Compounds.GetCompoundAmount(type) - (2 * Compounds.Capacity);

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
        if (allOrganellesDivided)
        {
            // Ready to reproduce already. Only the player gets here
            // as other cells split and reset varmatically
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
            var radiusOffset = Hex.HexNeighbourOffset[Hex.HexSide.BOTTOM_LEFT];
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

            OnReproductionStatus?.Invoke(this, true);
        }
        else
        {
            // Return the first cell to its normal, non duplicated cell arrangement.
            if (!Species.PlayerSpecies)
            {
                GameWorld.AlterSpeciesPopulation(Species,
                    Constants.CREATURE_REPRODUCE_POPULATION_GAIN, "reproduced");
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

            if (Compounds.TakeCompound(atp, cost) < cost - 0.001f)
            {
                EngulfMode = false;
            }
        }

        ProcessPhysicsForEngulfing();

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
        if (hostileEngulfer != null)
        {
            try
            {
                // Dead things can't engulf us
                if (hostileEngulfer.Dead)
                {
                    hostileEngulfer = null;
                    IsBeingEngulfed = false;
                }
            }
            catch (ObjectDisposedException)
            {
                // Something that's disposed can't engulf us
                hostileEngulfer = null;
                IsBeingEngulfed = false;
            }
        }
        else
        {
            IsBeingEngulfed = false;
        }

        previousEngulfMode = EngulfMode;
    }

    private void ProcessPhysicsForEngulfing()
    {
        if (!EngulfMode)
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
            microbe.hostileEngulfer = this;
            microbe.IsBeingEngulfed = true;
        }
    }

    private void RemoveEngulfedEffect()
    {
        // This kept getting doubled for some reason, so i just set it to default
        MovementFactor = 1.0f;
        wasBeingEngulfed = false;
        IsBeingEngulfed = false;

        if (hostileEngulfer != null)
        {
            // Currently unused
            // hostileEngulfer.isCurrentlyEngulfing = false;
        }

        hostileEngulfer = null;
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

            var cellBurstEffectParticles = (Particles)cellBurstEffectScene.Instance();
            var cellBurstEffectMaterial = (ParticlesMaterial)cellBurstEffectParticles.ProcessMaterial;

            cellBurstEffectMaterial.EmissionSphereRadius = Radius / 2;
            cellBurstEffectMaterial.LinearAccel = Radius / 2;
            cellBurstEffectParticles.OneShot = true;
            AddChild(cellBurstEffectParticles);

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

        if (Membrane.DissolveEffectValue >= 6)
        {
            QueueFree();
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
        Compounds.Capacity = organellesCapacity;
    }

    [DeserializedCallbackAllowed]
    private void OnOrganelleRemoved(PlacedOrganelle organelle)
    {
        organellesCapacity -= organelle.StorageCapacity;
        if (organelle.IsAgentVacuole)
            AgentVacuoleCount -= 1;
        organelle.OnRemovedFromMicrobe();

        // The organelle only detaches but doesn't delete itself, so we delete it here
        organelle.QueueFree();

        processesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;

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
        membraneOrganellePositionsAreDirty = false;
    }

    /// <summary>
    ///   Recomputes storage from organelles, used after loading a save
    /// </summary>
    private void RecomputeOrganelleCapacity()
    {
        organellesCapacity = organelles.Sum(o => o.StorageCapacity);
        Compounds.Capacity = organellesCapacity;
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
        var membraneCoords = Membrane.GetExternalOrganelle(exit.x, exit.z);

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

    private void OnContactBegin(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;

        if (body is Microbe microbe)
        {
            // TODO: does this need to check for disposed exception?
            if (microbe.Dead)
                return;

            bool otherIsPilus = microbe.IsPilus(microbe.ShapeFindOwner(bodyShape));
            bool oursIsPilus = IsPilus(ShapeFindOwner(localShape));

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
                if (microbe.Species == Species)
                    return;

                var target = otherIsPilus ? this : microbe;

                target.Damage(Constants.PILUS_BASE_DAMAGE, "pilus");
                return;
            }

            // Pili don't stop engulfing
            if (touchedMicrobes.Add(microbe))
            {
                CheckStartEngulfingOnCandidates();
            }
        }
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = bodyShape;
        _ = localShape;

        if (body is Microbe microbe)
        {
            // TODO: should this also check for pilus before removing the collision?
            touchedMicrobes.Remove(microbe);
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

    /// <summary>
    ///   This checks if we can start engulfing
    /// </summary>
    private void CheckStartEngulfingOnCandidates()
    {
        if (!EngulfMode)
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
        microbe.hostileEngulfer = this;
        microbe.IsBeingEngulfed = true;
    }

    private void StopEngulfingOnTarget(Microbe microbe)
    {
        RemoveCollisionExceptionWith(microbe);
        microbe.hostileEngulfer = null;
    }
}
