using System;
using System.Collections.Generic;
using System.Linq;
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
    private SphereShape engulfShape;

    /// <summary>
    ///   Init can call _Ready if it hasn't been called yet
    /// </summary>
    private bool onReadyCalled = false;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    private OrganelleLayout<PlacedOrganelle> organelles;

    /// <summary>
    ///   Contains the piluses this microbe has for collision checking
    /// </summary>
    private HashSet<uint> pilusPhysicsShapes = new HashSet<uint>();

    private bool processesDirty = true;
    private List<TweakedProcess> processes;

    private bool cachedHexCountDirty = true;
    private int cachedHexCount;

    private Vector3 queuedMovementForce;

    // variables for engulfing
    private bool engulfMode = false;
    private bool previousEngulfMode = false;
    private bool isBeingEngulfed = false;
    private Microbe hostileEngulfer = null;
    private bool wasBeingEngulfed = false;

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

    public float Hitpoints { get; private set; } = Constants.DEFAULT_HEALTH;
    public float MaxHitpoints { get; private set; } = Constants.DEFAULT_HEALTH;

    /// <summary>
    ///   Multiplied on the movement speed of the microbe.
    /// </summary>
    public float MovementFactor { get; private set; } = 1.0f;

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

    /// <summary>
    ///   The size this microbe is for engulfing calculations
    /// </summary>
    public float EngulfSize
    {
        get
        {
            if (Species.IsBacteria)
            {
                return HexCount * 0.5f;
            }
            else
            {
                return HexCount;
            }
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

    /// <summary>
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    public Spatial OrganelleParent { get; private set; }

    public int DespawnRadiusSqr { get; set; }

    public Node SpawnedNode
    {
        get
        {
            return this;
        }
    }

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

    /// <summary>
    ///   For checking if the player is in freebuild mode or not
    /// </summary>
    public GameProperties CurrentGame { get; private set; }

    /// <summary>
    ///   Needs access to the world for population changes
    /// </summary>
    public GameWorld GameWorld
    {
        get
        {
            return CurrentGame.GameWorld;
        }
    }

    public float TimeUntilNextAIUpdate { get; set; } = 0;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds
    /// </summary>
    public Dictionary<string, float> TotalAbsorbedCompounds { get; set; } =
        new Dictionary<string, float>();

    public float AgentEmissionCooldown { get; private set; } = 0.0f;

    /// <summary>
    ///   Called when this Microbe dies
    /// </summary>
    public Action<Microbe> OnDeath { get; set; }

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    public Action<Microbe, bool> OnReproductionStatus { get; set; }

    /// <summary>
    ///   Must be called when spawned to provide access to the needed systems
    /// </summary>
    public void Init(CompoundCloudSystem cloudSystem, GameProperties currentGame, bool isPlayer)
    {
        this.cloudSystem = cloudSystem;
        CurrentGame = currentGame;
        IsPlayerMicrobe = isPlayer;

        if (IsPlayerMicrobe)
            GD.Print("Player Microbe spawned");

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
        otherAudio = GetNode<AudioStreamPlayer3D>("OtherAudio");
        movementAudio = GetNode<AudioStreamPlayer3D>("MovementAudio");

        // Setup physics callback stuff
        var engulfDetector = GetNode<Area>("EngulfDetector");
        engulfShape = (SphereShape)engulfDetector.GetNode<CollisionShape>("EngulfShape").Shape;

        engulfDetector.Connect("body_entered", this, "OnBodyEnteredEngulfArea");
        engulfDetector.Connect("body_exited", this, "OnBodyExitedEngulfArea");

        ContactsReported = Constants.DEFAULT_STORE_CONTACTS_COUNT;
        Connect("body_shape_entered", this, "OnContactBegin");
        Connect("body_shape_exited", this, "OnContactEnd");

        Mass = Constants.MICROBE_BASE_MASS;
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

        var scale = new Vector3(1.0f, 1.0f, 1.0f);

        // Bacteria are 50% the size of other cells
        if (Species.IsBacteria)
            scale = new Vector3(0.5f, 0.5f, 0.5f);

        // Scale only the graphics parts to not have physics affected
        Membrane.Scale = scale;
        OrganelleParent.Scale = scale;

        ResetOrganelleLayout();

        // Set membrane type on the membrane
        Membrane.Type = Species.MembraneType;
        Membrane.Tint = Species.Colour;
        Membrane.Dirty = true;

        SetupMicrobeHitpoints();
    }

    /// <summary>
    ///   Resets the organelles in this microbe to match the species definition
    /// </summary>
    public void ResetOrganelleLayout()
    {
        // TODO: It would be much better if only organelles that need
        // to be removed where removed, instead of everything.
        // When doing that all organelles will need to be readded anyway if this turned from a prokaryote to eukaryote

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
    public void EmitToxin(Compound agentType = null)
    {
        if (AgentEmissionCooldown > 0)
            return;

        // Only shoot if you have an agent vacuole.
        if (agentVacuoleCount < 1)
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
        AgentEmissionCooldown = Constants.AGENT_EMISSION_COOLDOWN / agentVacuoleCount;

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
        if (amount == 0)
            return;

        if (source == string.Empty)
            throw new ArgumentException("damage type is empty");

        if (amount < 0)
            throw new ArgumentException("can't deal negative damage");

        if (source == "toxin")
        {
            // Play the toxin sound
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");

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

        if (OnDeath != null)
        {
            OnDeath(this);
        }

        // Reset some stuff
        EngulfMode = false;
        MovementDirection = new Vector3(0, 0, 0);
        LinearVelocity = new Vector3(0, 0, 0);
        allOrganellesDivided = false;

        var random = new Random();

        // Releasing all the agents.
        // To not completely deadlock in this there is a maximum limit
        int createdAgents = 0;

        if (agentVacuoleCount > 0)
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
        var compoundsToRelease = new Dictionary<string, float>();

        foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
        {
            var amount = Compounds.GetCompoundAmount(type) *
                Constants.COMPOUND_RELEASE_PERCENTAGE;

            compoundsToRelease[type.InternalName] = amount;
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
            float amount = (float)HexCount / Constants.CORPSE_CHUNK_AMOUNT_DIVISER;

            var positionAdded = new Vector3(random.Next(-2.0f, 2.0f), 0,
                random.Next(-2.0f, 2.0f));

            var chunkType = new Biome.ChunkConfiguration
            {
                ChunkScale = 1.0f,
                Dissolves = true,
                Mass = 1.0f,
                Radius = 1.0f,
                Size = 3.0f,
                VentAmount = 3,

                // Add compounds
                Compounds = new Dictionary<string,
                Biome.ChunkConfiguration.ChunkCompound>(),
            };

            // They were added in order already so looping through this other thing is fine
            foreach (var entry in compoundsToRelease)
            {
                var compoundValue = new Biome.ChunkConfiguration.ChunkCompound
                {
                    // Randomize compound amount a bit so things "rot away"
                    Amount = (entry.Value / random.Next(amount / 3.0f, amount)) *
                        Constants.CORPSE_COMPOUND_COMPENSATION,
                };

                chunkType.Compounds[entry.Key] = compoundValue;
            }

            // Grab random organelle from cell and use that for model
            chunkType.Meshes = new List<Biome.ChunkConfiguration.ChunkScene>();

            var organelleToUse = organelles.Organelles.Random(random).Definition;

            var sceneToUse = new Biome.ChunkConfiguration.ChunkScene();

            if (organelleToUse.DisplayScene != string.Empty)
            {
                sceneToUse.LoadedScene = organelleToUse.LoadedScene;
            }
            else
            {
                sceneToUse.LoadedScene = SimulationParameters.Instance.GetOrganelleType(
                    "mitochondrion").LoadedScene;
            }

            chunkType.Meshes.Add(sceneToUse);

            // Finally spawn a chunk with the settings
            SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetParent(),
                chunkScene, cloudSystem, random);
        }

        // TODO: fix. Might need to rethink destroying this
        // immediately, or spawning a Node here that despawns after
        // playing.
        // Play the death sound
        // playSoundWithDistance(world, "Data/Sound/soundeffects/microbe-death.ogg",
        // microbeEntity);

        // Subtract population
        if (!IsPlayerMicrobe && !Species.PlayerSpecies)
        {
            GameWorld.AlterSpeciesPopulation(Species,
                Constants.CREATURE_DEATH_POPULATION_LOSS, "death");
        }

        if (IsPlayerMicrobe)
        {
            // If you died before entering the editor disable that
            if (OnReproductionStatus != null)
            {
                OnReproductionStatus(this, false);
            }
        }

        var deathScene = GD.Load<PackedScene>("res://src/microbe_stage/MicrobeDeathEffect.tscn");
        var deathEffects = (MicrobeDeathEffect)deathScene.Instance();
        deathEffects.Transform = Transform;
        GetParent().AddChild(deathEffects);

        // It used to be that the physics shape was removed here and
        // graphics hidden, but now this is destroyed
        QueueFree();
    }

    public void PlaySoundEffect(string effect)
    {
        // TODO: make these sound objects only be loaded once
        var sound = GD.Load<AudioStream>(effect);

        otherAudio.Stream = sound;
        otherAudio.Play();
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
    public float CalculateReproductionProgress(out Dictionary<string, float> gatheredCompounds,
        out Dictionary<string, float> totalCompounds)
    {
        // Calculate total compounds needed to split all organelles
        totalCompounds = CalculateTotalCompounds();

        // Calculate how many compounds the cell already has absorbed to grow
        gatheredCompounds = CalculateAlreadyAbsorbedCompounds();

        // Add the currently held compounds
        var keys = new List<string>(gatheredCompounds.Keys);

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
    public Dictionary<string, float> CalculateTotalCompounds()
    {
        var result = new Dictionary<string, float>();

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
    public Dictionary<string, float> CalculateAlreadyAbsorbedCompounds()
    {
        var result = new Dictionary<string, float>();

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

        Membrane.HealthFraction = Hitpoints / MaxHitpoints;

        if (Hitpoints <= 0)
        {
            HandleDeath();
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

    public override void _IntegrateForces(PhysicsDirectBodyState state)
    {
        // TODO: should movement also be applied here?

        state.Transform = GetNewPhysicsRotation(state.Transform);
    }

    private void HandleCompoundAbsorbing(float delta)
    {
        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem.AbsorbCompounds(Translation, grabRadius, Compounds,
            TotalAbsorbedCompounds, delta);
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
        if (Hitpoints < MaxHitpoints)
        {
            if (Compounds.GetCompoundAmount("atp") >= 1.0f)
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
    private void HandleReproduction(float delta)
    {
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

        if (organellesToAdd.Count > 0)
        {
            // Redo the cell membrane.
            SendOrganellePositionsToMembrane();

            // Process list is varmatically marked dirty when the split organelle is added
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

            if (OnReproductionStatus != null)
            {
                OnReproductionStatus(this, true);
            }
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

            if (Compounds.TakeCompound("atp", cost) < cost - 0.001f)
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

        if (isBeingEngulfed)
        {
            MovementFactor /= Constants.ENGULFED_MOVEMENT_DIVISION;

            Damage(Constants.ENGULF_DAMAGE * delta, "isBeingEngulfed");
            wasBeingEngulfed = true;
        }
        else if (wasBeingEngulfed && !isBeingEngulfed)
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
                    Constants.CREATURE_ESCAPE_POPULATION_GAIN, "escape engulfing");
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
                    isBeingEngulfed = false;
                }
                else
                {
                    // This check used to be just to get some distance away and you are no longer being engulfed, now
                    // the engulfer overlap end callback is used to reset this

                    // Vector3 predatorPosition = new Vector3(0, 0, 0);

                    // var ourPosition = Translation;

                    // float circleRad = 0.0f;

                    // if (!hostileEngulfer.Dead)
                    // {
                    //     predatorPosition = hostileEngulfer.Translation;
                    //     circleRad = hostileEngulfer.Radius;
                    // }

                    // if (!hostileEngulfer.EngulfMode || hostileEngulfer.Dead ||
                    //     (ourPosition - predatorPosition).LengthSquared() >= circleRad)
                    // {
                    //     hostileEngulfer = null;
                    //     isBeingEngulfed = false;
                    // }
                }
            }
            catch (ObjectDisposedException)
            {
                // Something that's disposed can't engulf us
                hostileEngulfer = null;
                isBeingEngulfed = false;
            }
        }
        else
        {
            isBeingEngulfed = false;
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
            microbe.isBeingEngulfed = true;
        }
    }

    private void RemoveEngulfedEffect()
    {
        // This kept getting doubled for some reason, so i just set it to default
        MovementFactor = 1.0f;
        wasBeingEngulfed = false;
        isBeingEngulfed = false;

        if (hostileEngulfer != null)
        {
            // Currently unused
            // hostileEngulfer.isCurrentlyEngulfing = false;
        }

        hostileEngulfer = null;
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

            Damage(MaxHitpoints * Constants.NO_ATP_DAMAGE_FRACTION, "atpDamage");
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

        return Transform.basis.Xform(MovementDirection * force) * MovementFactor *
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

    /// <summary>
    ///   Ejects compounds from the microbes behind position, into the enviroment
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

        // Get the distance to eject the compunds
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
            else if (otherIsPilus || oursIsPilus)
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
        microbe.isBeingEngulfed = true;
    }

    private void StopEngulfingOnTarget(Microbe microbe)
    {
        RemoveCollisionExceptionWith(microbe);
        microbe.hostileEngulfer = null;
    }
}
