namespace Systems
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles digestion of engulfed objects
    /// </summary>
    [With(typeof(Engulfer))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(CompoundStorage))]
    [With(typeof(MicrobeStatus))]
    [With(typeof(Health))]
    public sealed class EngulfedDigestionSystem : AEntitySetSystem<float>
    {
        private readonly Compound oxytoxy;
        private readonly IReadOnlyCollection<Compound> digestibleCompounds;

        private readonly Enzyme lipase;

        public EngulfedDigestionSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
            var simulationParameters = SimulationParameters.Instance;
            oxytoxy = simulationParameters.GetCompound("oxytoxy");
            digestibleCompounds = simulationParameters.GetAllCompounds().Values.Where(c => c.Digestible).ToList();
            lipase = simulationParameters.GetEnzyme("lipase");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var engulfer = ref entity.Get<Engulfer>();

            if (engulfer.EngulfedObjects == null || engulfer.EngulfedObjects.Count < 1)
                return;

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

            float usedCapacity = 0;

            for (int i = engulfer.EngulfedObjects!.Count - 1; i >= 0; --i)
            {
                var engulfedObject = engulfer.EngulfedObjects[i];

                if (!engulfedObject.Has<Engulfable>())
                {
                    GD.PrintErr("Microbe has engulfed object that isn't engulfable");
                    continue;
                }

                ref var engulfable = ref engulfedObject.Get<Engulfable>();

                // Expel this engulfed object if the cell loses some of its size and its ingestion capacity
                // is overloaded
                if (engulfer.UsedIngestionCapacity > engulfer.EngulfStorageSize)
                {
                    engulfer.EjectEngulfable(engulfable);
                    continue;
                }

                // Doesn't make sense to digest non ingested objects, i.e. objects that are being engulfed,
                // being ejected, etc. So skip them.
                if (engulfable.PhagocytosisStep != PhagocytosisPhase.Ingested)
                    continue;

                Enzyme usedEnzyme;

                var digestibility = organelles.CanDigestObject(ref engulfable);

                switch (digestibility)
                {
                    case DigestCheckResult.Ok:
                    {
                        usedEnzyme = engulfable.RequisiteEnzymeToDigest ?? lipase;
                        break;
                    }

                    case DigestCheckResult.MissingEnzyme:
                    {
                        engulfer.EjectEngulfable(engulfable);

                        entity.SendNoticeIfPossible(new LocalizedString("NOTICE_ENGULF_MISSING_ENZYME",
                            engulfable.RequisiteEnzymeToDigest!.Name));
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

                // Workaround to avoid NaN compounds in engulfed objects, leading to glitches like infinite compound
                // ejection and incorrect ingested matter display
                // https://github.com/Revolutionary-Games/Thrive/issues/3548
                containedCompounds?.FixNaNCompounds();

                var totalAmountLeft = 0.0f;

                foreach (var compound in digestibleCompounds)
                {
                    var storageAmount = containedCompounds?.GetCompoundAmount(compound) ?? 0;

                    var additionalAmount = 0.0f;
                    additionalCompounds?.TryGetValue(compound, out additionalAmount);

                    var totalAvailable = storageAmount + additionalAmount;
                    totalAmountLeft += totalAvailable;

                    if (totalAvailable <= 0)
                        continue;

                    var amount = MicrobeInternalCalculations.CalculateDigestionSpeed(organelles.AvailableEnzymes[usedEnzyme]);
                    amount *= delta;

                    // Efficiency starts from Constants.ENGULF_BASE_COMPOUND_ABSORPTION_YIELD up to
                    // Constants.ENZYME_DIGESTION_EFFICIENCY_MAXIMUM. This means at least 7 lysosomes
                    // are needed to achieve "maximum" efficiency
                    var efficiency = MicrobeInternalCalculations.CalculateDigestionEfficiency(organelles.AvailableEnzymes[usedEnzyme]);

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
                    var added = compounds.AddCompound(compound, takenAdjusted);

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
                    usedCapacity += engulfable.AdjustedEngulfSize;
                }
            }

            engulfer.UsedIngestionCapacity = usedCapacity;
        }
    }
}
