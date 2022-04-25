using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Array = Godot.Collections.Array;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Engulf, Bind/Unbind, Colony,
///   Damage, Kill, Pilus, Membrane
/// </summary>
public partial class Microbe
{
    private SphereShape engulfShape = null!;

    /// <summary>
    ///   Contains the pili this microbe has for collision checking
    /// </summary>
    private HashSet<uint> pilusPhysicsShapes = new();

    private uint? engulfedAreaShapeOwner;

    private bool membraneOrganellePositionsAreDirty = true;

    private bool destroyed;

    // variables for engulfing
    [JsonProperty]
    private bool previousEngulfMode;

    [JsonProperty]
    private EntityReference<Microbe> hostileEngulfer = new();

    [JsonProperty]
    private bool wasBeingEngulfed;

    /// <summary>
    ///   Tracks other engulfables that are within the engulf area and are ignoring collisions with this body.
    /// </summary>
    private HashSet<IEngulfable> otherEngulfablesInEngulfRange = new();

    /// <summary>
    ///   Tracks engulfables this is touching, for beginning engulfing
    /// </summary>
    private HashSet<IEngulfable> touchedEngulfables = new();

    /// <summary>
    ///   Engulfables that this cell is actively trying to engulf
    /// </summary>
    private HashSet<IEngulfable> attemptingToEngulf = new();

    [JsonProperty]
    private List<EngulfedObject> engulfedObjects = new();

    [JsonProperty]
    private float escapeInterval;

    [JsonProperty]
    private bool hasEscaped;

    [JsonProperty]
    private float engulfedSize;

    /// <summary>
    ///   Controls for how long the flashColour is held before going
    ///   back to species colour.
    /// </summary>
    [JsonProperty]
    private float flashDuration;

    [JsonProperty]
    private Color flashColour = new(0, 0, 0, 0);

    /// <summary>
    ///   This determines how important the current flashing action is. This allows higher priority flash colours to
    ///   take over.
    /// </summary>
    [JsonProperty]
    private int flashPriority;

    private PackedScene cellBurstEffectScene = null!;

    [JsonProperty]
    private bool deathParticlesSpawned;

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
    public MicrobeColony? Colony { get; set; }

    [JsonProperty]
    public Microbe? ColonyParent { get; set; }

    [JsonProperty]
    public List<Microbe>? ColonyChildren { get; set; }

    /// <summary>
    ///   The membrane of this Microbe. Used for grabbing radius / points from this.
    /// </summary>
    [JsonIgnore]
    public Membrane Membrane { get; private set; } = null!;

    [JsonProperty]
    public float Hitpoints { get; private set; } = Constants.DEFAULT_HEALTH;

    [JsonProperty]
    public float MaxHitpoints { get; private set; } = Constants.DEFAULT_HEALTH;

    // Properties for engulfing
    [JsonProperty]
    public bool IsBeingEngulfed { get; set; }

    [JsonProperty]
    public bool IsIngested { get; set; }

    [JsonIgnore]
    public EntityReference<Microbe> HostileEngulfer => hostileEngulfer;

    [JsonIgnore]
    public AliveMarker AliveMarker { get; } = new();

    /// <summary>
    ///   The current state of the microbe. Shared across the colony
    /// </summary>
    [JsonIgnore]
    public MicrobeState State
    {
        get
        {
            if (Colony == null)
                return state;

            var colonyState = Colony.State;

            // Override engulf mode in colony cells that can't engulf
            if (colonyState == MicrobeState.Engulf && Membrane.Type.CellWall)
                return MicrobeState.Normal;

            return colonyState;
        }
        set
        {
            if (state == value)
                return;

            // Engulfing is not legal for microbes will cell walls
            if (value == MicrobeState.Engulf && Membrane.Type.CellWall)
            {
                // Don't warn when in a multicellular colony as the other cells there can enter engulf mode
                if (ColonyParent != null && IsMulticellular)
                    return;

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

    /// <summary>
    ///   The size this microbe is for engulfing calculations
    /// </summary>
    [JsonIgnore]
    public float Size
    {
        get
        {
            if (CellTypeProperties.IsBacteria)
            {
                return HexCount * 0.5f;
            }

            return HexCount;
        }
    }

    /// <summary>
    ///   Returns true when this microbe can enable binding mode. Multicellular species can't attach random cells
    ///   to themselves anymore
    /// </summary>
    [JsonIgnore]
    public bool CanBind => !IsMulticellular && (organelles?.Any(p => p.IsBindingAgent) == true || Colony != null);

    [JsonIgnore]
    public bool CanUnbind => !IsMulticellular && Colony != null;

    /// <summary>
    ///   Called when this Microbe dies
    /// </summary>
    [JsonProperty]
    public Action<Microbe>? OnDeath { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnUnbindEnabled { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnUnbound { get; set; }

    [JsonProperty]
    public Action<Microbe, Microbe>? OnIngested { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnEngulfmentStorageFull { get; set; }

    /// <summary>
    ///   Updates the intensity of wigglyness of this cell's membrane based on membrane type, taking
    ///   membrane rigidity into account.
    /// </summary>
    public void ApplyMembraneWigglyness()
    {
        Membrane.WigglyNess = Membrane.Type.BaseWigglyness - (CellTypeProperties.MembraneRigidity /
            Membrane.Type.BaseWigglyness) * 0.2f;
        Membrane.MovementWigglyNess = Membrane.Type.MovementWigglyness - (CellTypeProperties.MembraneRigidity /
            Membrane.Type.MovementWigglyness) * 0.2f;
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
        Membrane.Tint = CellTypeProperties.Colour;
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

        if (source is "toxin" or "oxytoxy")
        {
            // TODO: Replace this take damage sound with a more appropriate one.

            // Play the toxin sound
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");

            // Divide damage by toxin resistance
            amount /= CellTypeProperties.MembraneType.ToxinResistance;
        }
        else if (source == "pilus")
        {
            // Play the pilus sound
            PlaySoundEffect("res://assets/sounds/soundeffects/pilus_puncture_stab.ogg");

            // TODO: this may get triggered a lot more than the toxin
            // so this might need to be rate limited or something
            // Divide damage by physical resistance
            amount /= CellTypeProperties.MembraneType.PhysicalResistance;
        }
        else if (source == "chunk")
        {
            // TODO: Replace this take damage sound with a more appropriate one.

            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");

            // Divide damage by physical resistance
            amount /= CellTypeProperties.MembraneType.PhysicalResistance;
        }
        else if (source == "atpDamage")
        {
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-atp-damage.ogg");
        }
        else if (source == "ice")
        {
            PlayNonPositionalSoundEffect("res://assets/sounds/soundeffects/microbe-ice-damage.ogg", 0.5f);

            // Divide damage by physical resistance
            amount /= CellTypeProperties.MembraneType.PhysicalResistance;
        }

        Hitpoints -= amount;

        ModLoader.ModInterface.TriggerOnDamageReceived(this, amount, IsPlayerMicrobe);

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
    public bool CanEngulf(IEngulfable target)
    {
        // Log error if trying to engulf something that is disposed, we got a crash log trace with an error with that
        // TODO: find out why disposed microbes can be attempted to be engulfed
        try
        {
            if (target is Microbe casted)
            {
                // Access a Godot property to throw disposed exception
                _ = casted.GlobalTransform;
            }
        }
        catch (ObjectDisposedException)
        {
            GD.PrintErr("Touched microbe has been disposed before engulfing could start");
            return false;
        }

        // Limit amount of things that can be engulfed at once
        if (engulfedSize >= Size || engulfedSize + target.Size >= Size)
            return false;

        // Disallow cannibalism
        if (target is Microbe microbe && microbe.Species == Species)
            return false;

        // Membranes with Cell Wall cannot engulf
        if (Membrane.Type.CellWall)
            return false;

        // Needs to be big enough to engulf
        return Size >= target.Size * Constants.ENGULF_SIZE_RATIO_REQ;
    }

    public void OnEngulfed()
    {
        OnIngested?.Invoke(this, HostileEngulfer.Value!);
    }

    public void OnEjected()
    {
        // This will already be handled by HandleDecay()
    }

    /// <summary>
    ///   Offset relative to the colony lead cell. Throws if this cell is not part of a colony
    /// </summary>
    /// <returns>The offset</returns>
    public Vector3 GetOffsetRelativeToMaster()
    {
        return (GlobalTransform.origin - Colony!.Master.GlobalTransform.origin).Rotated(Vector3.Down,
            Colony.Master.Rotation.y);
    }

    /// <summary>
    ///  Public because it needs to be called by external organelles only.
    ///  Not meant for other uses.
    /// </summary>
    public void SendOrganellePositionsToMembrane()
    {
        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized first");

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
    ///   Instantly kills this microbe and queues this entity to be destroyed
    /// </summary>
    public void Kill()
    {
        if (Dead)
            return;

        Dead = true;

        OnDeath?.Invoke(this);
        ModLoader.ModInterface.TriggerOnMicrobeDied(this, IsPlayerMicrobe);

        OnDestroyed();

        // Reset some stuff
        State = MicrobeState.Normal;
        MovementDirection = new Vector3(0, 0, 0);
        LinearVelocity = new Vector3(0, 0, 0);
        allOrganellesDivided = false;

        // Releasing all the agents.
        // To not completely deadlock in this there is a maximum limit
        int createdAgents = 0;

        if (AgentVacuoleCount > 0)
        {
            var oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

            var amount = Compounds.GetCompoundAmount(oxytoxy);

            var props = new AgentProperties(Species, oxytoxy);

            var agentScene = SpawnHelpers.LoadAgentScene();

            while (amount > Constants.MAXIMUM_AGENT_EMISSION_AMOUNT)
            {
                var direction = new Vector3(random.Next(0.0f, 1.0f) * 2 - 1,
                    0, random.Next(0.0f, 1.0f) * 2 - 1);

                var agent = SpawnHelpers.SpawnAgent(props, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT,
                    Constants.EMITTED_AGENT_LIFETIME,
                    Translation, direction, GetStageAsParent(),
                    agentScene, this);

                ModLoader.ModInterface.TriggerOnToxinEmitted(agent);

                amount -= Constants.MAXIMUM_AGENT_EMISSION_AMOUNT;
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
        foreach (var organelle in organelles!)
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
            var chunk = SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetStageAsParent(),
                chunkScene, cloudSystem!, random);

            // Add to the spawn system to make these chunks limit possible number of entities
            SpawnSystem.AddEntityToTrack(chunk);

            ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, false);
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

        if (IsPlayerMicrobe)
        {
            // Playing from a positional audio player won't have any effect since the listener is
            // directly on it.
            PlayNonPositionalSoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg", 0.5f);
        }
        else
        {
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg");
        }

        // Disable collisions
        CollisionLayer = 0;
        CollisionMask = 0;

        // Post-death handling is done in HandleDeath
    }

    public void OnDestroyed()
    {
        if (destroyed)
            return;

        destroyed = true;

        // TODO: find out a way to cleanly despawn colonies without having to run the reproduction progress lost logic
        Colony?.RemoveFromColony(this);

        AliveMarker.Alive = false;
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
        if (State is MicrobeState.Unbinding or MicrobeState.Binding)
            State = MicrobeState.Normal;

        if (!CanUnbind)
            return;

        // TODO: once the colony leader can leave without the entire colony disbanding this perhaps should keep the
        // disband entire colony functionality
        Colony!.RemoveFromColony(this);
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

        if (IsMulticellular && Colony?.Master == this)
        {
            // Lost a member of the multicellular organism
            OnMulticellularColonyCellLost(microbe);
        }

        if (HostileEngulfer != microbe)
            microbe.RemoveCollisionExceptionWith(this);
        if (microbe.HostileEngulfer != this)
            RemoveCollisionExceptionWith(microbe);
    }

    internal void ReParentShapes(Microbe to, Vector3 offset)
    {
        // TODO: if microbeRotation is the rotation of *this* instance we should use the variable here directly
        // An object doesn't need to be told its own member variable in a method...
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        foreach (var organelle in organelles!)
            organelle.ReParentShapes(to, offset);
    }

    internal void OnColonyMemberAdded(Microbe microbe)
    {
        if (microbe == this)
        {
            if (Colony == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Colony)} must be set before calling {nameof(OnColonyMemberAdded)}");
            }

            OnIGotAddedToColony();

            if (Colony.Master != this)
            {
                Mode = ModeEnum.Static;
            }

            ReParentShapes(Colony.Master, GetOffsetRelativeToMaster());
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

    private Microbe? GetColonyMemberWithShapeOwner(uint ownerID, MicrobeColony colony)
    {
        foreach (var microbe in colony.ColonyMembers)
        {
            if (microbe.organelles!.Any(o => o.HasShape(ownerID)) || microbe.IsPilus(ownerID))
                return microbe;
        }

        // Now as we consider 0 valid, still don't want to crash here
        // TODO: re-check this once this issue is done: https://github.com/Revolutionary-Games/Thrive/issues/2671
        if (ownerID == 0)
            return null;

        // TODO: I really hope there is no way to hit this. I would really hate to reduce the game stability due to
        // possibly bogus ownerID values that sometimes seem to come from Godot
        // https://github.com/Revolutionary-Games/Thrive/issues/2504
        throw new InvalidOperationException();
    }

    private void OnIGotAddedToColony()
    {
        // Multicellular creature can stay in engulf mode when growing things
        if (!IsMulticellular || State != MicrobeState.Engulf)
        {
            State = MicrobeState.Normal;
        }

        UnreadyToReproduce();

        if (ColonyParent == null)
            return;

        var newTransform = GetNewRelativeTransform();

        Rotation = newTransform.Rotation;
        Translation = newTransform.Translation;

        ChangeNodeParent(ColonyParent);
    }

    private void SetMembraneFromSpecies()
    {
        Membrane.Type = CellTypeProperties.MembraneType;
        Membrane.Tint = CellTypeProperties.Colour;
        Membrane.Dirty = true;
        ApplyMembraneWigglyness();
    }

    private void CheckEngulfShapeSize()
    {
        var wanted = Radius;
        if (engulfShape.Radius != wanted)
            engulfShape.Radius = wanted;
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
                Membrane.Tint = CellTypeProperties.Colour;
            }

            // Flashing ended
            if (flashDuration <= 0)
            {
                flashDuration = 0;

                // Restore colour
                Membrane.Tint = CellTypeProperties.Colour;
            }
        }
    }

    /// <summary>
    ///   Handles things related to binding
    /// </summary>
    private void HandleBinding(float delta)
    {
        if (State != MicrobeState.Binding)
        {
            if (bindingAudio.Playing && bindingAudio.Volume > 0)
            {
                bindingAudio.Volume -= delta;

                if (bindingAudio.Volume <= 0)
                    bindingAudio.Stop();
            }

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

        // To balance loudness, here the binding audio's max volume is reduced to 0.6 in linear volume
        if (bindingAudio.Volume < 0.6f)
        {
            bindingAudio.Volume += delta;
        }
        else if (bindingAudio.Volume >= 0.6f)
        {
            bindingAudio.Volume = 0.6f;
        }

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

        ProcessPhysicsForEngulfing(delta);

        // Play sound
        if (State == MicrobeState.Engulf)
        {
            if (!engulfAudio.Playing)
                engulfAudio.Play();

            // To balance loudness, here the engulfment audio's max volume is reduced to 0.6 in linear volume

            if (engulfAudio.Volume < 0.6f)
            {
                engulfAudio.Volume += delta;
            }
            else if (engulfAudio.Volume >= 0.6f)
            {
                engulfAudio.Volume = 0.6f;
            }

            // Flash the membrane blue.
            Flash(1, new Color(0.2f, 0.5f, 1.0f, 0.5f));
        }
        else
        {
            if (engulfAudio.Playing && engulfAudio.Volume > 0)
            {
                engulfAudio.Volume -= delta;

                if (engulfAudio.Volume <= 0)
                    engulfAudio.Stop();
            }
        }

        // Movement modifier
        if (State == MicrobeState.Engulf)
        {
            MovementFactor /= Constants.ENGULFING_MOVEMENT_DIVISION;
        }

        if (IsBeingEngulfed)
        {
            // Microbe is now getting absorbed into the engulfer

            MovementFactor /= Constants.ENGULFED_MOVEMENT_DIVISION;

            wasBeingEngulfed = true;
        }
        else if (wasBeingEngulfed && !IsBeingEngulfed && !IsIngested)
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
        var hostile = HostileEngulfer.Value;
        if (hostile != null)
        {
            // Dead or engulfed things can't engulf us
            if (hostile.Dead || hostile.IsIngested)
            {
                HostileEngulfer.Value = null;
                IsBeingEngulfed = false;
            }
        }
        else
        {
            IsBeingEngulfed = false;
        }

        previousEngulfMode = State == MicrobeState.Engulf;
    }

    private void ProcessPhysicsForEngulfing(float delta)
    {
        if (State != MicrobeState.Engulf || engulfedSize >= Size)
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

        // Apply engulf effect to the objects we are engulfing
        foreach (var engulfable in attemptingToEngulf)
        {
            // Ignore non-engulfable objects
            if (!CanEngulf(engulfable))
            {
                StopEngulfingOnTarget(engulfable);
                engulfable.IsBeingEngulfed = false;
                continue;
            }

            if (engulfable.IsIngested)
                continue;

            if (engulfable is RigidBody body)
            {
                // Pull nearby bodies into our cell
                var direction = body.GlobalTransform.origin.DirectionTo(GlobalTransform.origin);
                body.AddCentralForce(direction * body.Mass * Constants.ENGULFING_PULL_SPEED);
            }

            engulfable.HostileEngulfer.Value = this;
            engulfable.IsBeingEngulfed = true;
        }
    }

    private void RemoveEngulfedEffect()
    {
        // This kept getting doubled for some reason, so i just set it to default
        MovementFactor = 1.0f;
        wasBeingEngulfed = false;
        IsBeingEngulfed = false;

        if (HostileEngulfer.Value != null)
        {
            // Currently unused
            // hostileEngulfer.isCurrentlyEngulfing = false;
        }

        HostileEngulfer.Value = null;
    }

    /// <summary>
    ///   Handles the death of this microbe. This queues this object
    ///   for deletion and handles some post-death actions.
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

            // This loop is placed here (which isn't related to the particles but for convenience)
            // so this loop is run only once
            foreach (var engulfed in engulfedObjects.ToList())
            {
                if (engulfed.Engulfable.Value != null)
                    EjectEngulfable(engulfed.Engulfable.Value);
            }
        }

        foreach (var organelle in organelles!)
        {
            organelle.Hide();
        }

        Membrane.DissolveEffectValue += delta * Constants.MEMBRANE_DISSOLVE_SPEED;

        if (Membrane.DissolveEffectValue >= 1)
        {
            this.DestroyDetachAndQueueFree();
        }
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
        if (Colony == null)
        {
            throw new InvalidOperationException(
                $"{nameof(RevertNodeParent)} can only be called on microbes in a colony");
        }

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

        var thisOwnerId = ShapeFindOwner(localShape);
        var thisMicrobe = GetMicrobeFromShape(localShape);

        // localShape is invalid. This can happen during re-parenting
        if (thisMicrobe == null)
            return;

        if (body is Microbe colonyLeader)
        {
            var touchedOwnerId = colonyLeader.ShapeFindOwner(bodyShape);
            var touchedMicrobe = colonyLeader.GetMicrobeFromShape(bodyShape);

            // bodyShape is invalid. This can happen during re-parenting
            // Disabled this warning here as touchedMicrobe is used so diversely that it's much more convenient to
            // do null check just once
            // ReSharper disable once UseNullPropagationWhenPossible
            if (touchedMicrobe == null)
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
            if (thisMicrobe.touchedEngulfables.Add(touchedMicrobe))
            {
                thisMicrobe.CheckStartEngulfingOnCandidates();
                thisMicrobe.CheckBinding();
            }

            // Play bump sound if certain total collision impulse is reached (adjusted by mass)
            if (thisMicrobe.collisionForce / Mass > Constants.CONTACT_IMPULSE_TO_BUMP_SOUND)
            {
                thisMicrobe.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-collision.ogg");
            }
        }
        else if (body is IEngulfable engulfable)
        {
            if (thisMicrobe.touchedEngulfables.Add(engulfable))
            {
                thisMicrobe.CheckStartEngulfingOnCandidates();
            }
        }
    }

    private void OnContactEnd(int bodyID, Node body, int bodyShape, int localShape)
    {
        _ = bodyID;
        _ = bodyShape;

        if (body is IEngulfable engulfable)
        {
            // GetMicrobeFromShape returns null when it was provided an invalid shape id.
            // This can happen when re-parenting is in progress.
            // https://github.com/Revolutionary-Games/Thrive/issues/2504
            var hitMicrobe = GetMicrobeFromShape(localShape) ?? this;

            // TODO: should this also check for pilus before removing the collision?
            hitMicrobe.touchedEngulfables.Remove(engulfable);
        }
    }

    private void OnBodyEnteredEngulfArea(Node body)
    {
        // ReSharper disable once MergeCastWithTypeCheck
        if (body is not IEngulfable engulfable || engulfable == this)
            return;

        if (otherEngulfablesInEngulfRange.Add((IEngulfable)body))
        {
            CheckStartEngulfingOnCandidates();
        }
    }

    private void OnBodyExitedEngulfArea(Node body)
    {
        if (body is IEngulfable engulfable)
        {
            if (otherEngulfablesInEngulfRange.Remove(engulfable))
            {
                CheckStopEngulfingOnTarget(engulfable);
            }
        }
    }

    private void OnAreaEnteredEngulfableArea(Area area)
    {
        if (area == engulfableArea || State != MicrobeState.Engulf)
            return;

        if (area.GetParent() is IEngulfable engulfable && CanEngulf(engulfable))
            IngestEngulfable(engulfable);
    }

    /// <summary>
    ///   Ingests (finishes engulfment of) the target into this microbe. Does not check whether the target
    ///   can be engulfed or not.
    /// </summary>
    private void IngestEngulfable(IEngulfable target)
    {
        if (target.IsIngested)
            return;

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ingested
            return;
        }

        touchedEngulfables.Remove(target);

        target.IsIngested = true;
        target.OnEngulfed();
        target.IsBeingEngulfed = false;

        engulfedSize += target.Size;

        body.Mode = ModeEnum.Static;

        // Disable collisions
        body.CollisionLayer = 0;
        body.CollisionMask = 0;

        var temp = body.GlobalTransform;
        body.ReParent(this);
        body.GlobalTransform = temp;

        // Delay this to avoid potential AddChild call failing due to parent node being busy error
        Invoke.Instance.Queue(() =>
        {
            var tween = new Tween();
            body.AddChild(tween);
            tween.Connect("tween_all_completed", this, nameof(OnIngestAnimationFinished), new Array { body });
            tween.Connect("tween_all_completed", tween, "queue_free");
            tween.InterpolateProperty(body, "scale", null, body.Scale / 2, 1.0f);
            var finalPosition = new Vector3(
                random.Next(0.0f, body.Translation.x), body.Translation.y, random.Next(0.0f, body.Translation.z));
            tween.InterpolateProperty(body, "translation", null, finalPosition, 1.5f);
            tween.Start();
        });
    }

    /// <summary>
    ///   Ejects an ingested engulfable from this microbe.
    /// </summary>
    private void EjectEngulfable(IEngulfable target)
    {
        if (!target.IsIngested)
            return;

        var engulfedObject = engulfedObjects.Find(e => e.Engulfable == target);
        if (engulfedObject == null)
            return;

        engulfedObjects.Remove(engulfedObject);

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ejected
            return;
        }

        target.IsIngested = false;
        target.OnEjected();

        engulfedSize -= target.Size;

        body.Mode = ModeEnum.Rigid;

        // Set to default microbe collision layer and mask values
        body.CollisionLayer = 3;
        body.CollisionMask = 3;

        // Reparent to world node
        var temp = body.GlobalTransform;
        body.ReParent(GetParent());
        body.GlobalTransform = temp;

        // Delay this to avoid potential AddChild call failing due to parent node being busy error
        Invoke.Instance.Queue(() =>
        {
            var tween = new Tween();
            body.AddChild(tween);
            tween.Connect("tween_all_completed", tween, "queue_free");
            tween.InterpolateProperty(body, "scale", body.Scale / 2, Vector3.One, 1.0f);
            tween.Start();
        });
    }

    private bool CanBindToMicrobe(IEngulfable other)
    {
        if (other is Microbe microbe)
        {
            // Cannot hijack the player, other species or other colonies (TODO: yet)
            return !microbe.IsPlayerMicrobe && microbe.Colony == null && microbe.Species == Species;
        }

        return false;
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

        var other = touchedEngulfables.FirstOrDefault(CanBindToMicrobe);

        // If there is no touching microbe that can bind, no need to invoke binding.
        if (other == null)
            return;

        // Invoke this on the next frame to avoid crashing when adding a third cell
        Invoke.Instance.Queue(BeginBind);
    }

    private void BeginBind()
    {
        var other = touchedEngulfables.FirstOrDefault(CanBindToMicrobe) as Microbe;

        if (other == null)
        {
            GD.PrintErr("Touched eligible microbe has disappeared before binding could start");
            return;
        }

        touchedEngulfables.Remove(other);

        try
        {
            other.touchedEngulfables.Remove(this);

            other.MovementDirection = Vector3.Zero;

            // This should ensure that Godot side will not throw disposed exception in an unexpected place causing
            // binding problems
            _ = other.GlobalTransform;
        }
        catch (ObjectDisposedException)
        {
            GD.PrintErr("Touched eligible microbe has been disposed before binding could start");
            return;
        }

        // This is probably unnecessary, but I'd like to make sure we have proper logging if this condition is ever
        // reached -hhyyrylainen
        try
        {
            _ = GlobalTransform;
        }
        catch (ObjectDisposedException e)
        {
            GD.PrintErr("Microbe that should be bound to is disposed. This should never happen. Please report this. ",
                e);
            return;
        }

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
        foreach (var engulfable in touchedEngulfables)
        {
            if (attemptingToEngulf.Add(engulfable) && CanEngulf(engulfable))
            {
                StartEngulfingTarget(engulfable);
            }
            else if (engulfedSize >= Size || engulfedSize + engulfable.Size >= Size)
            {
                OnEngulfmentStorageFull?.Invoke(this);
            }
        }
    }

    private void CheckStopEngulfingOnTarget(IEngulfable target)
    {
        if (touchedEngulfables.Contains(target) || otherEngulfablesInEngulfRange.Contains(target))
            return;

        StopEngulfingOnTarget(target);
        attemptingToEngulf.Remove(target);
    }

    private void StartEngulfingTarget(IEngulfable target)
    {
        if (target is not RigidBody body)
            return;

        try
        {
            target.IsBeingEngulfed = true;
            target.HostileEngulfer.Value = this;

            if (Colony != null)
            {
                Colony.Master.AddCollisionExceptionWith(body);
            }
            else
            {
                AddCollisionExceptionWith(body);
            }
        }
        catch (ObjectDisposedException)
        {
            GD.Print("Touched eligible engulfable has been disposed before engulfing could start");
        }
    }

    private void StopEngulfingOnTarget(IEngulfable target)
    {
        target.HostileEngulfer.Value = null;

        if (target is not RigidBody body)
            return;

        try
        {
            if (body is Microbe microbe && (Colony == null || Colony != microbe.Colony))
            {
                if (Colony != null)
                {
                    Colony.Master.RemoveCollisionExceptionWith(microbe);
                }
                else
                {
                    RemoveCollisionExceptionWith(microbe);
                }
            }
            else if (body is not Microbe)
            {
                if (Colony != null)
                {
                    Colony.Master.RemoveCollisionExceptionWith(body);
                }
                else
                {
                    RemoveCollisionExceptionWith(body);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            GD.Print("Engulfed object has been disposed before engulfing could stop");
        }
    }

    private void OnIngestAnimationFinished(RigidBody target)
    {
        var engulfable = (IEngulfable)target;

        engulfedObjects.Add(new EngulfedObject(engulfable, engulfable.CalculateDigestibleCompounds()));

        otherEngulfablesInEngulfRange.Remove(engulfable);
        touchedEngulfables.Remove(engulfable);
        attemptingToEngulf.Remove(engulfable);
    }

    /// <summary>
    ///   Holds cached informations in addition to the engulfed object
    /// </summary>
    private class EngulfedObject
    {
        public EngulfedObject(IEngulfable engulfable, Dictionary<Compound, float> cachedEngulfableCompounds)
        {
            Engulfable = new EntityReference<IEngulfable>(engulfable);
            CachedEngulfableCompounds = new Dictionary<Compound, float>(cachedEngulfableCompounds);
            InitialTotalEngulfableCompounds = CachedEngulfableCompounds.Sum(c => c.Value);
        }

        /// <summary>
        ///   The object that has been engulfed.
        /// </summary>
        public EntityReference<IEngulfable> Engulfable { get; }

        public float InitialTotalEngulfableCompounds { get; }

        public Dictionary<Compound, float> CachedEngulfableCompounds { get; }
    }
}
