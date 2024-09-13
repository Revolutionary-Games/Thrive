﻿namespace Systems;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles digestion of engulfed objects (and starting ejection of indigestible things).
///   <see cref="EngulfingSystem"/> for the system responsible for pulling in and ejecting engulfables.
/// </summary>
/// <remarks>
///   <para>
///     This system has a separate variable in <see cref="MicrobeStatus"/> that this updates, so this is marked as
///     just reading that component.
///   </para>
/// </remarks>
[With(typeof(Engulfer))]
[With(typeof(OrganelleContainer))]
[With(typeof(CompoundStorage))]
[With(typeof(MicrobeStatus))]
[With(typeof(CellProperties))]
[With(typeof(Health))]
[With(typeof(WorldPosition))]
[WritesToComponent(typeof(Engulfable))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(MicrobeStatus))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(MicrobeEventCallbacks))]
[ReadsComponent(typeof(SpeciesMember))]
[RunsAfter(typeof(EngulfingSystem))]
[RuntimeCost(2)]
public sealed class EngulfedDigestionSystem : AEntitySetSystem<float>
{
    private readonly CompoundCloudSystem compoundCloudSystem;
    private readonly IReadOnlyList<Compound> digestibleCompounds;

    private readonly Enzyme lipase;

    private GameWorld? gameWorld;

    public EngulfedDigestionSystem(CompoundCloudSystem compoundCloudSystem, World world,
        IParallelRunner parallelRunner) : base(world, parallelRunner, Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
    {
        this.compoundCloudSystem = compoundCloudSystem;
        var simulationParameters = SimulationParameters.Instance;
        digestibleCompounds = simulationParameters.GetAllCompounds().Values.Where(c => c.Digestible).Select(c => c.ID)
            .ToList();
        lipase = simulationParameters.GetEnzyme("lipase");
    }

    public void SetWorld(GameWorld world)
    {
        gameWorld = world;
    }

    protected override void PreUpdate(float state)
    {
        base.PreUpdate(state);

        if (gameWorld == null)
            throw new InvalidOperationException("GameWorld not set");
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var engulfer = ref entity.Get<Engulfer>();

        if (engulfer.EngulfedObjects == null || engulfer.EngulfedObjects.Count < 1)
        {
            // When something ejects its last engulfed object, the used engulfing size needs to be still updated

            // TODO: determine if it is faster to always write this value or compare first
            if (engulfer.UsedEngulfingCapacity > 0)
                engulfer.UsedEngulfingCapacity = 0;

            return;
        }

        ref var organelles = ref entity.Get<OrganelleContainer>();
        var compounds = entity.Get<CompoundStorage>().Compounds;

        HandleDigestion(entity, ref engulfer, ref organelles, compounds, delta);
    }

    /// <summary>
    ///   Absorbs compounds from ingested objects.
    /// </summary>
    private void HandleDigestion(in Entity entity, ref Engulfer engulfer, ref OrganelleContainer organelles,
        CompoundBag compounds, float delta)
    {
        // Skip if enzymes aren't calculated yet
        if (organelles.AvailableEnzymes == null)
            return;

        var engulferIsPlayer = entity.Has<PlayerMarker>();

        // TODO: if the entity is a colony with the player being the lead cell should that situation set
        // engulferIsPlayer?

        float usedCapacity = 0;

        ref var cellProperties = ref entity.Get<CellProperties>();
        ref var position = ref entity.Get<WorldPosition>();

        for (int i = engulfer.EngulfedObjects!.Count - 1; i >= 0; --i)
        {
            var engulfedObject = engulfer.EngulfedObjects![i];

#if DEBUG
            if (!engulfedObject.IsAlive)
            {
                throw new Exception(
                    "Digestion system has a non-alive engulfed object, engulfing system should have taken care " +
                    "of this before it reached us");
            }
#endif

            if (!engulfedObject.Has<Engulfable>())
            {
                GD.PrintErr("Microbe has engulfed object that isn't engulfable");
                continue;
            }

            ref var engulfable = ref engulfedObject.Get<Engulfable>();

            var currentEngulfableSize = engulfable.AdjustedEngulfSize;

            // Expel this engulfed object if the cell loses some of its size and its ingestion capacity is overloaded
            if (usedCapacity + currentEngulfableSize - MathUtils.EPSILON > engulfer.EngulfStorageSize)
            {
                if (engulfer.EjectEngulfable(ref engulfable))
                {
                    entity.SendNoticeIfPossible(
                        new SimpleHUDMessage(Localization.Translate("NOTICE_ENGULF_STORAGE_FULL")));
                }

                // As ejecting is delayed, we need to temporarily not count this in the used capacity otherwise we may
                // accidentally eject way too much stuff
                continue;
            }

            // Doesn't make sense to digest non ingested objects, i.e. objects that are being engulfed,
            // being ejected, etc. So skip them.
            if (engulfable.PhagocytosisStep != PhagocytosisPhase.Ingested)
            {
                // Still need to consider the size of this thing for the engulf storage, otherwise cells can start
                // pulling in too much
                usedCapacity += currentEngulfableSize;

                if (engulfable.PhagocytosisStep == PhagocytosisPhase.None)
                {
                    GD.PrintErr("Engulfed object is in engulfed list while being not in engulfed state");
                }

                continue;
            }

            Enzyme usedEnzyme;

            var digestibility = organelles.CanDigestObject(ref engulfable);

            switch (digestibility)
            {
                case DigestCheckResult.Ok:
                {
                    usedEnzyme = engulfable.RequisiteEnzymeToDigest ?? lipase;

                    // TODO: only call this once
                    engulfable.OnReportBecomeIngestedIfCallbackRegistered(engulfedObject);

                    break;
                }

                case DigestCheckResult.MissingEnzyme:
                {
                    if (engulfer.EjectEngulfable(ref engulfable))
                    {
                        entity.SendNoticeIfPossible(new LocalizedString("NOTICE_ENGULF_MISSING_ENZYME",
                            engulfable.RequisiteEnzymeToDigest!.Name));
                    }

                    // Count the size still in this case as otherwise there's a one frame flicker of the matter
                    // storage bar if engulfing just something that cannot be digested
                    usedCapacity += currentEngulfableSize;

                    continue;
                }

                default:
                    throw new InvalidOperationException("Unhandled digestibility check result, won't digest");
            }

            CompoundBag? containedCompounds = null;

            if (engulfedObject.Has<CompoundStorage>())
            {
                containedCompounds = engulfedObject.Get<CompoundStorage>().Compounds;
            }

            var additionalCompounds = engulfable.AdditionalEngulfableCompounds;

            // TODO: this seems not possible to run in parallel
            // This is maybe now no longer required as engulfed things keep running the process system and that
            // should clamp and fix NaN values.
            // Workaround to avoid NaN compounds in engulfed objects, leading to glitches like infinite compound
            // ejection and incorrect ingested matter display
            // https://github.com/Revolutionary-Games/Thrive/issues/3548
            // containedCompounds?.FixNaNCompounds();

            var totalAmountLeft = 0.0f;

            var digestibleCount = digestibleCompounds.Count;

            // Manual loop to ensure no enumerator allocations
            for (int j = 0; j < digestibleCount; ++j)
            {
                var compound = digestibleCompounds[j];

                var storageAmount = containedCompounds?.GetCompoundAmount(compound) ?? 0;

                var additionalAmount = 0.0f;
                additionalCompounds?.TryGetValue(compound, out additionalAmount);

                if (additionalAmount < 0)
                {
#if DEBUG
                    if (Debugger.IsAttached)
                        Debugger.Break();
#endif

                    additionalAmount = 0;
                    GD.PrintErr("Additional compound amount is negative");
                }

                var totalAvailable = storageAmount + additionalAmount;
                totalAmountLeft += totalAvailable;

                if (totalAvailable <= 0)
                    continue;

                var amount =
                    MicrobeInternalCalculations.CalculateDigestionSpeed(organelles.AvailableEnzymes[usedEnzyme]);
                amount *= delta;

                // Efficiency starts from Constants.ENGULF_BASE_COMPOUND_ABSORPTION_YIELD up to
                // Constants.ENZYME_DIGESTION_EFFICIENCY_MAXIMUM. This means at least 7 lysosomes
                // are needed to achieve "maximum" efficiency
                var efficiency =
                    MicrobeInternalCalculations.CalculateDigestionEfficiency(organelles.AvailableEnzymes[usedEnzyme]);

                var taken = MathF.Min(totalAvailable, amount);

                // Toxin damage
                if (compound == Compound.Oxytoxy && taken > 0)
                {
                    ref var status = ref entity.Get<MicrobeStatus>();

                    status.LastCheckedOxytoxyDigestionDamage += delta;

                    if (status.LastCheckedOxytoxyDigestionDamage >= Constants.TOXIN_DIGESTION_DAMAGE_CHECK_INTERVAL)
                    {
                        status.LastCheckedOxytoxyDigestionDamage -= Constants.TOXIN_DIGESTION_DAMAGE_CHECK_INTERVAL;

                        ref var health = ref entity.Get<Health>();

                        health.DealMicrobeDamage(ref cellProperties,
                            health.MaxHealth * Constants.TOXIN_DIGESTION_DAMAGE_FRACTION, "oxytoxy");

                        entity.SendNoticeIfPossible(() => new SimpleHUDMessage(
                            Localization.Translate("NOTICE_ENGULF_DAMAGE_FROM_TOXIN"),
                            DisplayDuration.Short));
                    }
                }

                if (additionalCompounds?.ContainsKey(compound) == true)
                {
                    additionalCompounds[compound] -= taken;

                    if (additionalCompounds[compound] < 0)
                        additionalCompounds[compound] = 0;
                }

                if (engulfedObject.Has<CompoundStorage>())
                {
                    // TODO: shouldn't this read the amount of compounds actually taken here?
                    // This used to be like this even before the ECS conversion
                    engulfedObject.Get<CompoundStorage>().Compounds.TakeCompound(compound, taken);
                }

                var takenAdjusted = taken * efficiency;
                var added = compounds.AddCompound(compound, takenAdjusted);

                // Eject excess
                cellProperties.SpawnEjectedCompound(ref position, compoundCloudSystem, compound,
                    takenAdjusted - added, Vector3.Back);
            }

            var initialTotalEngulfableCompounds = engulfable.InitialTotalEngulfableCompounds;

            if (initialTotalEngulfableCompounds != 0)
            {
                engulfable.DigestedAmount = 1 - (totalAmountLeft / initialTotalEngulfableCompounds);

                // Digested amount can become negative if the calculated initial compounds is not accurate anymore
                if (engulfable.DigestedAmount < 0)
                    engulfable.DigestedAmount = 0;
            }
            else
            {
                GD.PrintErr("Engulfing system hasn't initialized InitialTotalEngulfableCompounds");
            }

            if (totalAmountLeft <= 0 || engulfable.DigestedAmount >= Constants.FULLY_DIGESTED_LIMIT)
            {
                engulfable.PhagocytosisStep = PhagocytosisPhase.Digested;

                if (engulfedObject.Has<CellProperties>())
                {
                    if (engulferIsPlayer)
                        gameWorld!.StatisticsTracker.TotalDigestedByPlayer.Increment(1);

                    // TODO: maybe allow non-player and other species to to also perform endosymbiosis
                    if (engulferIsPlayer && entity.Has<SpeciesMember>() && engulfedObject.Has<SpeciesMember>())
                    {
                        var engulferSpecies = entity.Get<SpeciesMember>().Species;
                        var targetSpecies = engulfedObject.Get<SpeciesMember>().Species;

                        // TODO: if all species can do endosymbiosis then there might be a need to clear out this every
                        // now and then to not keep species history around forever
                        var engulfedSpecies = engulferSpecies.Endosymbiosis.EngulfedSpecies;
                        engulfedSpecies.TryGetValue(targetSpecies, out var existingCount);
                        engulfedSpecies[targetSpecies] = existingCount + 1;

                        // TODO: maybe add a notice that the player might be able to make an endosymbiont. That
                        // shouldn't always trigger on each engulf but would be nice to have sometimes.
                    }
                }
            }

            // This is always applied, even when digested fully now. This is because EngulfingSystem will subtract
            // the engulfing size when ejecting an object so this ensures that a digested object cannot contribute
            // negative size for a short while. The digested object's impact will be correctly recalculated once
            // it is ejected and this system runs again.
            usedCapacity += engulfable.AdjustedEngulfSize;
        }

        engulfer.UsedEngulfingCapacity = usedCapacity;
    }
}
