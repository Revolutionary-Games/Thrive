namespace Systems
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
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
    [With(typeof(ReproductionStatus))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(MicrobeStatus))]
    [With(typeof(CompoundStorage))]
    [With(typeof(CellProperties))]
    [With(typeof(MicrobeSpeciesMember))]
    [With(typeof(Health))]
    [Without(typeof(AttachedToEntity))]
    public sealed class MicrobeReproductionSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;
        private readonly ISpawnSystem spawnSystem;

        private readonly ConcurrentStack<PlacedOrganelle> organellesNeedingScaleUpdate = new();

        private GameWorld? gameWorld;

        private float reproductionDelta;

        public MicrobeReproductionSystem(IWorldSimulation worldSimulation, ISpawnSystem spawnSystem, World world,
            IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
            this.worldSimulation = worldSimulation;
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

        protected override void PreUpdate(float delta)
        {
            if (gameWorld == null)
                throw new InvalidOperationException("GameWorld not set");

            base.PreUpdate(delta);

            // Limit how big progress spikes lag can cause
            if (delta > Constants.MICROBE_REPRODUCTION_MAX_DELTA_FRAME)
            {
                reproductionDelta = Constants.MICROBE_REPRODUCTION_MAX_DELTA_FRAME;
            }
            else
            {
                reproductionDelta = delta;
            }

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

            // Multicellular microbes in a colony still run reproduction logic as long as they are the colony leader
            // TODO: move this to a separate system for code clarity (and stop running those here with
            // WithoutAttribute)? Would result in a tiny bit of code duplication regarding the dead check etc.
            // This already only runs on entities that are only a microbe species members
            if (isInColony && entity.Has<EarlyMulticellularSpeciesMember>())
            {
                throw new NotImplementedException();

                // HandleMulticellularReproduction();
                return;
            }

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

            // Apply scales
            while (organellesNeedingScaleUpdate.TryPop(out var organelle))
            {
                if (organelle.OrganelleGraphics == null)
                    continue;

                // The parent node of the organelle graphics is what needs to be scaled
                // TODO: check if it would be better to just store this node directly in the PlacedOrganelle to not
                // re-read it like this
                var nodeToScale = organelle.OrganelleGraphics.GetParentSpatial();

                if (!organelle.Definition.PositionedExternally)
                {
                    nodeToScale.Transform = organelle.CalculateVisualsTransform();
                }
                else
                {
                    // TODO: handle this somehow... (probably caching the position and rotation from last call in
                    // the visuals system?)
                    throw new NotImplementedException();

                    // nodeToScale.Transform = organelle.CalculateVisualsTransformExternal();
                }
            }
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

            var compounds = entity.Get<CompoundStorage>().Compounds;

            ref var baseReproduction = ref entity.Get<ReproductionStatus>();

            // Process base cost first so the player can be their designed cell (without extra organelles) for a while
            bool reproductionStageComplete =
                ProcessBaseReproductionCost(baseReproduction.MissingCompoundsForBaseReproduction, compounds,
                    ref remainingAllowedCompoundUse, ref remainingFreeCompounds,
                    consumeInReverseOrder);

            // For this stage and all others below, reproductionStageComplete tracks whether the previous reproduction
            // stage completed, i.e. whether we should proceed with the next stage
            if (reproductionStageComplete)
            {
                // Organelles that are ready to split
                var organellesToAdd = new List<PlacedOrganelle>();

                // Grow all the organelles, except the unique organelles which are given compounds last
                foreach (var organelle in organelles.Organelles)
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
                foreach (var organelle in organellesToAdd)
                {
                    // Mark this organelle as done and return to its normal size.
                    organelle.ResetGrowth();

                    // This doesn't need to update individual scales as a full organelles change is queued below for
                    // a different system to handle

                    organelle.WasSplit = true;

                    // Create a second organelle.
                    var organelle2 = SplitOrganelle(organelles.Organelles!, organelle);
                    organelle2.WasSplit = true;
                    organelle2.IsDuplicate = true;
                    organelle2.SisterOrganelle = organelle;

                    organelles.OnOrganellesChanged();
                }
            }

            if (reproductionStageComplete)
            {
                foreach (var organelle in organelles.Organelles!)
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
                        if (organelle.GrowOrganelle(compounds, ref remainingAllowedCompoundUse,
                                ref remainingFreeCompounds,
                                consumeInReverseOrder))
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

        private bool ProcessBaseReproductionCost(Dictionary<Compound, float>? requiredCompoundsForBaseReproduction,
            CompoundBag compounds, ref float remainingAllowedCompoundUse,
            ref float remainingFreeCompounds, bool consumeInReverseOrder,
            Dictionary<Compound, float>? trackCompoundUse = null)
        {
            // If no info created yet we don't know if we are done
            if (requiredCompoundsForBaseReproduction == null)
                return false;

            if (remainingAllowedCompoundUse <= 0)
            {
                return false;
            }

            bool reproductionStageComplete = true;

            foreach (var key in consumeInReverseOrder ?
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

        private PlacedOrganelle SplitOrganelle(OrganelleLayout<PlacedOrganelle> organelles, PlacedOrganelle organelle)
        {
            var q = organelle.Position.Q;
            var r = organelle.Position.R;

            // The position used here will be overridden with the right value when we manage to find a place
            // for this organelle
            var newOrganelle = new PlacedOrganelle(organelle.Definition, new Hex(q, r), 0, organelle.Upgrades);

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
                        Constants.CREATURE_REPRODUCE_POPULATION_GAIN, TranslationServer.Translate("REPRODUCED"));
                }

                ref var cellProperties = ref entity.Get<CellProperties>();

                // Return the first cell to its normal, non duplicated cell arrangement and spawn a daughter cell
                organelles.ResetOrganelleLayout(entity, species, species);

                cellProperties.Divide(ref organelles, entity, species, worldSimulation, spawnSystem, null);

                // TODO: move this to a separate system (multicellular reproduction)
                // Multicellular reproduction
                // cellProperties.Divide(entity, null);

                throw new NotImplementedException();

                // enoughResourcesForBudding = false;
                //
                // // Let's require the base reproduction cost to be fulfilled again as well, to keep down the
                // colony
                // // spam, and for consistency with non-multicellular microbes
                // SetupRequiredBaseReproductionCompounds();
            }
        }
    }
}
