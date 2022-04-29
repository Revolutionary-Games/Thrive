using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Main script on each cell in the game.
///   Partial class: Compounds, ATP, Reproduction
///   Divide, Organelles, Toxin
/// </summary>
public partial class Microbe
{
    /// <summary>
    ///   The stored compounds in this microbe
    /// </summary>
    [JsonProperty]
    public readonly CompoundBag Compounds = new(0.0f);

    private Compound atp = null!;

    [JsonProperty]
    private CompoundCloudSystem? cloudSystem;

    [JsonProperty]
    private Compound? queuedToxinToEmit;

    /// <summary>
    ///   The organelles in this microbe
    /// </summary>
    [JsonProperty]
    private OrganelleLayout<PlacedOrganelle>? organelles;

    [JsonProperty]
    private float lastCheckedATPDamage;

    private float lastCheckedReproduction;

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
    ///   All organelle nodes need to be added to this node to make scale work
    /// </summary>
    [JsonIgnore]
    public Spatial OrganelleParent { get; private set; } = null!;

    [JsonIgnore]
    public CompoundBag ProcessCompoundStorage => Compounds;

    /// <summary>
    ///   For use by the AI to do run and tumble to find compounds. Also used by player cell for tutorials
    /// </summary>
    [JsonProperty]
    public Dictionary<Compound, float> TotalAbsorbedCompounds { get; set; } = new();

    [JsonProperty]
    public float AgentEmissionCooldown { get; private set; }

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
        if (AgentEmissionCooldown > 0)
            return;

        // Only shoot if you have an agent vacuole.
        if (AgentVacuoleCount < 1)
            return;

        agentType ??= SimulationParameters.Instance.GetCompound("oxytoxy");

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
        var direction = (LookAtPoint - Translation).Normalized();

        var position = Translation + (direction * ejectionDistance);

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

        // Separate the two cells.
        var separation = new Vector3(Radius, 0, 0);

        if (Colony != null)
        {
            // When in a colony we approximate a much higher separation distance
            var colonyRadius = separation.x;

            foreach (var colonyMember in Colony.ColonyMembers)
            {
                if (colonyMember == this)
                    continue;

                var radius = colonyMember.Radius + Constants.COLONY_DIVIDE_EXTRA_DAUGHTER_OFFSET;

                // TODO: switch this to something else if this is too slow for large colonies
                var positionInColony = colonyMember.GlobalTransform.origin - currentPosition;

                var outerRadius = Math.Max(Math.Abs(positionInColony.x) + radius,
                    Math.Abs(positionInColony.z) + radius);

                if (outerRadius > colonyRadius)
                    colonyRadius = outerRadius;
            }

            separation = new Vector3(colonyRadius + Constants.COLONY_DIVIDE_EXTRA_DAUGHTER_OFFSET, 0, 0);
        }

        // Create the one daughter cell.
        var copyEntity = SpawnHelpers.SpawnMicrobe(Species, currentPosition + separation,
            GetParent(), SpawnHelpers.LoadMicrobeScene(), true, cloudSystem!, CurrentGame);

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

        return copyEntity;
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
            if (gatheredCompounds.TryGetValue(entry.Key, out var gathered))
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
            result.Merge(compoundsUsedForMulticellularGrowth);

        return result;
    }

    private void HandleCompoundAbsorbing(float delta)
    {
        // max here buffs compound absorbing for the smallest cells
        var grabRadius = Mathf.Max(Radius, 3.0f);

        cloudSystem!.AbsorbCompounds(GlobalTransform.origin, grabRadius, Compounds,
            TotalAbsorbedCompounds, delta, Membrane.Type.ResourceAbsorptionFactor);

        if (IsPlayerMicrobe && CheatManager.InfiniteCompounds)
        {
            var usefulCompounds = SimulationParameters.Instance.GetCloudCompounds().Where(Compounds.IsUseful);
            foreach (var usefulCompound in usefulCompounds)
                Compounds.AddCompound(usefulCompound, Compounds.Capacity - Compounds.GetCompoundAmount(usefulCompound));
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

        float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta;

        // Cloud types are ones that can be vented
        foreach (var type in SimulationParameters.Instance.GetCloudCompounds())
        {
            // Vent if not useful, or if overflowed the capacity
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
        // Dead cells can't reproduce
        if (Dead)
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

        lastCheckedReproduction = 0;

        // TODO: should we make it so that reproduction progress is only checked about max of 20 times per second?
        // might make a lot of cells use less CPU power

        // Multicellular microbes in a colony still run reproduction logic as long as they are the colony leader
        if (IsMulticellular && ColonyParent == null)
        {
            HandleMulticellularReproduction();
            return;
        }

        if (Colony != null)
            return;

        bool reproductionStageComplete = true;

        // Organelles that are ready to split
        var organellesToAdd = new List<PlacedOrganelle>();

        // Grow all the organelles, except the nucleus which is given compounds last
        foreach (var organelle in organelles!.Organelles)
        {
            // Check if already done
            if (organelle.WasSplit)
                continue;

            // We are in G1 phase of the cell cycle, duplicate all organelles.

            // Except the unique organelles
            if (organelle.Definition.Unique)
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

                // The nucleus hasn't finished replicating its DNA, give it some compounds.
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

            // For NPC cells this immediately splits them and the allOrganellesDivided flag is reset
            ReadyToReproduce();
        }
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
            if (!Species.PlayerSpecies)
            {
                GameWorld.AlterSpeciesPopulation(Species,
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
        var osmoregulationCost = (HexCount * CellTypeProperties.MembraneType.OsmoregulationFactor *
            Constants.ATP_COST_FOR_OSMOREGULATION) * delta;

        // 5% osmoregulation bonus per colony member
        if (Colony != null)
        {
            osmoregulationCost *= 20f / (20f + Colony.ColonyMembers.Count);
        }

        Compounds.TakeCompound(atp, osmoregulationCost);
    }

    private void HandleMovement(float delta)
    {
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
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;

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
        organelle.DetachAndQueueFree();

        processesDirty = true;
        cachedHexCountDirty = true;
        membraneOrganellePositionsAreDirty = true;
        hasSignalingAgent = null;

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
    private void SpawnEjectedCompound(Compound compound, float amount)
    {
        var amountToEject = amount * Constants.MICROBE_VENT_COMPOUND_MULTIPLIER;

        if (amountToEject <= 0)
            return;

        cloudSystem!.AddCloud(compound, amountToEject, CalculateNearbyWorldPosition());
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
}
