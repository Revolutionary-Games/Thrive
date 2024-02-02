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
    ///   Handles growth in multicellular cell colonies
    /// </summary>
    [With(typeof(EarlyMulticellularSpeciesMember))]
    [With(typeof(MulticellularGrowth))]
    [With(typeof(CompoundStorage))]
    [With(typeof(MicrobeStatus))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(Health))]
    [Without(typeof(AttachedToEntity))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(ProcessSystem))]
    [RunsAfter(typeof(ColonyCompoundDistributionSystem))]
    public sealed class MulticellularGrowthSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;
        private readonly ISpawnSystem spawnSystem;
        private GameWorld? gameWorld;

        public MulticellularGrowthSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world,
            IParallelRunner runner) : base(world, runner, Constants.SYSTEM_LOW_ENTITIES_PER_THREAD)
        {
            this.worldSimulation = worldSimulation;
            this.spawnSystem = spawnSystem;
        }

        public void SetWorld(GameWorld world)
        {
            gameWorld = world;
        }

        protected override void PreUpdate(float delta)
        {
            if (gameWorld == null)
                throw new InvalidOperationException("GameWorld not set");

            base.PreUpdate(delta);
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var health = ref entity.Get<Health>();

            // Dead multicellular colonies can't reproduce
            if (health.Dead)
                return;

            ref var growth = ref entity.Get<MulticellularGrowth>();
            HandleMulticellularReproduction(ref growth, entity, delta);
        }

        private void HandleMulticellularReproduction(ref MulticellularGrowth multicellularGrowth, in Entity entity,
            float elapsedSinceLastUpdate)
        {
            ref var speciesData = ref entity.Get<EarlyMulticellularSpeciesMember>();

            var compounds = entity.Get<CompoundStorage>().Compounds;

            ref var organelleContainer = ref entity.Get<OrganelleContainer>();

            ref var status = ref entity.Get<MicrobeStatus>();

            status.ConsumeReproductionCompoundsReverse = !status.ConsumeReproductionCompoundsReverse;

            ref var baseReproduction = ref entity.Get<ReproductionStatus>();

            multicellularGrowth.CompoundsUsedForMulticellularGrowth ??= new Dictionary<Compound, float>();

            var (remainingAllowedCompoundUse, remainingFreeCompounds) =
                MicrobeReproductionSystem.CalculateFreeCompoundsAndLimits(gameWorld!.WorldSettings,
                    organelleContainer.HexCount, false, elapsedSinceLastUpdate);

            if (multicellularGrowth.CompoundsNeededForNextCell == null)
            {
                // Regrow lost cells
                if (multicellularGrowth.LostPartsOfBodyPlan is { Count: > 0 })
                {
                    // Store where we will resume from
                    multicellularGrowth.ResumeBodyPlanAfterReplacingLost ??=
                        multicellularGrowth.NextBodyPlanCellToGrowIndex;

                    // Grow from the first cell to grow back, in the body plan grow order
                    multicellularGrowth.NextBodyPlanCellToGrowIndex = multicellularGrowth.LostPartsOfBodyPlan.Min();

                    if (multicellularGrowth.NextBodyPlanCellToGrowIndex <= 0)
                        throw new InvalidOperationException("Loaded bad next body plan index from regrow lost");

                    multicellularGrowth.LostPartsOfBodyPlan.Remove(multicellularGrowth.NextBodyPlanCellToGrowIndex);

                    // TODO: should this skip regrowing cells that already exist for some reason in the body?
                    // That can happen due to a problem elsewhere but then this will cause a duplicate cell to appear
                    // which will get reported by anyone seeing it
                }
                else if (multicellularGrowth.ResumeBodyPlanAfterReplacingLost != null)
                {
                    // Done regrowing, resume where we were
                    multicellularGrowth.NextBodyPlanCellToGrowIndex =
                        multicellularGrowth.ResumeBodyPlanAfterReplacingLost.Value;
                    multicellularGrowth.ResumeBodyPlanAfterReplacingLost = null;
                }

                // Need to setup the next cell to be grown in our body plan
                if (multicellularGrowth.IsFullyGrownMulticellular)
                {
                    // We have completed our body plan and can (once enough resources) reproduce
                    if (multicellularGrowth.EnoughResourcesForBudding)
                    {
                        ReadyToReproduce(ref organelleContainer, ref multicellularGrowth, ref baseReproduction, entity,
                            speciesData.Species);
                    }
                    else
                    {
                        // Apply the base reproduction cost at this point after growing the full layout

                        if (!MicrobeReproductionSystem.ProcessBaseReproductionCost(
                                baseReproduction.MissingCompoundsForBaseReproduction, compounds,
                                ref remainingAllowedCompoundUse,
                                ref remainingFreeCompounds, status.ConsumeReproductionCompoundsReverse,
                                multicellularGrowth.CompoundsUsedForMulticellularGrowth))
                        {
                            // Not ready yet for budding
                            return;
                        }

                        // Budding cost is after the base reproduction cost has been overcome
                        multicellularGrowth.CompoundsNeededForNextCell =
                            multicellularGrowth.GetCompoundsNeededForNextCell(speciesData.Species);
                    }

                    return;
                }

                multicellularGrowth.CompoundsNeededForNextCell =
                    multicellularGrowth.GetCompoundsNeededForNextCell(speciesData.Species);
            }

            bool stillNeedsSomething = false;

            ref var microbeStatus = ref entity.Get<MicrobeStatus>();
            microbeStatus.ConsumeReproductionCompoundsReverse = !microbeStatus.ConsumeReproductionCompoundsReverse;

            // Consume some compounds for the next cell in the layout
            // Similar logic for "growing" more cells than in PlacedOrganelle growth
            foreach (var entry in microbeStatus.ConsumeReproductionCompoundsReverse ?
                         multicellularGrowth.CompoundsNeededForNextCell.Reverse() :
                         multicellularGrowth.CompoundsNeededForNextCell)
            {
                var amountNeeded = entry.Value;

                float usedAmount = 0;

                float allowedUseAmount = Math.Min(amountNeeded, remainingAllowedCompoundUse);

                if (remainingFreeCompounds > 0)
                {
                    var usedFreeCompounds = Math.Min(allowedUseAmount, remainingFreeCompounds);
                    usedAmount += usedFreeCompounds;
                    allowedUseAmount -= usedFreeCompounds;

                    // As we loop just once we don't need to update the free compounds or allowed use compounds
                    // variables
                }

                stillNeedsSomething = true;

                var amountAvailable = compounds.GetCompoundAmount(entry.Key) -
                    Constants.ORGANELLE_GROW_STORAGE_MUST_HAVE_AT_LEAST;

                if (amountAvailable > MathUtils.EPSILON)
                {
                    // We can take some
                    var amountToTake = Mathf.Min(allowedUseAmount, amountAvailable);

                    usedAmount += compounds.TakeCompound(entry.Key, amountToTake);
                }

                var left = amountNeeded - usedAmount;

                if (left < 0.0001f)
                {
                    multicellularGrowth.CompoundsNeededForNextCell.Remove(entry.Key);
                }
                else
                {
                    multicellularGrowth.CompoundsNeededForNextCell[entry.Key] = left;
                }

                multicellularGrowth.CompoundsUsedForMulticellularGrowth!.TryGetValue(entry.Key, out float alreadyUsed);

                multicellularGrowth.CompoundsUsedForMulticellularGrowth[entry.Key] = alreadyUsed + usedAmount;

                // As we modify the list, we are content just consuming one type of compound per frame
                break;
            }

            if (!stillNeedsSomething)
            {
                // The current cell to grow is now ready to be added
                // Except in the case that we were just getting resources for budding, skip in that case
                if (!multicellularGrowth.IsFullyGrownMulticellular)
                {
                    var recorder = worldSimulation.StartRecordingEntityCommands();

                    multicellularGrowth.AddMulticellularGrowthCell(entity, speciesData.Species, worldSimulation,
                        recorder, spawnSystem);

                    worldSimulation.FinishRecordingEntityCommands(recorder);
                }
                else
                {
                    // Has collected enough resources to spawn the first cell type as budding type reproduction
                    multicellularGrowth.EnoughResourcesForBudding = true;
                    multicellularGrowth.CompoundsNeededForNextCell = null;
                }
            }
        }

        private void ReadyToReproduce(ref OrganelleContainer organelles, ref MulticellularGrowth multicellularGrowth,
            ref ReproductionStatus baseReproduction, in Entity entity, EarlyMulticellularSpecies species)
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
                multicellularGrowth.EnoughResourcesForBudding = false;

                // Let's require the base reproduction cost to be fulfilled again as well, to keep down the colony
                // spam, and for consistency with non-multicellular microbes
                baseReproduction.SetupRequiredBaseReproductionCompounds(species);

                // Total cost may have changed so recalculate that
                multicellularGrowth.CalculateTotalBodyPlanCompounds(species);

                SpawnEarlyMulticellularOffspring(ref organelles, in entity, species);
            }
        }

        private void SpawnEarlyMulticellularOffspring(ref OrganelleContainer organelles, in Entity entity,
            EarlyMulticellularSpecies species)
        {
            // Skip reproducing if we would go too much over the entity limit
            if (!spawnSystem.IsUnderEntityLimitForReproducing())
            {
                // For now this just loses the progress resources towards the reproduction and this will be checked
                // again when the budding cost is fulfilled again
                return;
            }

            if (!species.PlayerSpecies)
            {
                gameWorld!.AlterSpeciesPopulationInCurrentPatch(species,
                    Constants.CREATURE_REPRODUCE_POPULATION_GAIN, TranslationServer.Translate("REPRODUCED"));
            }

            ref var cellProperties = ref entity.Get<CellProperties>();

            try
            {
                // Create the colony bud (for now this is the only reproduction type)
                cellProperties.Divide(ref organelles, entity, species, worldSimulation, spawnSystem, null);
            }
            catch (Exception e)
            {
                // This catch helps if a colony member somehow got processed for the reproduction system and causes
                // an exception due to not being allowed to divide
                GD.PrintErr("Early multicellular cell divide failed: ", e);
            }
        }
    }
}
