﻿namespace Systems;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles reproduction progress in microbes that are not in aa cell colony. <see cref="AttachedToEntity"/> is
///   used to skip reproduction for engulfed cells or cells in colonies.
/// </summary>
/// <remarks>
///   <para>
///     This just needs an exclusive property for this system that is stored in <see cref="MicrobeStatus"/> so
///     this is marked just as reading that component as it won't conflict with other writes.
///   </para>
///   <para>
///     This needs to run on the main thread as this updates scale of organelles as they grow (Godot scale is
///     used directly)
///   </para>
/// </remarks>
[With(typeof(ReproductionStatus))]
[With(typeof(OrganelleContainer))]
[With(typeof(MicrobeStatus))]
[With(typeof(CompoundStorage))]
[With(typeof(CellProperties))]
[With(typeof(MicrobeSpeciesMember))]
[With(typeof(Health))]
[With(typeof(BioProcesses))]
[With(typeof(WorldPosition))]
[Without(typeof(AttachedToEntity))]
[Without(typeof(EarlyMulticellularSpeciesMember))]
[WritesToComponent(typeof(Engulfable))]
[WritesToComponent(typeof(Engulfer))]
[WritesToComponent(typeof(TemporaryEndosymbiontInfo))]
[ReadsComponent(typeof(MicrobeStatus))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(MicrobeEventCallbacks))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(SoundEffectPlayer))]
[RunsAfter(typeof(OsmoregulationAndHealingSystem))]
[RunsAfter(typeof(ProcessSystem))]
[RuntimeCost(14)]
[RunsOnMainThread]
public sealed class MicrobeReproductionSystem : AEntitySetSystem<float>
{
    private readonly IWorldSimulation worldSimulation;
    private readonly IMicrobeSpawnEnvironment spawnEnvironment;
    private readonly ISpawnSystem spawnSystem;

    private readonly ConcurrentStack<PlacedOrganelle> organellesNeedingScaleUpdate = new();

    // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
    // private readonly ThreadLocal<List<PlacedOrganelle>> organellesToSplit = new(() => new List<PlacedOrganelle>());
    // private readonly ThreadLocal<List<Compound>> compoundWorkData = new(() => new List<Compound>());
    // private readonly ThreadLocal<List<Hex>> hexWorkData = new(() => new List<Hex>());
    // private readonly ThreadLocal<List<Hex>> hexWorkData2 = new(() => new List<Hex>());

    private readonly List<PlacedOrganelle> organellesToSplit = new();
    private readonly List<Compound> compoundWorkData = new();
    private readonly List<Hex> hexWorkData = new();
    private readonly List<Hex> hexWorkData2 = new();

    private GameWorld? gameWorld;

    private float reproductionDelta;

    public MicrobeReproductionSystem(IWorldSimulation worldSimulation, IMicrobeSpawnEnvironment spawnEnvironment,
        ISpawnSystem spawnSystem, World world, IParallelRunner parallelRunner) :
        base(world, parallelRunner, Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
    {
        this.worldSimulation = worldSimulation;
        this.spawnEnvironment = spawnEnvironment;
        this.spawnSystem = spawnSystem;
    }

    public static (float RemainingAllowedCompoundUse, float RemainingFreeCompounds)
        CalculateFreeCompoundsAndLimits(WorldGenerationSettings worldSettings, int hexCount, bool isMulticellular,
            float delta)
    {
        // Skip some computations when they are not needed
        if (!worldSettings.PassiveGainOfReproductionCompounds &&
            !worldSettings.LimitReproductionCompoundUseSpeed)
        {
            return (float.MaxValue, 0);
        }

        // TODO: make the current patch affect this?
        // TODO: make being in a colony affect this
        float remainingFreeCompounds = Constants.MICROBE_REPRODUCTION_FREE_COMPOUNDS *
            (hexCount * Constants.MICROBE_REPRODUCTION_FREE_RATE_FROM_HEX + 1.0f) * delta;

        if (isMulticellular)
            remainingFreeCompounds *= Constants.EARLY_MULTICELLULAR_REPRODUCTION_COMPOUND_MULTIPLIER;

        float remainingAllowedCompoundUse = float.MaxValue;

        if (worldSettings.LimitReproductionCompoundUseSpeed)
        {
            remainingAllowedCompoundUse = remainingFreeCompounds * Constants.MICROBE_REPRODUCTION_MAX_COMPOUND_USE;
        }

        // Reset the free compounds if we don't want to give free compounds.
        // It was necessary to calculate for the above math to be able to use it, but we don't want it to apply when
        // not enabled.
        if (!worldSettings.PassiveGainOfReproductionCompounds)
        {
            remainingFreeCompounds = 0;
        }

        return (remainingAllowedCompoundUse, remainingFreeCompounds);
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    public override void Dispose()
    {
        Dispose(true);
        base.Dispose();
    }

    /// <summary>
    ///   Processes the base cost of reproduction (i.e. non-organelle or placed cell related cost). This is
    ///   internal to allow <see cref="MulticellularGrowthSystem"/> to access this. This requires a working list
    ///   memory to avoid memory allocations each call (<see cref="tempStorageForProcessing"/>).
    /// </summary>
    /// <returns>True when this stage of reproduction is done</returns>
    internal static bool ProcessBaseReproductionCost(Dictionary<Compound, float>? requiredCompoundsForBaseReproduction,
        CompoundBag compounds, ref float remainingAllowedCompoundUse,
        ref float remainingFreeCompounds, bool consumeInReverseOrder,
        List<Compound> tempStorageForProcessing, Dictionary<Compound, float>? trackCompoundUse = null)
    {
        // If no info created yet, we don't know if we are done
        if (requiredCompoundsForBaseReproduction == null)
            return false;

        if (remainingAllowedCompoundUse <= 0)
        {
            return false;
        }

        tempStorageForProcessing.Clear();

        // Prepare the compound types to process
        foreach (var entry in requiredCompoundsForBaseReproduction)
        {
            if (entry.Value > 0)
            {
                tempStorageForProcessing.Add(entry.Key);
            }
        }

        bool reproductionStageComplete = true;

        int count = tempStorageForProcessing.Count;

        if (count > 0)
        {
            if (consumeInReverseOrder)
            {
                for (int i = count - 1; i >= 0; --i)
                {
                    ProcessBaseReproductionForCompoundType(tempStorageForProcessing[i],
                        requiredCompoundsForBaseReproduction, compounds, ref remainingAllowedCompoundUse,
                        ref remainingFreeCompounds, trackCompoundUse, ref reproductionStageComplete);
                }
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    ProcessBaseReproductionForCompoundType(tempStorageForProcessing[i],
                        requiredCompoundsForBaseReproduction, compounds, ref remainingAllowedCompoundUse,
                        ref remainingFreeCompounds, trackCompoundUse, ref reproductionStageComplete);
                }
            }
        }

        return reproductionStageComplete;
    }

    protected override void PreUpdate(float delta)
    {
        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");

        base.PreUpdate(delta);

        reproductionDelta = delta;

        // TODO: rate limit how often reproduction update is allowed to run?
        // // Limit how often the reproduction logic is ran
        // if (lastCheckedReproduction < Constants.MICROBE_REPRODUCTION_PROGRESS_INTERVAL)
        //     return;

        while (organellesNeedingScaleUpdate.TryPop(out _))
        {
            GD.PrintErr("Organelles needing scale list is not empty like it should before a system run");
        }
    }

    protected override void Update(float state, in Entity entity)
    {
        ref var health = ref entity.Get<Health>();

        // Dead cells can't reproduce
        if (health.Dead)
            return;

        ref var organelles = ref entity.Get<OrganelleContainer>();

        if (organelles.AllOrganellesDivided)
        {
            // Ready to reproduce already. Only the player gets here as other cells split and reset automatically
            return;
        }

        ref var status = ref entity.Get<MicrobeStatus>();

        status.ConsumeReproductionCompoundsReverse = !status.ConsumeReproductionCompoundsReverse;

        bool isInColony = entity.Has<MicrobeColony>();

        if (isInColony)
        {
            // TODO: should the colony just passively get the reproduction compounds in its storage?
            // Otherwise early multicellular colonies lose the passive reproduction feature
            return;
        }

        HandleNormalMicrobeReproduction(entity, ref organelles, status.ConsumeReproductionCompoundsReverse);
    }

    protected override void PostUpdate(float state)
    {
        base.PostUpdate(state);

        bool printedError = false;

        // Apply scales
        while (organellesNeedingScaleUpdate.TryPop(out var organelle))
        {
            if (organelle.OrganelleGraphics == null)
                continue;

            // The parent node of the organelle graphics is what needs to be scaled
            // TODO: check if it would be better to just store this node directly in the PlacedOrganelle to not
            // re-read it like this
            var nodeToScale = organelle.OrganelleGraphics.GetParentSpatialWorking();

            // This should no longer happen with the working spatial fetch, but just for safety this is kept
            if (nodeToScale == null)
            {
                if (!printedError)
                {
                    GD.PrintErr("Organelle is missing Spatial parent, cannot apply scale change");
                    printedError = true;
                }

                continue;
            }

            if (!organelle.Definition.PositionedExternally)
            {
                nodeToScale.Transform = organelle.CalculateVisualsTransform();
            }
            else
            {
                nodeToScale.Transform = organelle.CalculateVisualsTransformExternalCached();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessBaseReproductionForCompoundType(Compound compound,
        Dictionary<Compound, float> requiredCompoundsForBaseReproduction,
        CompoundBag compounds, ref float remainingAllowedCompoundUse, ref float remainingFreeCompounds,
        Dictionary<Compound, float>? trackCompoundUse, ref bool reproductionStageComplete)
    {
        var amountNeeded = requiredCompoundsForBaseReproduction[compound];

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

        // For consistency, we apply the ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST constant here like for
        // organelle growth
        var amountAvailable =
            compounds.GetCompoundAmount(compound) - Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

        if (amountAvailable > MathUtils.EPSILON)
        {
            // We can take some
            var amountToTake = Mathf.Min(allowedUseAmount, amountAvailable);

            usedAmount += compounds.TakeCompound(compound, amountToTake);
        }

        if (usedAmount < MathUtils.EPSILON)
        {
            reproductionStageComplete = false;
            return;
        }

        remainingAllowedCompoundUse -= usedAmount;

        if (trackCompoundUse != null)
        {
            trackCompoundUse.TryGetValue(compound, out var trackedAlreadyUsed);
            trackCompoundUse[compound] = trackedAlreadyUsed + usedAmount;
        }

        var left = amountNeeded - usedAmount;

        if (left < 0.0001f)
        {
            // We don't remove these values even when empty as we rely on detecting this being empty for earlier
            // save compatibility, so we just leave 0 values in requiredCompoundsForBaseReproduction
            left = 0;
        }
        else
        {
            // Still something left
            reproductionStageComplete = false;
        }

        requiredCompoundsForBaseReproduction[compound] = left;
    }

    /// <summary>
    ///   Handles feeding the organelles in a microbe in order for them to split. After all are split the microbe
    ///   is ready to reproduce. This is allowed to be called only for non-multicellular growth only (and not in
    ///   a cell colony)
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     AI cells will immediately reproduce when they can. On the player cell the editor is unlocked when
    ///     reproducing is possible.
    ///   </para>
    /// </remarks>
    private void HandleNormalMicrobeReproduction(in Entity entity, ref OrganelleContainer organelles,
        bool consumeInReverseOrder)
    {
        // Skip not initialized microbes yet
        if (organelles.Organelles == null)
            return;

        var (remainingAllowedCompoundUse, remainingFreeCompounds) =
            CalculateFreeCompoundsAndLimits(gameWorld!.WorldSettings, organelles.HexCount, false,
                reproductionDelta);

        ref var storage = ref entity.Get<CompoundStorage>();
        var compounds = storage.Compounds;

        ref var baseReproduction = ref entity.Get<ReproductionStatus>();

        // Process base cost first so the player can be their designed cell (without extra organelles) for a while
        bool reproductionStageComplete;

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
        lock (compoundWorkData)
        {
            reproductionStageComplete = ProcessBaseReproductionCost(
                baseReproduction.MissingCompoundsForBaseReproduction, compounds,
                ref remainingAllowedCompoundUse, ref remainingFreeCompounds,
                consumeInReverseOrder, compoundWorkData);
        }

        // For this stage and all others below, reproductionStageComplete tracks whether the previous reproduction
        // stage completed, i.e. whether we should proceed with the next stage
        if (reproductionStageComplete)
        {
            // Organelles that are ready to split

            // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
            var organellesToAdd = organellesToSplit;

            lock (organellesToAdd)
            {
                // Grow all the organelles, except the unique organelles which are given compounds last
                // Manual loops are used here as profiling showed the reproduction system enumerator allocations caused
                // quite a lot of memory allocations during gameplay
                var organelleCount = organelles.Organelles.Count;
                for (int i = 0; i < organelleCount; ++i)
                {
                    var organelle = organelles.Organelles[i];

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
                    bool grown = organelle.GrowOrganelle(compounds, ref remainingAllowedCompoundUse,
                        ref remainingFreeCompounds, consumeInReverseOrder);

                    if (organelle.GrowthValue >= 1.0f)
                    {
                        // Queue this organelle for splitting after the loop.
                        organellesToAdd.Add(organelle);
                    }
                    else
                    {
                        // Needs more stuff
                        reproductionStageComplete = false;

                        // When not splitting, just the scale needs to be potentially updated
                        if (grown)
                        {
                            organellesNeedingScaleUpdate.Push(organelle);
                        }
                    }

                    // TODO: can we quit this loop early if we still would have dozens of organelles to check but
                    // don't have any compounds left to give them (that are probably useful)?
                }

                // Splitting the queued organelles.
                if (organellesToAdd.Count > 0)
                {
                    SplitQueuedOrganelles(organellesToAdd, entity, ref organelles, ref storage);
                    organellesToAdd.Clear();
                }
            }
        }

        if (reproductionStageComplete)
        {
            var organelleCount = organelles.Organelles!.Count;
            for (int i = 0; i < organelleCount; ++i)
            {
                var organelle = organelles.Organelles[i];

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

                // Unique organelles don't split, so we use the growth value to know when something is fully grown
                if (organelle.GrowthValue < 1.0f)
                {
                    if (organelle.GrowOrganelle(compounds, ref remainingAllowedCompoundUse,
                            ref remainingFreeCompounds, consumeInReverseOrder))
                    {
                        organellesNeedingScaleUpdate.Push(organelle);
                    }

                    // Nucleus (or another unique organelle) needs more compounds
                    reproductionStageComplete = false;
                }
            }
        }

        if (reproductionStageComplete)
        {
            // All organelles and base reproduction cost is now fulfilled, we are fully ready to reproduce
            organelles.AllOrganellesDivided = true;

            // For NPC cells this immediately splits them and the allOrganellesDivided flag is reset
            ReadyToReproduce(entity, ref organelles);
        }
    }

    private void SplitQueuedOrganelles(List<PlacedOrganelle> organellesToAdd,
        Entity entity, ref OrganelleContainer organelles, ref CompoundStorage storage)
    {
        foreach (var organelle in organellesToAdd)
        {
            // Mark this organelle as done and return to its normal size.
            organelle.ResetGrowth();
            organellesNeedingScaleUpdate.Push(organelle);

            // This doesn't need to update individual scales as a full organelles change is queued below for
            // a different system to handle

            organelle.WasSplit = true;

            // Create a second organelle.
            var organelle2 = SplitOrganelle(organelles.Organelles!, organelle);
            organelle2.WasSplit = true;
            organelle2.IsDuplicate = true;
            organelle2.SisterOrganelle = organelle;

            // These are fetched here as most of the time only one organelle will divide per step so it doesn't
            // help to complicate things by trying to fetch these before the loop
            organelles.OnOrganellesChanged(ref storage, ref entity.Get<BioProcesses>(),
                ref entity.Get<Engulfer>(), ref entity.Get<Engulfable>(),
                ref entity.Get<CellProperties>());

            if (entity.Has<MicrobeEventCallbacks>())
            {
                ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

                callbacks.OnOrganelleDuplicated?.Invoke(entity, organelle2);
            }
        }
    }

    private PlacedOrganelle SplitOrganelle(OrganelleLayout<PlacedOrganelle> organelles, PlacedOrganelle organelle)
    {
        var q = organelle.Position.Q;
        var r = organelle.Position.R;

        // The position used here will be overridden with the right value when we manage to find a place
        // for this organelle
        var newOrganelle = new PlacedOrganelle(organelle.Definition, new Hex(q, r), 0, organelle.Upgrades);

        var workData1 = hexWorkData;
        var workData2 = hexWorkData2;

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
        lock (workData1)
        {
            lock (workData2)
            {
                // Spiral search for space for the organelle
                organelles.FindAndPlaceAtValidPosition(newOrganelle, q, r, workData1, workData2);
            }
        }

        return newOrganelle;
    }

    /// <summary>
    ///   Called when a microbe is ready to reproduce. Divides this microbe (if this isn't the player).
    /// </summary>
    private void ReadyToReproduce(in Entity entity, ref OrganelleContainer organelles)
    {
        Action<Entity, bool>? reproductionCallback;
        if (entity.Has<MicrobeEventCallbacks>())
        {
            ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();
            reproductionCallback = callbacks.OnReproductionStatus;
        }
        else
        {
            reproductionCallback = null;
        }

        // Entities with a reproduction callback don't divide automatically
        if (reproductionCallback != null)
        {
            // The player doesn't split automatically
            organelles.AllOrganellesDivided = true;

            reproductionCallback.Invoke(entity, true);
        }
        else
        {
            // Skip reproducing if we would go too much over the entity limit
            if (!spawnSystem.IsUnderEntityLimitForReproducing())
            {
                // Set this to false so that we re-check in a few frames if we can reproduce then
                organelles.AllOrganellesDivided = false;
                return;
            }

            var species = entity.Get<MicrobeSpeciesMember>().Species;

            if (!species.PlayerSpecies)
            {
                gameWorld!.AlterSpeciesPopulationInCurrentPatch(species,
                    Constants.CREATURE_REPRODUCE_POPULATION_GAIN, Localization.Translate("REPRODUCED"));
            }

            ref var cellProperties = ref entity.Get<CellProperties>();

            var workData1 = hexWorkData;
            var workData2 = hexWorkData2;

            // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
            lock (workData1)
            {
                lock (workData2)
                {
                    // Return the first cell to its normal, non duplicated cell arrangement and spawn a daughter cell
                    organelles.ResetOrganelleLayout(ref entity.Get<CompoundStorage>(),
                        ref entity.Get<BioProcesses>(),
                        entity, species, species, worldSimulation, workData1, workData2);

                    // This is purely inside this lock to suppress a warning on worldSimulation
                    cellProperties.Divide(ref organelles, entity, species, worldSimulation, spawnEnvironment,
                        spawnSystem, null);
                }
            }
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            // TODO: https://github.com/Revolutionary-Games/Thrive/issues/4989
            /*organellesToSplit.Dispose();
            compoundWorkData.Dispose();
            hexWorkData.Dispose();
            hexWorkData2.Dispose();*/
        }
    }
}
