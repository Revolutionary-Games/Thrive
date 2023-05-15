using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Compounds, ATP, Reproduction
///   Divide, Organelles, Toxin, Digestion
/// </summary>
public partial class Microbe
{
    [JsonProperty]
    private readonly CompoundBag compounds = new(0.0f);

    [JsonProperty]
    private readonly Dictionary<Compound, float> requiredCompoundsForBaseReproduction = new();

    private Compound atp = null!;
    private Compound glucose = null!;
    private Compound mucilage = null!;

    private Enzyme lipase = null!;

    [JsonProperty]
    private CompoundCloudSystem? cloudSystem;

    [JsonProperty]
    private ISpawnSystem? spawnSystem;

    [JsonProperty]
    private Compound? queuedToxinToEmit;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<PlacedOrganelle>? organelles;

    private bool enzymesDirty = true;
    private Dictionary<Enzyme, int> enzymes = new();

    [JsonProperty]
    private float lastCheckedATPDamage;

    [JsonProperty]
    private float lastCheckedOxytoxyDigestionDamage;

    [JsonProperty]
    private float dissolveEffectValue;

    [JsonProperty]
    private float playerEngulfedDeathTimer;

    [JsonProperty]
    private float slimeSecretionCooldown;

    [JsonProperty]
    private float queuedSlimeSecretionTime;

    private float lastCheckedReproduction;

    /// <summary>
    ///   Flips every reproduction update. Used to make compound use for reproduction distribute more evenly between
    ///   the compound types.
    /// </summary>
    private bool consumeReproductionCompoundsReverse;

    /// <summary>
    ///   The microbe stores here the sum of capacity of all the
    ///   current organelles. This is here to prevent anyone from
    ///   messing with this value if we used the Capacity from the
    ///   CompoundBag for the calculations that use this.
    /// </summary>
    private float organellesCapacity;

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

    private float timeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_COMPOUND_UPDATE_INTERVAL;
    private float timeUntilDigestionUpdate = Constants.MICROBE_DIGESTION_UPDATE_INTERVAL;

    private bool organelleMaxRenderPriorityDirty = true;
    private int cachedOrganelleMaxRenderPriority;

    public enum DigestCheckResult
    {
        Ok,
        MissingEnzyme,
    }

    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    [JsonIgnore]
    public CompoundBag Compounds => compounds;

    /// <summary>
    ///   True only when this cell has been killed to let know things
    ///   being engulfed by us that we are dead.
    /// </summary>
    [JsonProperty]
    public bool Dead { get; private set; }

    /// <summary>
    ///   The number of agent vacuoles. Determines the time between
    ///   toxin shots.
    /// </summary>
    [JsonProperty]
    public int AgentVacuoleCount { get; private set; }

    /// <summary>
    ///   The slime jets attached to this microbe. JsonIgnore as the components add themselves to this list each load.
    /// </summary>
    [JsonIgnore]
    public List<SlimeJetComponent> SlimeJets { get; private set; } = new();

    /// <summary>
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleParent { get; private set; } = null!;

    /// <summary>
    ///   The cached highest assigned render priority from all of the organelles.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     A possibly cheaper version of <see cref="OrganelleLayout{T}.MaxRenderPriority"/>.
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public int OrganelleMaxRenderPriority
    {
        get
        {
            if (organelleMaxRenderPriorityDirty)
                CountOrganelleMaxRenderPriority();

            return cachedOrganelleMaxRenderPriority;
        }
    }

    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => Compounds;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds. Also used by player cell for tutorials
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> TotalAbsorbedCompounds { get; set; } = new();

    [JsonProperty]
    public float AgentEmissionCooldown { get; private set; }

    [JsonIgnore]
    public Enzyme RequisiteEnzymeToDigest => SimulationParameters.Instance.GetEnzyme(Membrane.Type.DissolverEnzyme);

    [JsonIgnore]
    public float DigestedAmount
    {
        get => dissolveEffectValue;
        set
        {
            dissolveEffectValue = Mathf.Clamp(value, 0.0f, 1.0f);
            UpdateDissolveEffect();
        }
    }

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    [JsonProperty]
    public Action<Microbe, bool>? OnReproductionStatus { get; set; }

    /// <summary>
    ///   Called periodically to report the chemoreception settings of the microbe
    /// </summary>
    [JsonProperty]
    public Action<Microbe, IEnumerable<(Compound Compound, float Range, float MinAmount, Color Colour)>>?
        OnCompoundChemoreceptionInfo { get; set; }

    /// <summary>
    ///   Resets the organelles in this microbe to match the species definition
    /// </summary>
    public void ResetOrganelleLayout()
    {
        // TODO: It would be much better if only organelles that need to be removed where removed,
        // instead of everything.
        // When doing that all organelles will need to be re-added anyway if this turned from a prokaryote to eukaryote

        if (organelles == null)
        {
            organelles = new OrganelleLayout<PlacedOrganelle>(OnOrganelleAdded, OnOrganelleRemoved);
        }
        else
        {
            // Just clear the existing ones
            organelles.Clear();
        }

        foreach (var entry in CellTypeProperties.Organelles.Organelles)
        {
            var placed = new PlacedOrganelle(entry.Definition, entry.Position, entry.Orientation)
            {
                Upgrades = entry.Upgrades,
            };

            organelles.Add(placed);
        }

        // Reproduction progress is lost
        allOrganellesDivided = false;
        SetupRequiredBaseReproductionCompounds();

        // Unbind if a colony's master cell removed its binding agent.
        if (Colony != null && Colony.Master == this && !organelles.Any(p => p.IsBindingAgent))
            Colony.RemoveFromColony(this);

        // Make chemoreception update happen immediately in case the settings changed so that new information is
        // used earlier
        timeUntilChemoreceptionUpdate = 0;

        if (IsMulticellular)
            ResetMulticellularProgress();
    }

    /// <summary>
    ///   Applies the set species' color to all of this microbe's organelles
    /// </summary>
    public void ApplyPreviewOrganelleColours()
    {
        if (!IsForPreviewOnly)
            throw new InvalidOperationException("Microbe must be a preview-only type");

        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized");

        foreach (var entry in organelles.Organelles)
        {
            entry.Colour = CellTypeProperties.Colour;
            entry.UpdateAsync(0);

            // This applies the colour so UpdateAsync is not technically needed but to avoid weird bugs we just do it
            // as well
            entry.UpdateSync();
        }
    }

    /// <summary>
    ///   Tries to fire a toxin if possible
    /// </summary>
    public void EmitToxin(Compound? agentType = null)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        agentType ??= SimulationParameters.Instance.GetCompound("oxytoxy");

        PerformForOtherColonyMembersIfWeAreLeader(m => m.EmitToxin(agentType));

        if (AgentEmissionCooldown > 0)
            return;

        // Only shoot if you have an agent vacuole.
        if (AgentVacuoleCount < 1)
            return;

        float amountAvailable = Compounds.GetCompoundAmount(agentType);

        // Emit as much as you have, but don't start the cooldown if that's zero
        float amountEmitted = Math.Min(amountAvailable, Constants.MAXIMUM_AGENT_EMISSION_AMOUNT);
        if (amountEmitted < Constants.MINIMUM_AGENT_EMISSION_AMOUNT)
            return;

        Compounds.TakeCompound(agentType, amountEmitted);

        // The cooldown time is inversely proportional to the amount of agent vacuoles.
        AgentEmissionCooldown = Constants.AGENT_EMISSION_COOLDOWN / AgentVacuoleCount;

        float ejectionDistance = Membrane.EncompassingCircleRadius +
            Constants.AGENT_EMISSION_DISTANCE_OFFSET;

        if (CellTypeProperties.IsBacteria)
            ejectionDistance *= 0.5f;

        var props = new AgentProperties(Species, agentType);

        // Find the direction the microbe is facing
        // (actual rotation, not LookAtPoint, also takes colony membership into account)
        Vector3 direction;
        if (Colony != null)
        {
            direction = Colony.Master.GlobalTransform
                .basis.Quat().Normalized().Xform(Vector3.Forward);
        }
        else
        {
            direction = GlobalTransform.basis.Quat().Normalized().Xform(Vector3.Forward);
        }

        var position = GlobalTransform.origin + (direction * ejectionDistance);

        var agent = SpawnHelpers.SpawnAgent(props, amountEmitted, Constants.EMITTED_AGENT_LIFETIME,
            position, direction, GetStageAsParent(),
            SpawnHelpers.LoadAgentScene(), this);

        ModLoader.ModInterface.TriggerOnToxinEmitted(agent);

        if (amountEmitted < Constants.MAXIMUM_AGENT_EMISSION_AMOUNT / 2)
        {
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin-low.ogg");
        }
        else
        {
            PlaySoundEffect("res://assets/sounds/soundeffects/microbe-release-toxin.ogg");
        }
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

    public void QueueSecreteSlime(float duration)
    {
        PerformForOtherColonyMembersIfWeAreLeader(m => m.QueueSecreteSlime(duration));

        if (SlimeJets.Count < 1)
            return;

        queuedSlimeSecretionTime += duration;
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
    /// <remarks>
    ///   <para>
    ///     Now with multicellular colonies are also allowed to divide so there's no longer a check against that
    ///   </para>
    /// </remarks>
    public Microbe Divide()
    {
        if (ColonyParent != null)
            throw new ArgumentException("Cell that is a colony member (non-leader) can't divide");

        var currentPosition = GlobalTransform.origin;

        // Find the direction to the right from where the cell is facing
        var direction = GlobalTransform.basis.Quat().Normalized().Xform(Vector3.Right);

        // Start calculating separation distance
        var organellePositions = organelles!.Organelles.Select(o => Hex.AxialToCartesian(o.Position)).ToList();

        float distanceRight = MathUtils.GetMaximumDistanceInDirection(Vector3.Right, Vector3.Zero, organellePositions);
        float distanceLeft = MathUtils.GetMaximumDistanceInDirection(Vector3.Left, Vector3.Zero, organellePositions);

        if (Colony != null)
        {
            var colonyMembers = Colony.ColonyMembers.Select(c => c.GlobalTransform.origin);

            distanceRight += MathUtils.GetMaximumDistanceInDirection(direction, currentPosition, colonyMembers);
        }

        float width = distanceLeft + distanceRight + Constants.DIVIDE_EXTRA_DAUGHTER_OFFSET;

        if (CellTypeProperties.IsBacteria)
            width *= 0.5f;

        // Create the one daughter cell.
        var copyEntity = SpawnHelpers.SpawnMicrobe(Species, currentPosition + direction * width,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem!, spawnSystem!, CurrentGame);

        // Since the daughter spawns right next to the cell, it should face the same way to avoid colliding
        var daughterBasis = new Basis(Transform.basis.Quat())
        {
            Scale = copyEntity.Transform.basis.Scale,
        };

        copyEntity.Transform = new Transform(daughterBasis, copyEntity.Translation);

        // Make it despawn like normal
        spawnSystem!.AddEntityToTrack(copyEntity);

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

        return copyEntity;
    }

    /// <summary>
    ///   Throws some compound out of this Microbe, up to maxAmount
    /// </summary>
    /// <param name="compound">The compound type to eject</param>
    /// <param name="maxAmount">The maximum amount to eject</param>
    /// <param name="direction">The direction in which to eject relative to the microbe</param>
    /// <param name="displacement">How far away from the microbe to eject</param>
    public float EjectCompound(Compound compound, float maxAmount, Vector3 direction, float displacement = 0)
    {
        float amount = Compounds.TakeCompound(compound, maxAmount);

        SpawnEjectedCompound(compound, amount, direction, displacement);
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

        // Add the currently held compounds, but only if configured as this can be pretty confusing for players
        // to have the bars in ready to reproduce state for a while before the time limited reproduction actually
        // catches up
        if (Constants.ALWAYS_SHOW_STORED_COMPOUNDS_IN_REPRODUCTION_PROGRESS ||
            !GameWorld.WorldSettings.LimitReproductionCompoundUseSpeed)
        {
            foreach (var key in gatheredCompounds.Keys.ToList())
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
        }

        float totalFraction = 0;

        foreach (var entry in totalCompounds)
        {
            if (gatheredCompounds.TryGetValue(entry.Key, out var gathered) && entry.Value != 0)
                totalFraction += gathered / entry.Value;
        }

        return totalFraction / totalCompounds.Count;
    }

    /// <summary>
    ///   Calculates total compounds needed for a cell to reproduce, used by calculateReproductionProgress to calculate
    ///   the fraction done.
    /// </summary>
    public Dictionary<Compound, float> CalculateTotalCompounds()
    {
        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized first");

        if (IsMulticellular)
            return CalculateTotalBodyPlanCompounds();

        var result = CellTypeProperties.CalculateTotalComposition();

        result.Merge(Species.BaseReproductionCost);

        return result;
    }

    /// <summary>
    ///   Calculates how much compounds organelles have already absorbed
    /// </summary>
    public Dictionary<Compound, float> CalculateAlreadyAbsorbedCompounds()
    {
        if (organelles == null)
            throw new InvalidOperationException("Microbe must be initialized first");

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

        if (compoundsUsedForMulticellularGrowth != null)
        {
            result.Merge(compoundsUsedForMulticellularGrowth);
        }
        else
        {
            // For single microbes the base reproduction cost needs to be calculated here
            // TODO: can we make this more efficient somehow
            foreach (var entry in Species.BaseReproductionCost)
            {
                requiredCompoundsForBaseReproduction.TryGetValue(entry.Key, out var remaining);

                var used = entry.Value - remaining;

                result.TryGetValue(entry.Key, out var alreadyUsed);

                result[entry.Key] = alreadyUsed + used;
            }
        }

        return result;
    }

    public Dictionary<Compound, float> CalculateAdditionalDigestibleCompounds()
    {
        var result = new Dictionary<Compound, float>();

        // Add some part of the build cost of all the organelles
        foreach (var organelle in organelles!)
        {
            foreach (var entry in organelle.Definition.InitialComposition)
            {
                result.TryGetValue(entry.Key, out float existing);
                result[entry.Key] = existing + entry.Value;
            }
        }

        CalculateBonusDigestibleGlucose(result);
        return result;
    }

    /// <summary>
    ///   Returns the check result whether this microbe can digest the target (has the enzyme necessary).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This is different from <see cref="CanEngulfObject(IEngulfable)"/> because ingestibility and digestibility
    ///     are separate, you can engulf a walled cell but not digest it if you're missing the enzyme required to do
    ///     so.
    ///   </para>
    /// </remarks>
    public DigestCheckResult CanDigestObject(IEngulfable engulfable)
    {
        var enzyme = engulfable.RequisiteEnzymeToDigest;

        if (enzyme != null && !Enzymes.ContainsKey(enzyme))
            return DigestCheckResult.MissingEnzyme;

        return DigestCheckResult.Ok;
    }

    /// <summary>
    ///   Perform an action for all members of this cell's colony other than this cell if this is the colony leader.
    /// </summary>
    private void PerformForOtherColonyMembersIfWeAreLeader(Action<Microbe> action)
    {
        if (Colony?.Master == this)
        {
            foreach (var cell in Colony.ColonyMembers)
            {
                if (cell == this)
                    continue;

                action(cell);
            }
        }
    }

    private void HandleCompoundAbsorbing(float delta)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem!.AbsorbCompounds(GlobalTransform.origin, grabRadius, Compounds,
            TotalAbsorbedCompounds, delta, Membrane.Type.ResourceAbsorptionFactor);

        // Cells with jets aren't affected by mucilage
        slowedBySlime = SlimeJets.Count < 1 && cloudSystem.AmountAvailable(mucilage, GlobalTransform.origin, 1.0f) >
            Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT;

        if (IsPlayerMicrobe && CheatManager.InfiniteCompounds)
        {
            var usefulCompounds = SimulationParameters.Instance.GetCloudCompounds().Where(Compounds.IsUseful);
            foreach (var usefulCompound in usefulCompounds)
                Compounds.AddCompound(usefulCompound, Compounds.GetFreeSpaceForCompound(usefulCompound));
        }
    }

    /// <summary>
    ///   Vents (throws out) non-useful compounds from this cell
    /// </summary>
    private void HandleCompoundVenting(float delta)
    {
        // Skip if process system has not run yet
        if (!Compounds.HasAnyBeenSetUseful())
            return;

        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta;

        // Cloud types are ones that can be vented
        foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
        {
            // Vent if not useful, or if overflowed the capacity
            // The multiply by 2 is here to be more kind to cells that have just divided and make it much less likely
            // the player often sees their cell venting away their precious compounds
            if (!Compounds.IsUseful(type))
            {
                amountToVent -= EjectCompound(type, amountToVent, Vector3.Back);
            }
            else if (Compounds.GetCompoundAmount(type) > 2 * Compounds.Capacity)
            {
                // Vent the part that went over
                float toVent = Compounds.GetCompoundAmount(type) - (2 * Compounds.Capacity);

                amountToVent -= EjectCompound(type, Math.Min(toVent, amountToVent), Vector3.Back);
            }

            if (amountToVent <= 0)
                break;
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

        MaxHitpoints = CellTypeProperties.MembraneType.Hitpoints +
            (CellTypeProperties.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER);

        Hitpoints = MaxHitpoints * currentHealth;
    }

    /// <summary>
    ///   Handles feeding the organelles in this microbe in order for them to split. After all are split this is
    ///   ready to reproduce.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     AI cells will immediately reproduce when they can. On the player cell the editor is unlocked when
    ///     reproducing is possible.
    ///   </para>
    ///   <para>
    ///     TODO: split this into two parts: giving compounds to grow, and actually spawning things to be able to
    ///     do multithreading here
    ///   </para>
    /// </remarks>
    private void HandleReproduction(float delta)
    {
        // Dead or engulfed cells can't reproduce
        if (Dead || PhagocytosisStep != PhagocytosisPhase.None)
            return;

        if (allOrganellesDivided)
        {
            // Ready to reproduce already. Only the player gets here as other cells split and reset automatically
            return;
        }

        lastCheckedReproduction += delta;

        // Limit how often the reproduction logic is ran
        if (lastCheckedReproduction < Constants.MICROBE_REPRODUCTION_PROGRESS_INTERVAL)
            return;

        // Limit how big progress spikes lag can cause
        if (lastCheckedReproduction > Constants.MICROBE_REPRODUCTION_MAX_DELTA_FRAME)
            lastCheckedReproduction = Constants.MICROBE_REPRODUCTION_MAX_DELTA_FRAME;

        var elapsedSinceLastUpdate = lastCheckedReproduction;
        consumeReproductionCompoundsReverse = !consumeReproductionCompoundsReverse;

        lastCheckedReproduction = 0;

        // Multicellular microbes in a colony still run reproduction logic as long as they are the colony leader
        if (IsMulticellular && ColonyParent == null)
        {
            HandleMulticellularReproduction(elapsedSinceLastUpdate);
            return;
        }

        if (Colony != null)
        {
            // TODO: should the colony just passively get the reproduction compounds in its storage?
            // Otherwise early multicellular colonies lose the passive reproduction feature
            return;
        }

        var (remainingAllowedCompoundUse, remainingFreeCompounds) =
            CalculateFreeCompoundsAndLimits(elapsedSinceLastUpdate);

        // Process base cost first so the player can be their designed cell (without extra organelles) for a while
        bool reproductionStageComplete =
            ProcessBaseReproductionCost(ref remainingAllowedCompoundUse, ref remainingFreeCompounds);

        // For this stage and all others below, reproductionStageComplete tracks whether the previous reproduction
        // stage completed, i.e. whether we should proceed with the next stage
        if (reproductionStageComplete)
        {
            // Organelles that are ready to split
            var organellesToAdd = new List<PlacedOrganelle>();

            // Grow all the organelles, except the unique organelles which are given compounds last
            foreach (var organelle in organelles!.Organelles)
            {
                // Check if already done
                if (organelle.WasSplit)
                    continue;

                // If we ran out of allowed compound use, stop early
                if (remainingAllowedCompoundUse <= 0)
                {
                    reproductionStageComplete = false;
                    break;
                }

                // We are in G1 phase of the cell cycle, duplicate all organelles.

                // Except the unique organelles
                if (organelle.Definition.Unique)
                    continue;

                // Give it some compounds to make it larger.
                organelle.GrowOrganelle(Compounds, ref remainingAllowedCompoundUse, ref remainingFreeCompounds,
                    consumeReproductionCompoundsReverse);

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

                // TODO: can we quit this loop early if we still would have dozens of organelles to check but don't have
                // any compounds left to give them (that are probably useful)?
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
        }

        if (reproductionStageComplete)
        {
            foreach (var organelle in organelles!.Organelles)
            {
                // In the second phase all unique organelles are given compounds
                // It used to be that only the nucleus was given compounds here
                if (!organelle.Definition.Unique)
                    continue;

                // If we ran out of allowed compound use, stop early
                if (remainingAllowedCompoundUse <= 0)
                {
                    reproductionStageComplete = false;
                    break;
                }

                // Unique organelles don't split so we use the growth value to know when something is fully grown
                if (organelle.GrowthValue < 1.0f)
                {
                    organelle.GrowOrganelle(Compounds, ref remainingAllowedCompoundUse, ref remainingFreeCompounds,
                        consumeReproductionCompoundsReverse);

                    // Nucleus (or another unique organelle) needs more compounds
                    reproductionStageComplete = false;
                }
            }
        }

        if (reproductionStageComplete)
        {
            // All organelles and base reproduction cost is now fulfilled, we are fully ready to reproduce
            allOrganellesDivided = true;

            // For NPC cells this immediately splits them and the allOrganellesDivided flag is reset
            ReadyToReproduce();
        }
    }

    private (float RemainingAllowedCompoundUse, float RemainingFreeCompounds)
        CalculateFreeCompoundsAndLimits(float delta)
    {
        var gameWorldWorldSettings = GameWorld.WorldSettings;

        // Skip some computations when they are not needed
        if (!gameWorldWorldSettings.PassiveGainOfReproductionCompounds &&
            !gameWorldWorldSettings.LimitReproductionCompoundUseSpeed)
        {
            return (float.MaxValue, 0);
        }

        // TODO: make the current patch affect this?
        // TODO: make being in a colony affect this
        float remainingFreeCompounds = Constants.MICROBE_REPRODUCTION_FREE_COMPOUNDS *
            (HexCount * Constants.MICROBE_REPRODUCTION_FREE_RATE_FROM_HEX + 1.0f) * delta;

        if (IsMulticellular)
            remainingFreeCompounds *= Constants.EARLY_MULTICELLULAR_REPRODUCTION_COMPOUND_MULTIPLIER;

        float remainingAllowedCompoundUse = float.MaxValue;

        if (gameWorldWorldSettings.LimitReproductionCompoundUseSpeed)
        {
            remainingAllowedCompoundUse = remainingFreeCompounds * Constants.MICROBE_REPRODUCTION_MAX_COMPOUND_USE;
        }

        // Reset the free compounds if we don't want to give free compounds.
        // It was necessary to calculate for the above math to be able to use it, but we don't want it to apply when
        // not enabled.
        if (!gameWorldWorldSettings.PassiveGainOfReproductionCompounds)
        {
            remainingFreeCompounds = 0;
        }

        return (remainingAllowedCompoundUse, remainingFreeCompounds);
    }

    private bool ProcessBaseReproductionCost(ref float remainingAllowedCompoundUse, ref float remainingFreeCompounds,
        Dictionary<Compound, float>? trackCompoundUse = null)
    {
        if (remainingAllowedCompoundUse <= 0)
        {
            return false;
        }

        bool reproductionStageComplete = true;

        foreach (var key in consumeReproductionCompoundsReverse ?
                     requiredCompoundsForBaseReproduction.Keys.Reverse() :
                     requiredCompoundsForBaseReproduction.Keys)
        {
            var amountNeeded = requiredCompoundsForBaseReproduction[key];

            if (amountNeeded <= 0.0f)
                continue;

            // TODO: the following is very similar code to PlacedOrganelle.GrowOrganelle
            float usedAmount = 0;

            float allowedUseAmount = Math.Min(amountNeeded, remainingAllowedCompoundUse);

            if (remainingFreeCompounds > 0)
            {
                var usedFreeCompounds = Math.Min(allowedUseAmount, remainingFreeCompounds);
                usedAmount += usedFreeCompounds;
                allowedUseAmount -= usedFreeCompounds;
                remainingFreeCompounds -= usedFreeCompounds;
            }

            // For consistency we apply the ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST constant here like for
            // organelle growth
            var amountAvailable =
                compounds.GetCompoundAmount(key) - Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

            if (amountAvailable > MathUtils.EPSILON)
            {
                // We can take some
                var amountToTake = Mathf.Min(allowedUseAmount, amountAvailable);

                usedAmount += compounds.TakeCompound(key, amountToTake);
            }

            if (usedAmount < MathUtils.EPSILON)
                continue;

            remainingAllowedCompoundUse -= usedAmount;

            if (trackCompoundUse != null)
            {
                trackCompoundUse.TryGetValue(key, out var trackedAlreadyUsed);
                trackCompoundUse[key] = trackedAlreadyUsed + usedAmount;
            }

            var left = amountNeeded - usedAmount;

            if (left < 0.0001f)
            {
                // We don't remove these values even when empty as we rely on detecting this being empty for earlier
                // save compatibility, so we just leave 0 values in requiredCompoundsForBaseReproduction
                left = 0;
            }

            requiredCompoundsForBaseReproduction[key] = left;

            // As we don't make duplicate lists, we can only process a single type per call
            // So we can't know here if we are fully ready
            reproductionStageComplete = false;
            break;
        }

        return reproductionStageComplete;
    }

    /// <summary>
    ///   Sets up the base reproduction cost that is on top of the normal costs
    /// </summary>
    private void SetupRequiredBaseReproductionCompounds()
    {
        requiredCompoundsForBaseReproduction.Clear();
        requiredCompoundsForBaseReproduction.Merge(Species.BaseReproductionCost);
        totalNeededForMulticellularGrowth = null;
    }

    private void OnPlayerDuplicationCheat(object sender, EventArgs e)
    {
        allOrganellesDivided = true;

        Divide();
    }

    private PlacedOrganelle SplitOrganelle(PlacedOrganelle organelle)
    {
        var q = organelle.Position.Q;
        var r = organelle.Position.R;

        // The position used here will be overridden with the right value when we manage to find a place
        // for this organelle
        var newOrganelle = new PlacedOrganelle(organelle.Definition, new Hex(q, r), 0)
        {
            Upgrades = organelle.Upgrades,
        };

        // Spiral search for space for the organelle
        int radius = 1;
        while (true)
        {
            // Moves into the ring of radius "radius" and center the old organelle
            var radiusOffset = Hex.HexNeighbourOffset[Hex.HexSide.BottomLeft];
            q += radiusOffset.Q;
            r += radiusOffset.R;

            // Iterates in the ring
            for (int side = 1; side <= 6; ++side)
            {
                var offset = Hex.HexNeighbourOffset[(Hex.HexSide)side];

                // Moves "radius" times into each direction
                for (int i = 1; i <= radius; ++i)
                {
                    q += offset.Q;
                    r += offset.R;

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
                        if (organelles!.CanPlace(newOrganelle))
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

            OnReproductionStatus?.Invoke(this, Colony == null || IsMulticellular);
        }
        else
        {
            // Skip reproducing if we would go too much over the entity limit
            if (!spawnSystem!.IsUnderEntityLimitForReproducing())
            {
                // Set this to false so that we re-check in a few frames if we can reproduce then
                allOrganellesDivided = false;
                return;
            }

            if (!Species.PlayerSpecies)
            {
                GameWorld.AlterSpeciesPopulationInCurrentPatch(Species,
                    Constants.CREATURE_REPRODUCE_POPULATION_GAIN, TranslationServer.Translate("REPRODUCED"));
            }

            if (!IsMulticellular)
            {
                // Return the first cell to its normal, non duplicated cell arrangement and spawn a daughter cell
                ResetOrganelleLayout();

                Divide();
            }
            else
            {
                Divide();

                enoughResourcesForBudding = false;

                // Let's require the base reproduction cost to be fulfilled again as well, to keep down the colony
                // spam, and for consistency with non-multicellular microbes
                SetupRequiredBaseReproductionCompounds();
            }
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

    private void HandleOsmoregulation(float delta)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
            return;

        var osmoregulationCost = (HexCount * CellTypeProperties.MembraneType.OsmoregulationFactor *
            Constants.ATP_COST_FOR_OSMOREGULATION) * delta;

        // 5% osmoregulation bonus per colony member
        if (Colony != null)
        {
            osmoregulationCost *= 20.0f / (20.0f + Colony.ColonyMembers.Count);
        }

        if (CellTypeProperties.IsBacteria != true)
            osmoregulationCost *= Constants.NUCLEUS_OSMOREGULATION_REDUCTION;

        if (Species.PlayerSpecies)
            osmoregulationCost *= CurrentGame.GameWorld.WorldSettings.OsmoregulationMultiplier;

        Compounds.TakeCompound(atp, osmoregulationCost);
    }

    private void HandleMovement(float delta)
    {
        if (PhagocytosisStep != PhagocytosisPhase.None)
        {
            // Reset movement
            MovementDirection = Vector3.Zero;
            queuedMovementForce = Vector3.Zero;

            return;
        }

        if (MovementDirection != Vector3.Zero || queuedMovementForce != Vector3.Zero)
        {
            // Movement direction should not be normalized to allow different speeds
            Vector3 totalMovement = Vector3.Zero;

            if (MovementDirection != Vector3.Zero)
            {
                totalMovement += DoBaseMovementForce(delta);
            }

            totalMovement += queuedMovementForce;

            ApplyMovementImpulse(totalMovement, delta);

            var deltaAcceleration = (linearAcceleration - lastLinearAcceleration).LengthSquared();

            if (movementSoundCooldownTimer > 0)
                movementSoundCooldownTimer -= delta;

            // The cell starts moving from a relatively idle velocity, so play the begin movement sound
            // TODO: Account for cell turning, I can't figure out a reliable way to do that using the current
            // calculation - Kasterisk
            if (movementSoundCooldownTimer <= 0 && deltaAcceleration > lastLinearAcceleration.LengthSquared() &&
                lastLinearVelocity.LengthSquared() <= 1)
            {
                movementSoundCooldownTimer = Constants.MICROBE_MOVEMENT_SOUND_EMIT_COOLDOWN;
                PlaySoundEffect("res://assets/sounds/soundeffects/microbe-movement-1.ogg");
            }

            if (!movementAudio.Playing)
                movementAudio.Play();

            // Max volume is 0.4
            if (movementAudio.Volume < 0.4f)
                movementAudio.Volume += delta;
        }
        else
        {
            if (movementAudio.Playing)
            {
                movementAudio.Volume -= delta;

                if (movementAudio.Volume <= 0)
                    movementAudio.Stop();
            }
        }
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

    [DeserializedCallbackAllowed]
    private void OnOrganelleAdded(PlacedOrganelle organelle)
    {
        organelle.OnAddedToMicrobe(this);
        processesDirty = true;
        enzymesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;
        cachedRotationSpeed = null;
        organelleMaxRenderPriorityDirty = true;

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

        if (organelle.IsSlimeJet)
            SlimeJets.Remove((SlimeJetComponent)organelle.Components.First(c => c is SlimeJetComponent));

        organelle.OnRemovedFromMicrobe();

        // The organelle only detaches but doesn't delete itself, so we delete it here
        organelle.DetachAndQueueFree();

        processesDirty = true;
        enzymesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;
        cachedRotationSpeed = null;
        organelleMaxRenderPriorityDirty = true;

        Compounds.Capacity = organellesCapacity;
    }

    /// <summary>
    ///   Recomputes storage from organelles, used after loading a save
    /// </summary>
    private void RecomputeOrganelleCapacity()
    {
        organellesCapacity = organelles!.Sum(o => o.StorageCapacity);
        Compounds.Capacity = organellesCapacity;
    }

    private bool CheckHasSignalingAgent()
    {
        if (hasSignalingAgent != null)
            return hasSignalingAgent.Value;

        hasSignalingAgent = organelles!.Any(o => o.HasComponent<SignalingAgentComponent>());
        return hasSignalingAgent.Value;
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
    private void SpawnEjectedCompound(Compound compound, float amount, Vector3 direction, float displacement = 0)
    {
        var amountToEject = amount * Constants.MICROBE_VENT_COMPOUND_MULTIPLIER;

        if (amountToEject <= MathUtils.EPSILON)
            return;

        cloudSystem!.AddCloud(compound, amountToEject, CalculateNearbyWorldPosition(direction, displacement));
    }

    /// <summary>
    ///   Calculates a world pos for emitting compounds
    /// </summary>
    private Vector3 CalculateNearbyWorldPosition(Vector3 direction, float displacement = 0)
    {
        // OLD CODE kept here in case we want a more accurate membrane position, also this code
        // produces an incorrect world position which needs fixing if this were to be used
        /*
        // The back of the microbe
        var exit = Hex.AxialToCartesian(new Hex(0, 1));
        var membraneCoords = Membrane.GetVectorTowardsNearestPointOfMembrane(exit.x, exit.z);

        // Get the distance to eject the compounds
        var ejectionDistance = Membrane.EncompassingCircleRadius;

        // The membrane radius doesn't take being bacteria into account
        if (CellTypeProperties.IsBacteria)
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
        */

        // Unlike the commented block of code above, this uses cheap membrane radius to calculate
        // distance for cheaper computations
        var distance = Membrane.EncompassingCircleRadius;

        // The membrane radius doesn't take being bacteria into account
        if (CellTypeProperties.IsBacteria)
            distance *= 0.5f;

        distance += displacement;

        var ejectionDirection = GlobalTransform.basis.Quat().Normalized().Xform(direction);

        var result = GlobalTransform.origin + (ejectionDirection * distance);

        return result;
    }

    private void HandleChemoreceptorLines(float delta)
    {
        timeUntilChemoreceptionUpdate -= delta;

        if (timeUntilChemoreceptionUpdate > 0 || Dead)
            return;

        timeUntilChemoreceptionUpdate = Constants.CHEMORECEPTOR_COMPOUND_UPDATE_INTERVAL;

        OnCompoundChemoreceptionInfo?.Invoke(this, activeCompoundDetections);

        // TODO: should this be cleared each time or only when the chemoreception update interval has elapsed?
        activeCompoundDetections.Clear();
    }

    /// <summary>
    ///   Absorbs compounds/nutrients from ingested objects.
    /// </summary>
    private void HandleDigestion(float delta)
    {
        timeUntilDigestionUpdate -= delta;

        if (timeUntilDigestionUpdate > 0 || Dead)
            return;

        timeUntilDigestionUpdate = Constants.MICROBE_DIGESTION_UPDATE_INTERVAL;

        var compoundTypes = SimulationParameters.Instance.GetAllCompounds();
        var oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

        float usedCapacity = 0.0f;

        // Handle logic if the objects that are being digested are the ones we have engulfed
        for (int i = engulfedObjects.Count - 1; i >= 0; --i)
        {
            var engulfedObject = engulfedObjects[i];

            var engulfable = engulfedObject.Object.Value;
            if (engulfable == null)
                continue;

            // Expel this engulfed object if the cell loses some of its size and its ingestion capacity
            // is overloaded
            if (UsedIngestionCapacity > EngulfSize)
            {
                EjectEngulfable(engulfable);
                continue;
            }

            // Doesn't make sense to digest non ingested objects, i.e. objects that are being engulfed,
            // being ejected, etc. So skip them.
            if (engulfable.PhagocytosisStep != PhagocytosisPhase.Ingested)
                continue;

            Enzyme usedEnzyme;

            var digestibility = CanDigestObject(engulfable);

            switch (digestibility)
            {
                case DigestCheckResult.Ok:
                    usedEnzyme = engulfable.RequisiteEnzymeToDigest ?? lipase;
                    break;
                case DigestCheckResult.MissingEnzyme:
                    EjectEngulfable(engulfable);
                    OnNoticeMessage?.Invoke(this,
                        new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_MISSING_ENZYME")
                            .FormatSafe(engulfable.RequisiteEnzymeToDigest!.Name)));
                    continue;
                default:
                    throw new InvalidOperationException("Unhandled digestibility check result, won't digest");
            }

            var containedCompounds = engulfable.Compounds;
            var additionalCompounds = engulfedObject.AdditionalEngulfableCompounds;

            // Workaround to avoid NaN compounds in engulfed objects, leading to glitches like infinite compound
            // ejection and incorrect ingested matter display
            // https://github.com/Revolutionary-Games/Thrive/issues/3548
            containedCompounds.FixNaNCompounds();

            var totalAmountLeft = 0.0f;

            foreach (var compound in compoundTypes.Values)
            {
                if (!compound.Digestible)
                    continue;

                var originalAmount = containedCompounds.GetCompoundAmount(compound);

                var additionalAmount = 0.0f;
                additionalCompounds?.TryGetValue(compound, out additionalAmount);

                var totalAvailable = originalAmount + additionalAmount;
                totalAmountLeft += totalAvailable;

                if (totalAvailable <= 0)
                    continue;

                var amount = MicrobeInternalCalculations.CalculateDigestionSpeed(Enzymes[usedEnzyme]);
                amount *= delta;

                // Efficiency starts from Constants.ENGULF_BASE_COMPOUND_ABSORPTION_YIELD up to
                // Constants.ENZYME_DIGESTION_EFFICIENCY_MAXIMUM. This means at least 7 lysosomes
                // are needed to achieve "maximum" efficiency
                var efficiency = MicrobeInternalCalculations.CalculateDigestionEfficiency(Enzymes[usedEnzyme]);

                var taken = Mathf.Min(totalAvailable, amount);

                // Toxin damage
                if (compound == oxytoxy && taken > 0)
                {
                    lastCheckedOxytoxyDigestionDamage += delta;

                    if (lastCheckedOxytoxyDigestionDamage >= Constants.TOXIN_DIGESTION_DAMAGE_CHECK_INTERVAL)
                    {
                        lastCheckedOxytoxyDigestionDamage -= Constants.TOXIN_DIGESTION_DAMAGE_CHECK_INTERVAL;
                        Damage(MaxHitpoints * Constants.TOXIN_DIGESTION_DAMAGE_FRACTION, "oxytoxy");

                        OnNoticeMessage?.Invoke(this,
                            new SimpleHUDMessage(TranslationServer.Translate("NOTICE_ENGULF_DAMAGE_FROM_TOXIN"),
                                DisplayDuration.Short));
                    }
                }

                if (additionalCompounds?.ContainsKey(compound) == true)
                    additionalCompounds[compound] -= taken;

                engulfable.Compounds.TakeCompound(compound, taken);

                var takenAdjusted = taken * efficiency;
                var added = Compounds.AddCompound(compound, takenAdjusted);

                // Eject excess
                SpawnEjectedCompound(compound, takenAdjusted - added, Vector3.Back);
            }

            var initialTotalEngulfableCompounds = engulfedObject.InitialTotalEngulfableCompounds;

            if (initialTotalEngulfableCompounds.HasValue && initialTotalEngulfableCompounds.Value != 0)
            {
                engulfable.DigestedAmount = 1 -
                    (totalAmountLeft / initialTotalEngulfableCompounds.Value);
            }

            if (totalAmountLeft <= 0 || engulfable.DigestedAmount >= Constants.FULLY_DIGESTED_LIMIT)
            {
                engulfable.PhagocytosisStep = PhagocytosisPhase.Digested;
            }
            else
            {
                usedCapacity += engulfable.EngulfSize;
            }
        }

        UsedIngestionCapacity = usedCapacity;

        // Else handle logic if the cell that's being/has been digested is us
        if (PhagocytosisStep == PhagocytosisPhase.None)
        {
            if (DigestedAmount > 0 && DigestedAmount < Constants.PARTIALLY_DIGESTED_THRESHOLD)
            {
                // Cell is not too damaged, can heal itself in open environment and continue living
                DigestedAmount -= delta * Constants.ENGULF_COMPOUND_ABSORBING_PER_SECOND;
            }
        }
        else
        {
            // Species handling for the player microbe in case the process into partial digestion took too long
            // so here we want to limit how long the player should wait until they respawn
            if (IsPlayerMicrobe && PhagocytosisStep == PhagocytosisPhase.Ingested)
                playerEngulfedDeathTimer += delta;

            if (DigestedAmount >= Constants.PARTIALLY_DIGESTED_THRESHOLD || playerEngulfedDeathTimer >=
                Constants.PLAYER_ENGULFED_DEATH_DELAY_MAX)
            {
                playerEngulfedDeathTimer = 0;

                // Microbe is beyond repair, might as well consider it as dead
                Kill();

                if (IsPlayerMicrobe)
                {
                    // Playing from a positional audio player won't have any effect since the listener is
                    // directly on it.
                    PlayNonPositionalSoundEffect("res://assets/sounds/soundeffects/microbe-death-2.ogg", 0.5f);
                }

                var hostile = HostileEngulfer.Value;
                if (hostile == null)
                    return;

                // Transfer ownership of all the objects we engulfed to our engulfer
                foreach (var other in engulfedObjects.ToList())
                {
                    var engulfed = other.Object.Value;
                    if (engulfedObjects.Remove(other) && engulfed != null)
                    {
                        engulfed.HostileEngulfer.Value = hostile;
                        hostile.engulfedObjects.Add(other);
                        engulfed.EntityNode.ReParentWithTransform(hostile);
                    }
                }
            }
        }
    }

    private void CalculateBonusDigestibleGlucose(Dictionary<Compound, float> result)
    {
        result.TryGetValue(glucose, out float existingGlucose);
        result[glucose] = existingGlucose + Compounds.Capacity *
            Constants.ADDITIONAL_DIGESTIBLE_GLUCOSE_AMOUNT_MULTIPLIER;
    }

    private void HandleSlimeSecretion(float delta)
    {
        // Ignore if we have no slime jets
        if (SlimeJets.Count < 1)
            return;

        // Start a cooldown timer if we're out of mucilage to prevent visible trails or puffs when empty.
        // Scaling by slime jet count ensures we aren't producing mucilage fast enough to beat this check.
        if (compounds.GetCompoundAmount(mucilage) < Constants.MUCILAGE_MIN_TO_VENT * SlimeJets.Count)
            slimeSecretionCooldown = Constants.MUCILAGE_COOLDOWN_TIMER;

        // If we've been told to secrete slime and can do it, proceed
        if (queuedSlimeSecretionTime > 0 && slimeSecretionCooldown <= 0)
        {
            // Play a sound only if we've just started, i.e. only if no jets are already active
            if (SlimeJets.All(c => !c.Active))
                PlaySoundEffect("res://assets/sounds/soundeffects/microbe-slime-jet.ogg");

            // Activate all jets, which will constantly secrete slime until we turn them off
            foreach (var jet in SlimeJets)
                jet.Active = true;
        }
        else
        {
            // Deactivate the jets if we aren't supposed to secrete slime
            foreach (var jet in SlimeJets)
                jet.Active = false;
        }

        queuedSlimeSecretionTime -= delta;
        if (queuedSlimeSecretionTime < 0)
            queuedSlimeSecretionTime = 0;
    }

    private void UpdateDissolveEffect()
    {
        Membrane.DissolveEffectValue = dissolveEffectValue;

        foreach (var organelle in organelles!)
        {
            organelle.DissolveEffectValue = dissolveEffectValue;

            if (IsForPreviewOnly || PhagocytosisStep == PhagocytosisPhase.Ingested)
            {
                organelle.UpdateAsync(0);
                organelle.UpdateSync();
            }
        }
    }

    private void CountOrganelleMaxRenderPriority()
    {
        cachedOrganelleMaxRenderPriority = 0;

        if (organelles == null)
            return;

        cachedOrganelleMaxRenderPriority = organelles.MaxRenderPriority;
        organelleMaxRenderPriorityDirty = false;
    }
}
