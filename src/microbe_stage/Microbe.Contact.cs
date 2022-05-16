using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Engulf, Bind/Unbind, Colony,
///   Damage, Kill, Pilus, Membrane
/// </summary>
public partial class Microbe
{
    private SphereShape pseudopodRangeSphereShape = null!;

    /// <summary>
    ///   Contains the pili this microbe has for collision checking
    /// </summary>
    private HashSet<uint> pilusPhysicsShapes = new();

    private uint? engulfedAreaShapeOwner;

    private bool membraneOrganellePositionsAreDirty = true;
    private bool membraneOrganellesWereUpdatedThisFrame;

    private bool destroyed;

    // variables for engulfing
    [JsonProperty]
    private bool previousEngulfMode;

    /// <summary>
    ///   Tracks entities this is touching, for beginning engulfing and cell binding.
    /// </summary>
    private HashSet<IEntity> touchedEntities = new();

    /// <summary>
    ///   Tracks entities this already engulfed.
    /// </summary>
    [JsonProperty]
    private List<EngulfedMaterial> engulfedMaterials = new();

    /// <summary>
    ///   Tracks entities this previously engulfed.
    /// </summary>
    [JsonProperty]
    private List<EngulfedMaterial> ejectedMaterials = new();

    private HashSet<IEngulfable> engulfablesInPseudopodRange = new();

    private MeshInstance pseudopodTarget = null!;

    private PackedScene endosomeScene = null!;
    private SpatialMaterial endosomeMaterial = null!;

    /// <summary>
    ///   The available space to store engulfable materials internally from engulfment.
    /// </summary>
    [JsonProperty]
    private float engulfStorage;

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

    /// <summary>
    ///   Used to log just once when the touched microbe disposed issue happens to reduce log spam
    /// </summary>
    private bool loggedTouchedDisposeIssue;

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
    public EngulfmentStep CurrentEngulfmentStep { get; set; }

    [JsonProperty]
    public EntityReference<Microbe> HostileEngulfer { get; private set; } = new();

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
        if (target.CurrentEngulfmentStep != EngulfmentStep.NotEngulfed)
            return false;

        // Can't engulf recently ejected material, this act as a cooldown
        if (ejectedMaterials.Any(m => m.Material == target))
            return false;

        var microbe = target as Microbe;
        var isMicrobe = microbe != null;

        // Can't engulf already destroyed microbes. We don't use entity references so we need to manually check if
        // something is destroyed or not here (especially now that the Invoke the engulf start callback)
        if (isMicrobe && microbe!.destroyed)
            return false;

        // Can't engulf dead microbes (unlikely to happen but this is fail-safe)
        if (isMicrobe && microbe!.Dead)
            return false;

        // Log error if trying to engulf something that is disposed, we got a crash log trace with an error with that
        // TODO: find out why disposed microbes can be attempted to be engulfed
        try
        {
            if (isMicrobe)
            {
                // Access a Godot property to throw disposed exception
                _ = microbe!.GlobalTransform;
            }
        }
        catch (ObjectDisposedException)
        {
            if (!loggedTouchedDisposeIssue)
            {
                GD.PrintErr("Touched microbe has been disposed before engulfing could start");
                loggedTouchedDisposeIssue = true;
            }

            return false;
        }

        // Limit amount of things that can be engulfed at once
        if (engulfStorage >= Size || engulfStorage + target.Size >= Size)
            return false;

        // Disallow cannibalism
        if (isMicrobe && microbe!.Species == Species)
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

        // Make the render priority of our organelles be on top of the highest possible render priority
        // of the hostile engulfer's organelles
        if (HostileEngulfer.Value != null)
        {
            foreach (var organelle in organelles!)
            {
                var newPriority = Mathf.Clamp(Hex.GetRenderPriority(organelle.Position) +
                    HostileEngulfer.Value.organelles!.MaxRenderPriority, 0, Material.RenderPriorityMax);
                organelle.UpdateRenderPriority(newPriority);
            }
        }
    }

    public void OnEjected()
    {
        // Reset our organelles' render priority back to their original value
        foreach (var organelle in organelles!)
        {
            organelle.UpdateRenderPriority(Hex.GetRenderPriority(organelle.Position));
        }
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
                Constants.COMPOUND_RELEASE_FRACTION;

            compoundsToRelease[type] = amount;
        }

        // Eject some part of the build cost of all the organelles
        foreach (var organelle in organelles!)
        {
            foreach (var entry in organelle.Definition.InitialComposition)
            {
                compoundsToRelease.TryGetValue(entry.Key, out var existing);

                compoundsToRelease[entry.Key] = existing + (entry.Value *
                    Constants.COMPOUND_MAKEUP_RELEASE_FRACTION);
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
                chunkScene, random);

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

        if (endosomeMaterial == null)
            endosomeMaterial = new SpatialMaterial { FlagsTransparent = true };

        endosomeMaterial.AlbedoColor = new Color(CellTypeProperties.Colour, 0.3f);
    }

    private void CheckEngulfShape()
    {
        var wantedRadius = Radius * 5;
        if (pseudopodRangeSphereShape.Radius != wantedRadius)
        {
            pseudopodRangeSphereShape.Radius = wantedRadius;
        }

        if (engulfedAreaShapeOwner == null)
        {
            var newShape = new ConvexPolygonShape();
            engulfedAreaShapeOwner = engulfDetector.CreateShapeOwner(newShape);
            engulfDetector.ShapeOwnerAddShape(engulfedAreaShapeOwner.Value, newShape);
        }

        var existingShape = (ConvexPolygonShape)engulfDetector.ShapeOwnerGetShape(engulfedAreaShapeOwner.Value, 0);

        var wanted = Membrane.ConvexShape;
        if (!existingShape.Points.SequenceEqual(wanted))
        {
            existingShape.Points = wanted;
            engulfDetector.Scale = Membrane.Scale;
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

        // TODO: Add back escaped from engulfment population reward. This is currently not feasible
        // due to new engulfment behavior not allowing things to escape

        for (int i = engulfedMaterials.Count - 1; i >= 0; --i)
        {
            var engulfed = engulfedMaterials[i];

            var body = engulfed.Material as RigidBody;
            if (body == null)
                continue;

            body.Mode = ModeEnum.Static;

            if (engulfed.Material.CurrentEngulfmentStep == EngulfmentStep.FullyDigested)
            {
                engulfed.ValuesToLerp = (null, null, Vector3.One * Mathf.Epsilon);
                StartEngulfmentLerp(engulfed, 1.0f, false);
            }

            if (!engulfed.Interpolate)
                continue;

            // Interpolate values manually to make saving these values possible
            if (LerpEngulfmentProcess(delta, engulfed))
            {
                switch (engulfed.Material.CurrentEngulfmentStep)
                {
                    case EngulfmentStep.BeingEngulfed:
                        CompleteIngestion(engulfed);
                        break;
                    case EngulfmentStep.FullyDigested:
                        engulfed.Material.DestroyDetachAndQueueFree();
                        engulfedMaterials.Remove(engulfed);
                        break;
                    case EngulfmentStep.BeingRegurgitated:
                        engulfed.Endosome.Hide();
                        engulfed.ValuesToLerp = (null, engulfed.OriginalScale, null);
                        StartEngulfmentLerp(engulfed, 1.0f);
                        engulfed.Material.CurrentEngulfmentStep = EngulfmentStep.PreparingEjection;
                        continue;
                    case EngulfmentStep.PreparingEjection:
                        CompleteEjection(engulfed);
                        break;
                }
            }
        }

        for (int i = ejectedMaterials.Count - 1; i >= 0; --i)
        {
            var ejected = ejectedMaterials[i];

            ejected.TimeElapsedSinceEjection += delta;

            if (ejected.TimeElapsedSinceEjection >= Constants.ENGULF_EJECTED_COOLDOWN)
                ejectedMaterials.RemoveAt(i);
        }

        /* DEBUG CODE
        if (state == MicrobeState.Engulf)
        {
            foreach (Spatial engulfable in engulfablesInPseudopodRange)
            {
                pseudopodTarget.Translation = ToLocal(pseudopodTarget.GlobalTransform.origin.LinearInterpolate(engulfable.GlobalTransform.origin, 0.5f * delta));
            }
        }
        else
        {
            pseudopodTarget.Translation = ToLocal(pseudopodTarget.GlobalTransform.origin.LinearInterpolate(GlobalTransform.origin, 0.5f * delta));
        }

        Membrane.EngulfPosition = pseudopodTarget.Translation;
        Membrane.EngulfRadius = ((SphereMesh)pseudopodTarget.Mesh).Radius;
        Membrane.EngulfOffset = 1f;
        */

        previousEngulfMode = State == MicrobeState.Engulf;
    }

    /// <summary>
    ///   Handles the death of this microbe. This queues this object
    ///   for deletion and handles some post-death actions.
    /// </summary>
    private void HandleDeath(float delta)
    {
        // Spawn cell death particles. Don't spawn it if this has been digested as the membrane
        // have already broke up by then and having a bursting particles won't make sense
        if (!deathParticlesSpawned && DigestionProgress <= 0)
        {
            deathParticlesSpawned = true;

            var cellBurstEffectParticles = (CellBurstEffect)cellBurstEffectScene.Instance();
            cellBurstEffectParticles.Translation = Translation;
            cellBurstEffectParticles.Radius = Radius;
            cellBurstEffectParticles.AddToGroup(Constants.TIMED_GROUP);

            GetParent().AddChild(cellBurstEffectParticles);

            // This loop is placed here (which isn't related to the particles but for convenience)
            // so this loop is run only once
            foreach (var engulfed in engulfedMaterials.ToList())
            {
                EjectEngulfable(engulfed.Material);
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

                Invoke.Instance.Perform(() => target.Damage(Constants.PILUS_BASE_DAMAGE, "pilus"));
                return;
            }

            // Pili don't stop engulfing
            if (thisMicrobe.touchedEntities.Add(touchedMicrobe))
            {
                Invoke.Instance.Perform(() =>
                {
                    thisMicrobe.CheckStartEngulfingOnCandidate(touchedMicrobe);
                    thisMicrobe.CheckBinding();
                });
            }

            // Play bump sound if certain total collision impulse is reached (adjusted by mass)
            if (thisMicrobe.collisionForce / Mass > Constants.CONTACT_IMPULSE_TO_BUMP_SOUND)
            {
                Invoke.Instance.Perform(() =>
                    thisMicrobe.PlaySoundEffect("res://assets/sounds/soundeffects/microbe-collision.ogg"));
            }
        }
        else if (body is IEngulfable engulfable)
        {
            if (thisMicrobe.touchedEntities.Add(engulfable))
            {
                thisMicrobe.CheckStartEngulfingOnCandidate(engulfable);
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
            hitMicrobe.touchedEntities.Remove(engulfable);
        }
    }

    private void OnBodyEnteredEngulfArea(Node body)
    {
        if (body == this)
            return;

        // A redundancy in case an engulfable is technically inside this cell's membrane but is not ingested
        // (presumably due to it not touching any of this cell's collision shape), so we refer to the
        // engulf detector area which is shaped as the membrane to check if the engulfable really is
        // inside this cell
        if (body is IEngulfable engulfable)
        {
            CheckStartEngulfingOnCandidate(engulfable);
        }
    }

    private void OnBodyEnteredPseudopodRange(Node body)
    {
        if (body == this)
            return;

        if (body is IEngulfable engulfable)
        {
            engulfablesInPseudopodRange.Add(engulfable);
        }
    }

    private void OnBodyExitedPseudopodRange(Node body)
    {
        if (body is IEngulfable engulfable)
        {
            engulfablesInPseudopodRange.Remove(engulfable);
        }
    }

    /// <summary>
    ///   Ingests (finishes engulfment of) the target into this microbe. Does not check whether the target
    ///   can be engulfed or not.
    /// </summary>
    private void IngestEngulfable(IEngulfable target)
    {
        if (target.CurrentEngulfmentStep is EngulfmentStep.BeingEngulfed or EngulfmentStep.Ingested ||
            target.EntityNode.GetParent() == this)
        {
            return;
        }

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ingested
            return;
        }

        touchedEntities.Remove(target);

        target.HostileEngulfer.Value = this;
        target.CurrentEngulfmentStep = EngulfmentStep.BeingEngulfed;
        target.OnEngulfed();

        engulfStorage += target.Size;

        // Disable collisions
        body.CollisionLayer = 0;
        body.CollisionMask = 0;

        var temp = body.GlobalTransform;
        body.ReParent(this);
        body.GlobalTransform = temp;

        // Below is for figuring out where to place the ingested material inside the membrane and calculated
        // accordingly to hopefully minimize any part of the material sticking out the membrane.
        // Note: extremely long and thin objects might still stick out

        var targetRadiusNormalized = Mathf.Clamp(target.Radius / Radius, 0.0f, 1.0f);

        var nearestPointOfMembraneToTarget = Membrane.GetVectorTowardsNearestPointOfMembrane(
            body.Translation.x, body.Translation.z);

        // The point nearest to the membrane calculation doesn't take being bacteria into account
        if (CellTypeProperties.IsBacteria)
            nearestPointOfMembraneToTarget *= 0.5f;

        // From the calculated nearest point of membrane above we then linearly interpolate it by the engulfed's
        // normalized radius to this cell's center in order to "shrink" the point relative to this cell's origin.
        // This will then act as a "maximum extent/edge" that qualifies as the interior of the engulfer's membrane
        var viableStoringAreaEdge = nearestPointOfMembraneToTarget.LinearInterpolate(
            Vector3.Zero, targetRadiusNormalized);

        // Get the final storing position by taking a value between this cell's center and the storing area edge.
        // This would lessen the possibility of engulfed things getting bunched up in the same position.
        var finalPosition = new Vector3(
            random.Next(0.0f, viableStoringAreaEdge.x),
            body.Translation.y,
            random.Next(0.0f, viableStoringAreaEdge.z));

        var Aabb = target.EntityGraphics.GetAabb().Size;

        // In the case of flat mesh (like membrane) we don't want the endosome to end up completely flat
        // as it can cause unwanted visual glitch
        var AabbClamped = new Vector3(Aabb.x, Mathf.Clamp(Aabb.y, Mathf.Epsilon, Mathf.Inf), Aabb.z);

        var engulfedMaterial = new EngulfedMaterial(target)
        {
            ValuesToLerp = (finalPosition, body.Scale / 2, AabbClamped),
            OriginalScale = body.Scale,
            OriginalRenderPriority = target.EntityMaterial.RenderPriority,
        };

        StartEngulfmentLerp(engulfedMaterial, 3.0f);

        engulfedMaterials.Add(engulfedMaterial);

        // Set the render priority of the ingested object higher than all of our organelles' so it
        // stays visible on top of our insides
        target.EntityMaterial.RenderPriority += organelles!.MaxRenderPriority + 1;

        // Form endosome
        engulfedMaterial.Endosome = endosomeScene.Instance<Endosome>();
        engulfedMaterial.Endosome.Scale = Vector3.Zero;
        engulfedMaterial.Endosome.Transform = engulfedMaterial.Material.EntityGraphics.Transform.Scaled(Vector3.Zero);
        engulfedMaterial.Material.EntityGraphics.AddChild(engulfedMaterial.Endosome);

        engulfedMaterial.Endosome.Mesh.MaterialOverride = endosomeMaterial;
        engulfedMaterial.Endosome.Mesh.MaterialOverride.RenderPriority = target.EntityMaterial.RenderPriority + 1;
    }

    /// <summary>
    ///   Ejects (regurgitate) an ingested material from this microbe.
    /// </summary>
    private void EjectEngulfable(IEngulfable target)
    {
        if (target.CurrentEngulfmentStep is EngulfmentStep.BeingRegurgitated or EngulfmentStep.NotEngulfed ||
            target.EntityNode.GetParent() != this)
        {
            return;
        }

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ejected
            return;
        }

        var engulfedObject = engulfedMaterials.Find(e => e.Material == target);
        if (engulfedObject == null)
            return;

        target.HostileEngulfer.Value = null;
        target.CurrentEngulfmentStep = EngulfmentStep.BeingRegurgitated;
        engulfStorage -= target.Size;

        var nearestPointOfMembraneToTarget = Membrane.GetVectorTowardsNearestPointOfMembrane(
            body.Translation.x, body.Translation.z);

        // The point nearest to the membrane calculation doesn't take being bacteria into account
        if (CellTypeProperties.IsBacteria)
            nearestPointOfMembraneToTarget *= 0.5f;

        // If engulfer cell is dead (us) or the engulfed is positioned outside any of our closest membrane, immediately
        // eject it without animation
        // TODO: Asses performance cost in massive cells?
        if (Dead || !Membrane.Contains(body.Translation.x, body.Translation.z))
        {
            CompleteEjection(engulfedObject);
            body.Scale = engulfedObject.OriginalScale;
            engulfedMaterials.Remove(engulfedObject);
            return;
        }

        // Animate object move to the nearest point of the membrane
        engulfedObject.ValuesToLerp = (nearestPointOfMembraneToTarget, null, Vector3.One * Mathf.Epsilon);
        StartEngulfmentLerp(engulfedObject, 3.0f);

        // Engulfed object will be removed from the list in CompleteEjection
    }

    private bool CanBindToMicrobe(IEntity other)
    {
        if (other is Microbe microbe)
        {
            // Cannot hijack the player, other species or other colonies (TODO: yet)
            return !microbe.Dead && !microbe.IsPlayerMicrobe && microbe.Colony == null && microbe.Species == Species;
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

        var other = touchedEntities.FirstOrDefault(CanBindToMicrobe);

        // If there is no touching microbe that can bind, no need to invoke binding.
        if (other == null)
            return;

        // Invoke this on the next frame to avoid crashing when adding a third cell
        Invoke.Instance.Queue(BeginBind);
    }

    private void BeginBind()
    {
        var other = touchedEntities.FirstOrDefault(CanBindToMicrobe) as Microbe;

        if (other == null)
        {
            GD.PrintErr("Touched eligible microbe has disappeared before binding could start");
            return;
        }

        touchedEntities.Remove(other);

        try
        {
            other.touchedEntities.Remove(this);

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
    private void CheckStartEngulfingOnCandidate(IEngulfable engulfable)
    {
        if (State != MicrobeState.Engulf)
            return;

        if (CanEngulf(engulfable))
        {
            IngestEngulfable(engulfable);
        }
        else if (engulfStorage >= Size || engulfStorage + engulfable.Size >= Size)
        {
            OnEngulfmentStorageFull?.Invoke(this);
        }
    }

    private bool LerpEngulfmentProcess(float delta, EngulfedMaterial engulfed)
    {
        var body = (RigidBody)engulfed.Material;

        if (engulfed.AnimationTimeElapsed < engulfed.LerpDuration)
        {
            engulfed.AnimationTimeElapsed += delta;

            var fraction = engulfed.AnimationTimeElapsed / engulfed.LerpDuration;

            // Smoothstep
            fraction = fraction * fraction * fraction * (fraction * (6f * fraction - 15f) + 10f);

            if (engulfed.ValuesToLerp.Translation.HasValue)
            {
                body.Translation = body.Translation.LinearInterpolate(engulfed.ValuesToLerp.Translation.Value,
                    fraction);
            }

            if (engulfed.ValuesToLerp.Scale.HasValue)
                body.Scale = body.Scale.LinearInterpolate(engulfed.ValuesToLerp.Scale.Value, fraction);

            if (engulfed.ValuesToLerp.EndosomeScale.HasValue)
                engulfed.Endosome.Scale = engulfed.Endosome.Scale.LinearInterpolate(
                    engulfed.ValuesToLerp.EndosomeScale.Value, fraction);

            return false;
        }

        // Snap values
        if (engulfed.ValuesToLerp.Translation.HasValue)
            body.Translation = engulfed.ValuesToLerp.Translation.Value;

        if (engulfed.ValuesToLerp.Scale.HasValue)
            body.Scale = engulfed.ValuesToLerp.Scale.Value;

        if (engulfed.ValuesToLerp.EndosomeScale.HasValue)
            engulfed.Endosome.Scale = engulfed.ValuesToLerp.EndosomeScale.Value;

        StopEngulfmentLerp(engulfed);

        return true;
    }

    private void StartEngulfmentLerp(EngulfedMaterial material, float duration, bool resetElapsedTime = true)
    {
        if (resetElapsedTime)
            material.AnimationTimeElapsed = 0;

        material.LerpDuration = duration;
        material.Interpolate = true;
    }

    private void StopEngulfmentLerp(EngulfedMaterial material)
    {
        material.AnimationTimeElapsed = 0;
        material.Interpolate = false;
    }

    private void CompleteIngestion(EngulfedMaterial engulfed)
    {
        var engulfable = engulfed.Material;

        engulfable.CurrentEngulfmentStep = EngulfmentStep.Ingested;

        // In contrast with CompleteEjection, we call OnIngested immediately when ingestion is triggered

        touchedEntities.Remove(engulfable);
    }

    private void CompleteEjection(EngulfedMaterial engulfed)
    {
        var engulfable = engulfed.Material;

        engulfedMaterials.Remove(engulfed);
        ejectedMaterials.Add(engulfed);

        engulfable.CurrentEngulfmentStep = EngulfmentStep.NotEngulfed;
        engulfable.OnEjected();

        // Reset render priority
        engulfable.EntityMaterial.RenderPriority = engulfed.OriginalRenderPriority;

        engulfed.Endosome?.DetachAndQueueFree();

        // Ignore possible invalid cast as the engulfed node should be a rigidbody either way
        var body = (RigidBody)engulfable;

        body.Mode = ModeEnum.Rigid;

        // Reparent to world node
        var temp = body.GlobalTransform;
        body.ReParent(GetStageAsParent());
        body.GlobalTransform = temp;

        // Set to default microbe collision layer and mask values
        body.CollisionLayer = 3;
        body.CollisionMask = 3;

        // Apply outwards ejection force
        body.ApplyCentralImpulse(Transform.origin.DirectionTo(body.Transform.origin) * body.Mass * 20);
    }

    /// <summary>
    ///   Holds cached and extra informations to work on in addition to the engulfed object
    /// </summary>
    private class EngulfedMaterial
    {
        [JsonConstructor]
        public EngulfedMaterial(IEngulfable material)
        {
            Material = material;
            AvailableEngulfableCompounds = material.CalculateDigestibleCompounds();
            InitialTotalEngulfableCompounds = AvailableEngulfableCompounds.Sum(c => c.Value);
        }

        /// <summary>
        ///   The object that has been engulfed.
        /// </summary>
        public IEngulfable Material { get; private set; }

        public Endosome Endosome { get; set; } = null!;

        public Dictionary<Compound, float> AvailableEngulfableCompounds { get; private set; }

        public float InitialTotalEngulfableCompounds { get; private set; }

        public bool Interpolate { get; set; }

        public float LerpDuration { get; set; }

        public float AnimationTimeElapsed { get; set; }

        public float TimeElapsedSinceEjection { get; set; }

        public (Vector3? Translation, Vector3? Scale, Vector3? EndosomeScale) ValuesToLerp { get; set; }

        public Vector3 OriginalScale { get; set; }

        public int OriginalRenderPriority { get; set; }
    }
}
