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
#pragma warning disable CA2213
    private PackedScene endosomeScene = null!;

    private PackedScene cellBurstEffectScene = null!;
#pragma warning restore CA2213

    // private SphereShape pseudopodRangeSphereShape = null!;

    /// <summary>
    ///   Contains the pili this microbe has for collision checking
    /// </summary>
    private HashSet<uint> pilusPhysicsShapes = new();

    private bool membraneOrganellePositionsAreDirty = true;
    private bool membraneOrganellesWereUpdatedThisFrame;

    private bool destroyed;

    [JsonProperty]
    private float escapeInterval;

    [JsonProperty]
    private bool hasEscaped;

    /// <summary>
    ///   Tracks entities this is touching, for beginning engulfing and cell binding.
    /// </summary>
    private HashSet<IEntity> touchedEntities = new();

    /// <summary>
    ///   Tracks entities this is trying to engulf.
    /// </summary>
    [JsonProperty]
    private HashSet<IEngulfable> attemptingToEngulf = new();

    /// <summary>
    ///   Tracks entities this already engulfed.
    /// </summary>
    [JsonProperty]
    private List<EngulfedObject> engulfedObjects = new();

    /// <summary>
    ///   Tracks entities this has previously engulfed.
    /// </summary>
    [JsonProperty]
    private List<EngulfedObject> expelledObjects = new();

    // private HashSet<IEngulfable> engulfablesInPseudopodRange = new();

    // private MeshInstance pseudopodTarget = null!;

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

    /// <summary>
    ///   This determines how much time is left (in seconds) until this cell can take damage again after becoming
    ///   invulnerable due to a damage source. This was added to balance pili but might extend to more sources.
    /// </summary>
    [JsonProperty]
    private float invulnerabilityDuration;

    [JsonProperty]
    private bool deathParticlesSpawned;

    /// <summary>
    ///   Used to log just once when the touched microbe disposed issue happens to reduce log spam
    /// </summary>
    private bool loggedTouchedDisposeIssue;

    [JsonProperty]
    private MicrobeState state;

    public enum EngulfCheckResult
    {
        Ok,
        NotInEngulfMode,
        RecentlyExpelled,
        TargetDead,
        TargetTooBig,
        IngestedMatterFull,
        CannotCannibalize,
        TargetInvulnerable,
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
    public PhagocytosisPhase PhagocytosisStep { get; set; }

    /// <summary>
    ///   The amount of space all of the currently engulfed objects occupy in the cytoplasm. This is used to determine
    ///   whether a cell can ingest any more objects or not due to being full.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     In a more technical sense, this is the accumulated <see cref="IEngulfable.EngulfSize"/> from all
    ///     the ingested objects. Maximum should be this cell's own <see cref="EngulfSize"/>.
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public float UsedIngestionCapacity { get; private set; }

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
        get => Colony?.State ?? state;
        set
        {
            if (state == value)
                return;

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
    public float EngulfSize
    {
        get
        {
            // Scale with digested progress
            var size = HexCount * (1 - DigestedAmount);

            if (CellTypeProperties.IsBacteria)
                return size * 0.5f;

            return size;
        }
    }

    /// <summary>
    ///   Just like <see cref="ICellProperties.CanEngulf"/> but decoupled from Species and is based on the local
    ///   condition of the microbe instead.
    /// </summary>
    /// <returns>True if this cell fills all the requirements needed to enter engulf mode.</returns>
    [JsonIgnore]
    public bool CanEngulf => !Membrane.Type.CellWall;

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
    public Action<Microbe, Microbe>? OnIngestedByHostile { get; set; }

    [JsonProperty]
    public Action<Microbe, IEngulfable>? OnSuccessfulEngulfment { get; set; }

    [JsonProperty]
    public Action<Microbe>? OnEngulfmentStorageFull { get; set; }

    [JsonProperty]
    public Action<Microbe, IHUDMessage>? OnNoticeMessage { get; set; }

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
    ///   Give this microbe a specified amount of invulnerability time. Overrides previous value.
    ///   NOTE: Not all damage sources apply invulnerability, check method usages.
    /// </summary>
    public void MakeInvulnerable(float duration)
    {
        invulnerabilityDuration = duration;
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

        // Damage reduction is only wanted for non-starving damage
        bool canApplyDamageReduction = true;

        if (source is "toxin" or "oxytoxy")
        {
            // Play the toxin sound
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-toxin-damage.ogg");

            // TODO: fix this, currently "toxin" is used both by microbes and chunks, as well as damage from ingested
            // toxins
            // OnNoticeMessage?.Invoke(this,
            //     new SimpleHUDMessage(TranslationServer.Translate("NOTICE_DAMAGED_BY_ENVIRONMENTAL_TOXIN")));

            // Divide damage by toxin resistance
            amount /= CellTypeProperties.MembraneType.ToxinResistance;
        }
        else if (source == "pilus")
        {
            if (invulnerabilityDuration > 0)
                return;

            // Play the pilus sound
            PlaySoundEffect("res://assets/sounds/soundeffects/pilus_puncture_stab.ogg", 4.0f);

            // Give immunity to prevent massive damage at some angles
            // https://github.com/Revolutionary-Games/Thrive/issues/3267
            MakeInvulnerable(Constants.PILUS_INVULNERABLE_TIME);

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
            canApplyDamageReduction = false;

            OnNoticeMessage?.Invoke(this,
                new SimpleHUDMessage(TranslationServer.Translate("NOTICE_DAMAGED_BY_NO_ATP"),
                    DisplayDuration.Short));
        }
        else if (source == "ice")
        {
            PlayNonPositionalSoundEffect("res://assets/sounds/soundeffects/microbe-ice-damage.ogg", 0.5f);

            // Divide damage by physical resistance
            amount /= CellTypeProperties.MembraneType.PhysicalResistance;
        }

        if (!CellTypeProperties.IsBacteria && canApplyDamageReduction)
        {
            amount /= 2;
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
    ///   Overrides this microbe's health. Used by testing code to ensure microbes don't die when not wanted
    /// </summary>
    /// <param name="newHitpoints">
    ///   The new health to set the microbe to. Setting to 0 won't immediately kill the microbe
    /// </param>
    public void TestOverrideHitpoints(float newHitpoints)
    {
        Hitpoints = Mathf.Clamp(newHitpoints, 0, MaxHitpoints);
    }

    /// <summary>
    ///   Returns the check result whether this microbe can engulf the target
    /// </summary>
    public EngulfCheckResult CanEngulfObject(IEngulfable target)
    {
        if (target.PhagocytosisStep != PhagocytosisPhase.None)
            return EngulfCheckResult.NotInEngulfMode;

        // Membranes with Cell Wall cannot engulf
        if (!CanEngulf)
            return EngulfCheckResult.NotInEngulfMode;

        // Can't engulf recently ejected objects, this act as a cooldown
        if (expelledObjects.Any(m => m.Object == target))
            return EngulfCheckResult.RecentlyExpelled;

        var targetAsMicrobe = target as Microbe;

        // Can't engulf already destroyed microbes. We don't use entity references so we need to manually check if
        // something is destroyed or not here (especially now that the Invoke the engulf start callback)
        if (targetAsMicrobe != null && targetAsMicrobe.destroyed)
            return EngulfCheckResult.TargetDead;

        // Can't engulf dead microbes (unlikely to happen but this is fail-safe)
        if (targetAsMicrobe != null && targetAsMicrobe.Dead)
            return EngulfCheckResult.TargetDead;

        // Log error if trying to engulf something that is disposed, we got a crash log trace with an error with that
        // TODO: find out why disposed microbes can be attempted to be engulfed
        try
        {
            if (targetAsMicrobe != null)
            {
                // Access a Godot property to throw disposed exception
                _ = targetAsMicrobe.GlobalTransform;
            }
        }
        catch (ObjectDisposedException)
        {
            if (!loggedTouchedDisposeIssue)
            {
                GD.PrintErr("Touched microbe has been disposed before engulfing could start");
                loggedTouchedDisposeIssue = true;
            }

            return EngulfCheckResult.TargetDead;
        }

        // The following checks are in a specific order to make sure the fail reporting logic gives sensible results

        // Disallow cannibalism
        if (targetAsMicrobe != null && targetAsMicrobe.Species == Species)
            return EngulfCheckResult.CannotCannibalize;

        // Needs to be big enough to engulf
        if (EngulfSize < target.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ)
            return EngulfCheckResult.TargetTooBig;

        // Limit amount of things that can be engulfed at once
        if (UsedIngestionCapacity >= EngulfSize || UsedIngestionCapacity + target.EngulfSize >= EngulfSize)
            return EngulfCheckResult.IngestedMatterFull;

        // Too many things attempted to be pulled in at once
        if (UsedIngestionCapacity + attemptingToEngulf.Sum(e => e.EngulfSize) + target.EngulfSize >= EngulfSize)
            return EngulfCheckResult.IngestedMatterFull;

        // Godmode grants player complete engulfment invulnerability
        if (targetAsMicrobe != null && targetAsMicrobe.IsPlayerMicrobe && CheatManager.GodMode)
            return EngulfCheckResult.TargetInvulnerable;

        return EngulfCheckResult.Ok;
    }

    /// <summary>
    ///   Returns true if this microbe OR the colony this microbe is part of has the capability to engulf.
    /// </summary>
    public bool CanEngulfInColony()
    {
        if (Colony != null)
            return Colony.CanEngulf;

        return CanEngulf;
    }

    public void OnAttemptedToBeEngulfed()
    {
        Membrane.WigglyNess = 0;

        UnreadyToReproduce();

        // Make the render priority of our organelles be on top of the highest possible render priority
        // of the hostile engulfer's organelles
        var hostile = HostileEngulfer.Value;
        if (hostile != null)
        {
            foreach (var organelle in organelles!)
            {
                var newPriority = Mathf.Clamp(Hex.GetRenderPriority(organelle.Position) +
                    hostile.OrganelleMaxRenderPriority, 0, Material.RenderPriorityMax);
                organelle.UpdateRenderPriority(newPriority);
            }
        }

        Colony?.RemoveFromColony(this);

        // Just in case player is engulfed again after escaping
        playerEngulfedDeathTimer = 0;
    }

    public void OnIngestedFromEngulfment()
    {
        OnIngestedByHostile?.Invoke(this, HostileEngulfer.Value!);
    }

    public void OnExpelledFromEngulfment()
    {
        var hostile = HostileEngulfer.Value;

        // Reset wigglyness
        ApplyMembraneWigglyness();

        // Reset our organelles' render priority back to their original values
        foreach (var organelle in organelles!)
        {
            organelle.UpdateRenderPriority(Hex.GetRenderPriority(organelle.Position));
        }

        if (DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD)
        {
            // Cell is too damaged from digestion, can't live in open environment and is considered dead
            // Kill() is not called here because it's already called during partial digestion
            OnDestroyed();
            var droppedChunks = OnKilled().ToList();

            if (hostile == null)
                return;

            foreach (var chunk in droppedChunks)
            {
                var direction = hostile.Transform.origin.DirectionTo(chunk.Transform.origin);
                chunk.Translation += direction *
                    Constants.EJECTED_PARTIALLY_DIGESTED_CELL_CORPSE_CHUNKS_SPAWN_OFFSET;

                var impulse = direction * chunk.Mass * Constants.ENGULF_EJECTION_FORCE;

                // Apply outwards ejection force
                chunk.ApplyCentralImpulse(impulse + LinearVelocity);
            }
        }
        else
        {
            hasEscaped = true;
            escapeInterval = 0;
            playerEngulfedDeathTimer = 0;
        }
    }

    public void ClearEngulfedObjects()
    {
        foreach (var engulfed in engulfedObjects.ToList())
        {
            if (engulfed.Object.Value != null)
            {
                engulfedObjects.Remove(engulfed);
                engulfed.Object.Value.DestroyDetachAndQueueFree();
            }

            engulfed.Phagosome.Value?.DestroyDetachAndQueueFree();
        }

        engulfedObjects.Clear();
    }

    /// <summary>
    ///   Returns true if this microbe is currently in the process of ingesting engulfables.
    /// </summary>
    public bool IsPullingInEngulfables()
    {
        return attemptingToEngulf.Any();
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
    ///   Public because it needs to be called by external organelles only.
    ///   Not meant for other uses.
    /// </summary>
    public void SendOrganellePositionsToMembrane()
    {
        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized first");

        var organellePositions = new List<Vector2>();

        foreach (var entry in organelles.Organelles)
        {
            // The membrane needs hex positions to handle cells with multihex organelles
            foreach (var hex in entry.Definition.GetRotatedHexes(entry.Orientation))
            {
                var hexCartesian = Hex.AxialToCartesian(entry.Position + hex);
                organellePositions.Add(new Vector2(hexCartesian.x, hexCartesian.z));
            }
        }

        Membrane.OrganellePositions = organellePositions;
        Membrane.Dirty = true;
        membraneOrganellePositionsAreDirty = false;
    }

    /// <summary>
    ///   Instantly kills this microbe and queues this entity to be destroyed
    /// </summary>
    /// <returns>
    ///   The dropped corpse chunks. Null if this cell is already dead or is engulfed.
    /// </returns>
    public IEnumerable<FloatingChunk>? Kill()
    {
        if (Dead)
            return null;

        Dead = true;

        OnDeath?.Invoke(this);
        ModLoader.ModInterface.TriggerOnMicrobeDied(this, IsPlayerMicrobe);

        // If being phagocytized don't continue further because the entity reference is still needed to
        // maintain related functions, also dropping corpse chunks won't make sense while inside a cell
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return null;

        OnDestroyed();

        // Post-death handling is done in HandleDeath

        return OnKilled();
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

    public void OnMouseEnter(RaycastResult result)
    {
        var microbe = GetMicrobeFromShape(result.Shape);

        if (microbe != null)
            microbe.IsHoveredOver = true;
    }

    public void OnMouseExit(RaycastResult result)
    {
        var microbe = GetMicrobeFromShape(result.Shape);

        if (microbe != null)
            microbe.IsHoveredOver = false;
    }

    internal void OnColonyMemberRemoved(Microbe microbe)
    {
        cachedColonyRotationMultiplier = null;

        if (microbe == this)
        {
            OnUnbound?.Invoke(this);

            if (PhagocytosisStep == PhagocytosisPhase.None)
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
        cachedColonyRotationMultiplier = null;

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
        GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
            Constants.CREATURE_SCAVENGE_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_SCAVENGE"));
    }

    internal void SuccessfulKill()
    {
        GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
            Constants.CREATURE_KILL_POPULATION_GAIN,
            TranslationServer.Translate("SUCCESSFUL_KILL"));
    }

    /// <summary>
    ///   Operations that should be done when this cell is killed. ONLY use this independently of <see cref="Kill"/>
    ///   if you've already made sure that this microbe is marked as dead since this doesn't do that.
    /// </summary>
    /// <returns>
    ///   The dropped corpse chunks.
    /// </returns>
    private IEnumerable<FloatingChunk> OnKilled()
    {
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

                // If this microbe is in a colony, send each of its colony members as a collision exception
                // so the projectile isn't blocked by the microbe's own colony
                var agent = SpawnHelpers.SpawnAgent(props, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT,
                    Constants.EMITTED_AGENT_LIFETIME,
                    Translation, direction, GetStageAsParent(),
                    agentScene, this, Colony?.ColonyMembers);

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

                // Only add up if there's still some compounds left, otherwise
                // we're releasing compounds out of thin air.
                if (existing > 0)
                {
                    compoundsToRelease[entry.Key] = existing + (entry.Value *
                        Constants.COMPOUND_MAKEUP_RELEASE_FRACTION);
                }
            }
        }

        CalculateBonusDigestibleGlucose(compoundsToRelease);

        // Queues either 1 corpse chunk or a factor of the hexes
        int chunksToSpawn = Math.Max(1, HexCount / Constants.CORPSE_CHUNK_DIVISOR);

        var droppedCorpseChunks = new HashSet<FloatingChunk>(chunksToSpawn);

        var chunkScene = SpawnHelpers.LoadChunkScene();

        // An enumerator to step through all available organelles in a random order when making chunks
        using var organellesAvailableEnumerator = organelles.OrderBy(_ => random.Next()).GetEnumerator();

        // The default model for chunks is the cytoplasm model in case there isn't a model left in the species
        var defaultChunkScene = SimulationParameters.Instance
                .GetOrganelleType(Constants.DEFAULT_CHUNK_MODEL_NAME).LoadedCorpseChunkScene ??
            throw new Exception("No default chunk scene");

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
                    Amount = (entry.Value / (random.Next(amount / 3.0f, amount) *
                        Constants.CHUNK_ENGULF_COMPOUND_DIVISOR)) * Constants.CORPSE_COMPOUND_COMPENSATION,
                };

                chunkType.Compounds[entry.Key] = compoundValue;
            }

            chunkType.Meshes = new List<ChunkConfiguration.ChunkScene>();

            var sceneToUse = new ChunkConfiguration.ChunkScene
            {
                LoadedScene = defaultChunkScene,
            };

            // Will only loop if there are still organelles available
            while (organellesAvailableEnumerator.MoveNext() && organellesAvailableEnumerator.Current != null)
            {
                if (!string.IsNullOrEmpty(organellesAvailableEnumerator.Current.Definition.CorpseChunkScene))
                {
                    sceneToUse.LoadedScene =
                        organellesAvailableEnumerator.Current.Definition.LoadedCorpseChunkScene;
                }
                else if (!string.IsNullOrEmpty(organellesAvailableEnumerator.Current.Definition.DisplayScene))
                {
                    sceneToUse.LoadedScene = organellesAvailableEnumerator.Current.Definition.LoadedScene;
                    sceneToUse.SceneModelPath =
                        organellesAvailableEnumerator.Current.Definition.DisplaySceneModelPath;
                }
                else
                {
                    continue;
                }

                if (sceneToUse.LoadedScene != null)
                    break;
            }

            if (sceneToUse.LoadedScene == null)
                throw new Exception("loaded scene is null");

            chunkType.Meshes.Add(sceneToUse);

            // Finally spawn a chunk with the settings
            var chunk = SpawnHelpers.SpawnChunk(chunkType, Translation + positionAdded, GetStageAsParent(),
                chunkScene, random);
            droppedCorpseChunks.Add(chunk);

            // Add to the spawn system to make these chunks limit possible number of entities
            spawnSystem!.AddEntityToTrack(chunk);

            ModLoader.ModInterface.TriggerOnChunkSpawned(chunk, false);
        }

        // Subtract population
        if (!IsPlayerMicrobe && !Species.PlayerSpecies)
        {
            GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
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

        return droppedCorpseChunks;
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

        foreach (var engulfed in engulfedObjects)
        {
            if (engulfed.Phagosome.Value != null)
                engulfed.Phagosome.Value.Tint = CellTypeProperties.Colour;
        }
    }

    private void CheckEngulfShape()
    {
        /*
        var wantedRadius = Radius * 5;
        if (pseudopodRangeSphereShape.Radius != wantedRadius)
        {
            pseudopodRangeSphereShape.Radius = wantedRadius;
        }
        */
    }

    /// <summary>
    ///   Decrease the remaining invulnerability time
    /// </summary>
    private void HandleInvulnerabilityDecay(float delta)
    {
        if (invulnerabilityDuration > 0)
        {
            invulnerabilityDuration -= delta;
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
        var actuallyEngulfing = State == MicrobeState.Engulf && CanEngulf;

        if (actuallyEngulfing)
        {
            // Drain atp
            var cost = Constants.ENGULFING_ATP_COST_PER_SECOND * delta;

            if (Compounds.TakeCompound(atp, cost) < cost - 0.001f || PhagocytosisStep != PhagocytosisPhase.None)
            {
                State = MicrobeState.Normal;
            }
        }
        else
        {
            attemptingToEngulf.Clear();
        }

        // Play sound
        if (actuallyEngulfing)
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
        if (actuallyEngulfing)
        {
            MovementFactor /= Constants.ENGULFING_MOVEMENT_DIVISION;
        }

        // Still considered to be chased for CREATURE_ESCAPE_INTERVAL milliseconds
        if (hasEscaped)
        {
            escapeInterval += delta;
            if (escapeInterval >= Constants.CREATURE_ESCAPE_INTERVAL)
            {
                hasEscaped = false;
                escapeInterval = 0;

                GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
                    Constants.CREATURE_ESCAPE_POPULATION_GAIN,
                    TranslationServer.Translate("ESCAPE_ENGULFING"));
            }
        }

        for (int i = engulfedObjects.Count - 1; i >= 0; --i)
        {
            var engulfedObject = engulfedObjects[i];

            var engulfable = engulfedObject.Object.Value;

            // ReSharper disable once UseNullPropagation
            if (engulfable == null)
                continue;

            var body = engulfable as RigidBody;
            if (body == null)
            {
                attemptingToEngulf.Remove(engulfable);
                engulfedObjects.Remove(engulfedObject);
                continue;
            }

            body.Mode = ModeEnum.Static;

            if (engulfable.PhagocytosisStep == PhagocytosisPhase.Digested)
            {
                engulfedObject.TargetValuesToLerp = (null, null, Vector3.One * Mathf.Epsilon);
                StartBulkTransport(engulfedObject, 1.5f, false);
            }

            if (!engulfedObject.Interpolate)
                continue;

            if (AnimateBulkTransport(delta, engulfedObject))
            {
                switch (engulfable.PhagocytosisStep)
                {
                    case PhagocytosisPhase.Ingestion:
                        CompleteIngestion(engulfedObject);
                        break;
                    case PhagocytosisPhase.Digested:
                        engulfable.DestroyAndQueueFree();
                        engulfedObjects.Remove(engulfedObject);
                        break;
                    case PhagocytosisPhase.Exocytosis:
                        engulfedObject.Phagosome.Value?.Hide();
                        engulfedObject.TargetValuesToLerp = (null, engulfedObject.OriginalScale, null);
                        StartBulkTransport(engulfedObject, 1.0f);
                        engulfable.PhagocytosisStep = PhagocytosisPhase.Ejection;
                        continue;
                    case PhagocytosisPhase.Ejection:
                        CompleteEjection(engulfedObject);
                        break;
                }
            }
        }

        foreach (var expelled in expelledObjects)
            expelled.TimeElapsedSinceEjection += delta;

        expelledObjects.RemoveAll(e => e.TimeElapsedSinceEjection >= Constants.ENGULF_EJECTED_COOLDOWN);

        /* Membrane engulf stretch debug code
        if (state == MicrobeState.Engulf)
        {
            foreach (Spatial engulfable in engulfablesInPseudopodRange)
            {
                pseudopodTarget.Translation = ToLocal(pseudopodTarget.GlobalTransform.origin.LinearInterpolate(
                    engulfable.GlobalTransform.origin, 0.5f * delta));
            }
        }
        else
        {
            pseudopodTarget.Translation = ToLocal(
                pseudopodTarget.GlobalTransform.origin.LinearInterpolate(GlobalTransform.origin, 0.5f * delta));
        }

        Membrane.EngulfPosition = pseudopodTarget.Translation;
        Membrane.EngulfRadius = ((SphereMesh)pseudopodTarget.Mesh).Radius;
        Membrane.EngulfOffset = 1.0f;
        */
    }

    /// <summary>
    ///   Handles the death of this microbe. This queues this object
    ///   for deletion and handles some post-death actions.
    /// </summary>
    private void HandleDeath(float delta)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        // Spawn cell death particles.
        if (!deathParticlesSpawned && DigestedAmount <= 0)
        {
            deathParticlesSpawned = true;

            var cellBurstEffectParticles = (CellBurstEffect)cellBurstEffectScene.Instance();
            cellBurstEffectParticles.Translation = Translation;
            cellBurstEffectParticles.Radius = Radius;
            cellBurstEffectParticles.AddToGroup(Constants.TIMED_GROUP);

            GetParent().AddChild(cellBurstEffectParticles);

            // This loop is placed here (which isn't related to the particles but for convenience)
            // so this loop is run only once
            foreach (var engulfed in engulfedObjects.ToList())
            {
                if (engulfed.Object.Value != null)
                    EjectEngulfable(engulfed.Object.Value);
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

            if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
                hitMicrobe.attemptingToEngulf.Remove(engulfable);
        }
    }

    /*
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
    */

    /// <summary>
    ///   Attempts to engulf the given target into the cytoplasm. Does not check whether the target
    ///   can be engulfed or not.
    /// </summary>
    private void IngestEngulfable(IEngulfable target, float animationSpeed = 2.0f)
    {
        if (target.PhagocytosisStep != PhagocytosisPhase.None)
            return;

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ingested
            return;
        }

        attemptingToEngulf.Add(target);
        touchedEntities.Remove(target);

        target.HostileEngulfer.Value = this;
        target.PhagocytosisStep = PhagocytosisPhase.Ingestion;

        body.ReParentWithTransform(this);

        // Below is for figuring out where to place the object attempted to be engulfed inside the cytoplasm,
        // calculated accordingly to hopefully minimize any part of the object sticking out the membrane.
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
        var ingestionPoint = new Vector3(
            random.Next(0.0f, viableStoringAreaEdge.x),
            body.Translation.y,
            random.Next(0.0f, viableStoringAreaEdge.z));

        var boundingBoxSize = target.EntityGraphics.GetAabb().Size;

        // In the case of flat mesh (like membrane) we don't want the endosome to end up completely flat
        // as it can cause unwanted visual glitch
        if (boundingBoxSize.y < Mathf.Epsilon)
            boundingBoxSize = new Vector3(boundingBoxSize.x, 0.1f, boundingBoxSize.z);

        // Form phagosome
        var phagosome = endosomeScene.Instance<Endosome>();
        phagosome.Transform = target.EntityGraphics.Transform.Scaled(Vector3.Zero);
        phagosome.Tint = CellTypeProperties.Colour;
        phagosome.RenderPriority = target.RenderPriority + engulfedObjects.Count + 1;
        target.EntityGraphics.AddChild(phagosome);

        var engulfedObject = new EngulfedObject(target, phagosome)
        {
            TargetValuesToLerp = (ingestionPoint, body.Scale / 2, boundingBoxSize),
            OriginalScale = body.Scale,
            OriginalRenderPriority = target.RenderPriority,
            OriginalCollisionLayer = body.CollisionLayer,
            OriginalCollisionMask = body.CollisionMask,
        };

        engulfedObjects.Add(engulfedObject);

        // We want the ingested material to be always visible over the organelles
        target.RenderPriority += OrganelleMaxRenderPriority + 1;

        // Disable collisions
        body.CollisionLayer = 0;
        body.CollisionMask = 0;

        foreach (string group in engulfedObject.OriginalGroups)
        {
            if (group != Constants.RUNNABLE_MICROBE_GROUP)
                target.EntityNode.RemoveFromGroup(group);
        }

        StartBulkTransport(engulfedObject, animationSpeed);

        target.OnAttemptedToBeEngulfed();
    }

    /// <summary>
    ///   Expels an ingested object from this microbe out into the environment.
    /// </summary>
    private void EjectEngulfable(IEngulfable target, float animationSpeed = 2.0f)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None || target.PhagocytosisStep is PhagocytosisPhase.Exocytosis or
                PhagocytosisPhase.None)
        {
            return;
        }

        attemptingToEngulf.Remove(target);

        var body = target as RigidBody;
        if (body == null)
        {
            // Engulfable must be of rigidbody type to be ejected
            return;
        }

        var engulfedObject = engulfedObjects.Find(e => e.Object == target);
        if (engulfedObject == null)
            return;

        target.PhagocytosisStep = PhagocytosisPhase.Exocytosis;

        // The back of the microbe
        var exit = Hex.AxialToCartesian(new Hex(0, 1));
        var nearestPointOfMembraneToTarget = Membrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

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
            engulfedObjects.Remove(engulfedObject);
            return;
        }

        // Animate object move to the nearest point of the membrane
        engulfedObject.TargetValuesToLerp = (nearestPointOfMembraneToTarget, null, Vector3.One * Mathf.Epsilon);
        StartBulkTransport(engulfedObject, animationSpeed);

        // The rest of the operation is done in CompleteEjection
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

        foreach (var entity in touchedEntities)
        {
            if (entity is Microbe microbe && microbe.destroyed)
            {
                GD.Print($"Removed destroyed microbe from {nameof(touchedEntities)}");
                touchedEntities.Remove(microbe);
                break;
            }
        }

        var engulfCheckResult = CanEngulfObject(engulfable);

        if (engulfCheckResult == EngulfCheckResult.Ok)
        {
            IngestEngulfable(engulfable);
        }
        else if (engulfCheckResult == EngulfCheckResult.IngestedMatterFull)
        {
            OnEngulfmentStorageFull?.Invoke(this);

            OnNoticeMessage?.Invoke(this,
                new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_STORAGE_FULL")));
        }
        else if (engulfCheckResult == EngulfCheckResult.TargetTooBig)
        {
            OnNoticeMessage?.Invoke(this,
                new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_SIZE_TOO_SMALL")));
        }
    }

    /// <summary>
    ///   Animates transporting objects from phagocytosis process with linear interpolation.
    /// </summary>
    /// <returns>True when Lerp finishes.</returns>
    private bool AnimateBulkTransport(float delta, EngulfedObject engulfed)
    {
        if (engulfed.Object.Value == null || engulfed.Phagosome.Value == null)
            return false;

        var body = (RigidBody)engulfed.Object.Value;

        if (engulfed.AnimationTimeElapsed < engulfed.LerpDuration)
        {
            engulfed.AnimationTimeElapsed += delta;

            var fraction = engulfed.AnimationTimeElapsed / engulfed.LerpDuration;

            // Ease out
            fraction = Mathf.Sin(fraction * Mathf.Pi * 0.5f);

            if (engulfed.TargetValuesToLerp.Translation.HasValue)
            {
                body.Translation = engulfed.InitialValuesToLerp.Translation.LinearInterpolate(
                    engulfed.TargetValuesToLerp.Translation.Value, fraction);
            }

            if (engulfed.TargetValuesToLerp.Scale.HasValue)
            {
                body.Scale = engulfed.InitialValuesToLerp.Scale.LinearInterpolate(
                    engulfed.TargetValuesToLerp.Scale.Value, fraction);
            }

            if (engulfed.TargetValuesToLerp.EndosomeScale.HasValue)
            {
                engulfed.Phagosome.Value.Scale = engulfed.InitialValuesToLerp.EndosomeScale.LinearInterpolate(
                    engulfed.TargetValuesToLerp.EndosomeScale.Value, fraction);
            }

            return false;
        }

        // Snap values
        if (engulfed.TargetValuesToLerp.Translation.HasValue)
            body.Translation = engulfed.TargetValuesToLerp.Translation.Value;

        if (engulfed.TargetValuesToLerp.Scale.HasValue)
            body.Scale = engulfed.TargetValuesToLerp.Scale.Value;

        if (engulfed.TargetValuesToLerp.EndosomeScale.HasValue)
            engulfed.Phagosome.Value.Scale = engulfed.TargetValuesToLerp.EndosomeScale.Value;

        StopBulkTransport(engulfed);

        return true;
    }

    /// <summary>
    ///   Begins phagocytosis related lerp animation
    /// </summary>
    private void StartBulkTransport(EngulfedObject engulfedObject, float duration, bool resetElapsedTime = true)
    {
        if (engulfedObject.Object.Value == null || engulfedObject.Phagosome.Value == null)
            return;

        if (resetElapsedTime)
            engulfedObject.AnimationTimeElapsed = 0;

        var body = (RigidBody)engulfedObject.Object.Value;
        engulfedObject.InitialValuesToLerp = (body.Translation, body.Scale, engulfedObject.Phagosome.Value.Scale);
        engulfedObject.LerpDuration = duration;
        engulfedObject.Interpolate = true;
    }

    /// <summary>
    ///   Stops phagocytosis related lerp animation
    /// </summary>
    private void StopBulkTransport(EngulfedObject engulfedObject)
    {
        engulfedObject.AnimationTimeElapsed = 0;
        engulfedObject.Interpolate = false;
    }

    private void CompleteIngestion(EngulfedObject engulfed)
    {
        var engulfable = engulfed.Object.Value;
        if (engulfable == null)
            return;

        engulfable.PhagocytosisStep = PhagocytosisPhase.Ingested;

        attemptingToEngulf.Remove(engulfable);
        touchedEntities.Remove(engulfable);

        OnSuccessfulEngulfment?.Invoke(this, engulfable);
        engulfable.OnIngestedFromEngulfment();
    }

    private void CompleteEjection(EngulfedObject engulfed)
    {
        var engulfable = engulfed.Object.Value;
        if (engulfable == null)
            return;

        attemptingToEngulf.Remove(engulfable);
        engulfedObjects.Remove(engulfed);
        expelledObjects.Add(engulfed);

        engulfable.PhagocytosisStep = PhagocytosisPhase.None;

        foreach (string group in engulfed.OriginalGroups)
        {
            if (group != Constants.RUNNABLE_MICROBE_GROUP)
                engulfable.EntityNode.AddToGroup(group);
        }

        // Reset render priority
        engulfable.RenderPriority = engulfed.OriginalRenderPriority;

        engulfed.Phagosome.Value?.DestroyDetachAndQueueFree();

        // Ignore possible invalid cast as the engulfed node should be a rigidbody either way
        var body = (RigidBody)engulfable;

        body.Mode = ModeEnum.Rigid;

        // Re-parent to world node
        body.ReParentWithTransform(GetStageAsParent());

        // Reset collision layer and mask
        body.CollisionLayer = engulfed.OriginalCollisionLayer;
        body.CollisionMask = engulfed.OriginalCollisionMask;

        var impulse = Transform.origin.DirectionTo(body.Transform.origin) * body.Mass *
            Constants.ENGULF_EJECTION_FORCE;

        // Apply outwards ejection force
        body.ApplyCentralImpulse(impulse + LinearVelocity);

        // We have our own engulfer and it wants to claim this object we've just expelled
        HostileEngulfer.Value?.IngestEngulfable(engulfable);

        engulfable.OnExpelledFromEngulfment();
        engulfable.HostileEngulfer.Value = null;
    }

    /// <summary>
    ///   Stores extra information to the objects that have been engulfed.
    /// </summary>
    private class EngulfedObject
    {
        public EngulfedObject(IEngulfable @object, Endosome phagosome)
        {
            Object = new EntityReference<IEngulfable>(@object);
            Phagosome = new EntityReference<Endosome>(phagosome);

            AdditionalEngulfableCompounds = @object.CalculateAdditionalDigestibleCompounds()?
                .Where(c => c.Key.Digestible)
                .ToDictionary(c => c.Key, c => c.Value);

            InitialTotalEngulfableCompounds = @object.Compounds.Compounds
                .Where(c => c.Key.Digestible)
                .Sum(c => c.Value);

            if (AdditionalEngulfableCompounds != null)
                InitialTotalEngulfableCompounds += AdditionalEngulfableCompounds.Sum(c => c.Value);

            OriginalGroups = @object.EntityNode.GetGroups();
        }

        [JsonConstructor]
        public EngulfedObject(IEngulfable @object, Endosome phagosome,
            Dictionary<Compound, float> additionalEngulfableCompounds, float initialTotalEngulfableCompounds)
        {
            Object = new EntityReference<IEngulfable>(@object);
            Phagosome = new EntityReference<Endosome>(phagosome);
            AdditionalEngulfableCompounds = additionalEngulfableCompounds;
            InitialTotalEngulfableCompounds = initialTotalEngulfableCompounds;
        }

        /// <summary>
        ///   The solid matter that has been engulfed.
        /// </summary>
        public EntityReference<IEngulfable> Object { get; private set; }

        /// <summary>
        ///   A food vacuole containing the engulfed object. Only decorative.
        /// </summary>
        public EntityReference<Endosome> Phagosome { get; private set; }

        [JsonProperty]
        public Dictionary<Compound, float>? AdditionalEngulfableCompounds { get; private set; }

        [JsonProperty]
        public float? InitialTotalEngulfableCompounds { get; private set; }

        [JsonProperty]
        public Array OriginalGroups { get; private set; } = new();

        public bool Interpolate { get; set; }
        public float LerpDuration { get; set; }
        public float AnimationTimeElapsed { get; set; }
        public float TimeElapsedSinceEjection { get; set; }
        public (Vector3? Translation, Vector3? Scale, Vector3? EndosomeScale) TargetValuesToLerp { get; set; }
        public (Vector3 Translation, Vector3 Scale, Vector3 EndosomeScale) InitialValuesToLerp { get; set; }
        public Vector3 OriginalScale { get; set; }
        public int OriginalRenderPriority { get; set; }

        // These values (default microbe collision layer & mask) are here for save compatibility
        public uint OriginalCollisionLayer { get; set; } = 3;
        public uint OriginalCollisionMask { get; set; } = 3;
    }
}
