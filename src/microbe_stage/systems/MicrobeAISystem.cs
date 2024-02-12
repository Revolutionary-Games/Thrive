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
    ///   Microbe AI logic
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Without the attached component here stops this from running for microbes in colonies
    ///   </para>
    /// </remarks>
    [With(typeof(MicrobeAI))]
    [With(typeof(SpeciesMember))]
    [With(typeof(MicrobeControl))]
    [With(typeof(Health))]
    [With(typeof(CompoundAbsorber))]
    [With(typeof(CompoundStorage))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(CommandSignaler))]
    [With(typeof(CellProperties))]
    [With(typeof(Engulfer))]
    [With(typeof(WorldPosition))]
    [Without(typeof(AttachedToEntity))]
    [ReadsComponent(typeof(CompoundStorage))]
    [ReadsComponent(typeof(OrganelleContainer))]
    [ReadsComponent(typeof(CellProperties))]
    [ReadsComponent(typeof(Engulfer))]
    [ReadsComponent(typeof(MicrobeColony))]
    [ReadsComponent(typeof(WorldPosition))]
    [RunsAfter(typeof(OrganelleComponentFetchSystem))]
    [RunsBefore(typeof(MicrobeMovementSystem))]
    [RunsBefore(typeof(MicrobeEmissionSystem))]
    [RunsConditionally("RunAI")]
    [RunsWithCustomCode("{0}.ReportPotentialPlayerPosition(reportedPlayerPosition);\n{0}.Update(delta);")]
    [RuntimeCost(6)]
    public sealed class MicrobeAISystem : AEntitySetSystem<float>, ISpeciesMemberLocationData
    {
        private readonly Compound atp;
        private readonly Compound glucose;
        private readonly Compound iron;
        private readonly Compound oxytoxy;
        private readonly Compound ammonia;
        private readonly Compound phosphates;

        private readonly IReadonlyCompoundClouds clouds;

        // TODO: for actual consistency these should probably be in the MicrobeAI component so that each AI entity
        // consistently uses its own random instance, instead of just a few being used per update for whatever set of
        // microbes want to update right this second
        // TODO: save these for more consistency after loading a save?
        /// <summary>
        ///   Stored random instances for use by the individual AI methods which may run in multiple threads
        /// </summary>
        private readonly List<Random> thinkRandoms = new();

        // New access to the world stuff for AI to see
        private readonly EntitySet microbesSet;
        private readonly EntitySet chunksSet;

        private readonly List<uint> speciesCachesToDrop = new();

        private readonly Dictionary<uint, List<(Entity Entity, Vector3 Position, float EngulfSize)>> microbesBySpecies =
            new();

        private readonly List<(Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)>
            chunkDataCache = new();

        private bool microbeCacheBuilt;
        private bool chunkCacheBuilt;

        private Vector3? potentiallyKnownPlayerPosition;

        private Random aiThinkRandomSource = new();

        private bool printedPlayerControlMessage;

        private int usedAIThinkRandomIndex;

        private bool skipAI;

        public MicrobeAISystem(IReadonlyCompoundClouds cloudSystem, World world, IParallelRunner runner) :
            base(world, runner, Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
        {
            clouds = cloudSystem;

            // Microbes that aren't colony non-leaders (and also not eaten)
            // The WorldPosition require is here to just ensure that the AI won't accidentally throw an exception if
            // it sees an entity with no position
            microbesSet = world.GetEntities().With<WorldPosition>().With<SpeciesMember>()
                .With<Health>().With<Engulfer>().With<Engulfable>().Without<AttachedToEntity>().AsSet();

            // Engulfables, which are basically all chunks when they aren't cells, and aren't attached so that they
            // also aren't eaten already
            chunksSet = world.GetEntities().With<Engulfable>().With<WorldPosition>().With<CompoundBag>()
                .Without<SpeciesMember>().Without<AttachedToEntity>().AsSet();

            var simulationParameters = SimulationParameters.Instance;
            atp = simulationParameters.GetCompound("atp");
            glucose = simulationParameters.GetCompound("glucose");
            iron = simulationParameters.GetCompound("iron");
            oxytoxy = simulationParameters.GetCompound("oxytoxy");
            ammonia = simulationParameters.GetCompound("ammonia");
            phosphates = simulationParameters.GetCompound("phosphates");
        }

        public void OverrideAIRandomSeed(int seed)
        {
            lock (thinkRandoms)
            {
                thinkRandoms.Clear();
                usedAIThinkRandomIndex = 0;

                aiThinkRandomSource = new Random(seed);
            }
        }

        public void ReportPotentialPlayerPosition(Vector3? playerPosition)
        {
            potentiallyKnownPlayerPosition = playerPosition;
        }

        public IReadOnlyList<(Entity Entity, Vector3 Position, float EngulfSize)>? GetSpeciesMembers(Species species)
        {
            BuildMicrobesCache();
            var id = species.ID;

            if (microbesBySpecies.TryGetValue(id, out var result))
                return result;

            return null;
        }

        public override void Dispose()
        {
            Dispose(true);
            base.Dispose();
        }

        protected override void PreUpdate(float delta)
        {
            base.PreUpdate(delta);

            skipAI = CheatManager.NoAI;
            usedAIThinkRandomIndex = 0;

            if (!skipAI)
            {
                // Clean up old cached microbes
                CleanMicrobeCache();
                CleanChunkCache();
            }
        }

        protected override void Update(float delta, in Entity entity)
        {
            if (skipAI)
                return;

            ref var ai = ref entity.Get<MicrobeAI>();

            ai.TimeUntilNextThink -= delta;

            if (ai.TimeUntilNextThink > 0)
                return;

            // TODO: would be nice to add a tiny bit of randomness to the times here so that not all cells think at once
            ai.TimeUntilNextThink = Constants.MICROBE_AI_THINK_INTERVAL;

            // This is probably pretty useless for most situations, but hopefully this doesn't eat too much
            // performance
            if (entity.Has<PlayerMarker>())
            {
                if (!printedPlayerControlMessage)
                {
                    GD.Print("AI is controlling the player microbe");
                    printedPlayerControlMessage = true;
                }
            }

            ref var health = ref entity.Get<Health>();

            if (health.Dead)
                return;

            // This shouldn't be needed thanks to the check that this doesn't run on attached entities
            // ref var engulfable = ref entity.Get<Engulfable>();
            // if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
            //     return;

            AIThink(GetNextAIRandom(), in entity, ref ai, ref health);
        }

        protected override void PostUpdate(float state)
        {
            base.PostUpdate(state);

            microbesSet.Complete();
            chunksSet.Complete();
        }

        private static bool RollCheck(float ourStat, float dc, Random random)
        {
            return random.Next(0.0f, dc) <= ourStat;
        }

        private static bool RollReverseCheck(float ourStat, float dc, Random random)
        {
            return ourStat <= random.Next(0.0f, dc);
        }

        /// <summary>
        ///   Main AI think function for cells
        /// </summary>
        private void AIThink(Random random, in Entity entity, ref MicrobeAI ai, ref Health health)
        {
            ref var absorber = ref entity.Get<CompoundAbsorber>();

            if (absorber.TotalAbsorbedCompounds == null)
                throw new InvalidOperationException("AI microbe doesn't have compound absorb tracking on");

            ai.PreviouslyAbsorbedCompounds ??= new Dictionary<Compound, float>(absorber.TotalAbsorbedCompounds);

            ChooseActions(in entity, ref ai, ref absorber, ref health, random);

            // Store the absorbed compounds for run and rumble
            ai.PreviouslyAbsorbedCompounds!.Clear();

            foreach (var compound in absorber.TotalAbsorbedCompounds!)
            {
                ai.PreviouslyAbsorbedCompounds[compound.Key] = compound.Value;
            }

            // We clear here for update, this is why we stored above!
            absorber.TotalAbsorbedCompounds.Clear();
        }

        private void ChooseActions(in Entity entity, ref MicrobeAI ai, ref CompoundAbsorber absorber,
            ref Health health, Random random)
        {
            // Fetch all the components that are usually needed
            ref var position = ref entity.Get<WorldPosition>();

            ref var ourSpecies = ref entity.Get<SpeciesMember>();

            ref var organelles = ref entity.Get<OrganelleContainer>();

            ref var cellProperties = ref entity.Get<CellProperties>();

            ref var signaling = ref entity.Get<CommandSignaler>();

            ref var engulfer = ref entity.Get<Engulfer>();

            ref var control = ref entity.Get<MicrobeControl>();

            var compounds = entity.Get<CompoundStorage>().Compounds;

            // Adjusted behaviour values (calculated here as these are needed by various methods)
            float speciesAggression = ourSpecies.Species.Behaviour.Aggression *
                (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.5f : 1.0f);

            float speciesFear = ourSpecies.Species.Behaviour.Fear *
                (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 0.75f : 1.0f);

            float speciesActivity = ourSpecies.Species.Behaviour.Activity *
                (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.25f : 1.0f);

            float speciesFocus = ourSpecies.Species.Behaviour.Focus;
            float speciesOpportunism = ourSpecies.Species.Behaviour.Opportunism;

            // If nothing is engulfing me right now, see if there's something that might want to hunt me
            Vector3? predator =
                GetNearestPredatorItem(ref health, ref ourSpecies, ref engulfer, ref position, speciesFear)?.Position;
            if (predator.HasValue && position.Position.DistanceSquaredTo(predator.Value) <
                (1500.0 * speciesFear / Constants.MAX_SPECIES_FEAR))
            {
                FleeFromPredators(ref position, ref ai, ref control, ref organelles, compounds, entity, predator.Value,
                    speciesFocus, speciesActivity, speciesAggression, speciesFear, random);
                return;
            }

            // If this microbe is out of ATP, pick an amount of time to rest
            if (compounds.GetCompoundAmount(atp) < 1.0f)
            {
                // Keep the maximum at 95% full, as there is flickering when near full
                ai.ATPThreshold = 0.95f * speciesFocus / Constants.MAX_SPECIES_FOCUS;
            }

            if (ai.ATPThreshold > 0.0f)
            {
                if (compounds.GetCompoundAmount(atp) < compounds.GetCapacityForCompound(atp) * ai.ATPThreshold
                    && compounds.Any(compound => IsVitalCompound(compound.Key, compounds) && compound.Value > 0.0f))
                {
                    control.SetMoveSpeed(0.0f);
                    return;
                }

                ai.ATPThreshold = 0.0f;
            }

            // Follow received commands if we have them
            if (organelles.HasSignalingAgent && signaling.ReceivedCommand != MicrobeSignalCommand.None)
            {
                // TODO: tweak the balance between following commands and doing normal behaviours
                // TODO: and also probably we want to add some randomness to the positions and speeds based on distance
                switch (signaling.ReceivedCommand)
                {
                    case MicrobeSignalCommand.MoveToMe:
                    {
                        // TODO: should these use signaling.ReceivedCommandSource ? As that's where the chemical signal
                        // was smelled from
                        if (signaling.ReceivedCommandFromEntity.Has<WorldPosition>())
                        {
                            ai.MoveToLocation(signaling.ReceivedCommandFromEntity.Get<WorldPosition>().Position,
                                ref control, entity);
                            return;
                        }

                        break;
                    }

                    case MicrobeSignalCommand.FollowMe:
                    {
                        if (signaling.ReceivedCommandFromEntity.Has<WorldPosition>())
                        {
                            var signalerPosition = signaling.ReceivedCommandFromEntity.Get<WorldPosition>().Position;
                            if (position.Position.DistanceSquaredTo(signalerPosition) >
                                Constants.AI_FOLLOW_DISTANCE_SQUARED)
                            {
                                ai.MoveToLocation(signalerPosition, ref control, entity);
                                return;
                            }

                            return;
                        }

                        break;
                    }

                    case MicrobeSignalCommand.FleeFromMe:
                    {
                        if (signaling.ReceivedCommandFromEntity.Has<WorldPosition>())
                        {
                            var signalerPosition = signaling.ReceivedCommandFromEntity.Get<WorldPosition>().Position;
                            if (position.Position.DistanceSquaredTo(signalerPosition) <
                                Constants.AI_FLEE_DISTANCE_SQUARED)
                            {
                                control.SetStateColonyAware(entity, MicrobeState.Normal);
                                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);

                                // Direction is calculated to be the opposite from where we should flee
                                ai.TargetPosition = position.Position + (position.Position - signalerPosition);
                                control.LookAtPoint = ai.TargetPosition;
                                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                                return;
                            }
                        }

                        break;
                    }
                }
            }

            bool isSessile = speciesActivity < Constants.MAX_SPECIES_ACTIVITY / 10;

            // If I'm very far from the player, and I have not been near the player yet, get on stage
            if (!ai.HasBeenNearPlayer)
            {
                if (potentiallyKnownPlayerPosition != null)
                {
                    // Only move if we aren't sessile
                    if (position.Position.DistanceSquaredTo(potentiallyKnownPlayerPosition.Value) >
                        Math.Pow(Constants.SPAWN_SECTOR_SIZE, 2) * 0.75f &&
                        !isSessile)
                    {
                        ai.MoveToLocation(potentiallyKnownPlayerPosition.Value, ref control, entity);
                        return;
                    }

                    ai.HasBeenNearPlayer = true;
                }
            }

            // If there are no threats, look for a chunk to eat
            // TODO: still consider engulfing things if we're in a colony that can engulf (has engulfer cells)
            if (cellProperties.MembraneType.CanEngulf)
            {
                var targetChunk = GetNearestChunkItem(in entity, ref engulfer, ref position, compounds,
                    speciesFocus, speciesOpportunism, random);
                if (targetChunk != null)
                {
                    PursueAndConsumeChunks(ref position, ref ai, ref control, ref engulfer, entity,
                        targetChunk.Value.Position, speciesActivity, random);
                    return;
                }
            }

            // If there are no chunks, look for living prey to hunt
            var possiblePrey = GetNearestPreyItem(ref ai, ref position, ref organelles, ref ourSpecies, ref engulfer,
                compounds, speciesFocus, speciesAggression, speciesOpportunism, random);
            if (possiblePrey != default && possiblePrey.IsAlive)
            {
                Vector3 prey;

                try
                {
                    prey = possiblePrey.Get<WorldPosition>().Position;
                }
                catch (Exception e)
                {
                    GD.PrintErr("Microbe AI tried to engage prey with no position: " + e);
                    ai.FocusedPrey = default;
                    return;
                }

                bool engulfPrey = cellProperties.CanEngulfObject(ref ourSpecies, ref engulfer, possiblePrey) ==
                    EngulfCheckResult.Ok && position.Position.DistanceSquaredTo(prey) <
                    10.0f * engulfer.EngulfingSize;

                EngagePrey(ref ai, ref control, ref organelles, ref position, compounds, entity, prey, engulfPrey,
                    speciesAggression, speciesFocus, speciesActivity, random);
                return;
            }

            // There is no reason to be engulfing at this stage
            control.SetStateColonyAware(entity, MicrobeState.Normal);

            // Otherwise just wander around and look for compounds
            if (!isSessile)
            {
                SeekCompounds(in entity, ref ai, ref position, ref control, ref organelles, ref absorber, compounds,
                    speciesActivity, speciesFocus, random);
            }
            else
            {
                // This organism is sessile, and will not act until the environment changes
                control.SetMoveSpeed(0.0f);
            }
        }

        private (Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)? GetNearestChunkItem(
            in Entity entity, ref Engulfer engulfer, ref WorldPosition position,
            CompoundBag ourCompounds, float speciesFocus, float speciesOpportunism, Random random)
        {
            (Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)? chosenChunk = null;
            float bestFoundChunkDistance = float.MaxValue;

            BuildChunksCache();

            // Retrieve nearest potential chunk
            foreach (var chunk in chunkDataCache)
            {
                // Skip too big things
                if (engulfer.EngulfingSize < chunk.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ)
                    continue;

                // And too distant things
                var distance = (chunk.Position - position.Position).LengthSquared();

                if (distance > bestFoundChunkDistance)
                    continue;

                if (distance > (20000.0 * speciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0)
                    continue;

                if (chunk.Compounds.Compounds.Any(p => ourCompounds.IsUseful(p.Key) && p.Key.Digestible))
                {
                    if (chosenChunk == null)
                    {
                        chosenChunk = chunk;
                        bestFoundChunkDistance = distance;
                    }
                }
            }

            // Don't bother with chunks when there's a lot of microbes to compete with
            if (chosenChunk != null)
            {
                BuildMicrobesCache();

                var rivals = 0;
                foreach (var entry in microbesBySpecies)
                {
                    // Take own species members also into account when considering rivals

                    foreach (var rival in entry.Value)
                    {
                        // Don't compete against yourself
                        if (rival.Entity == entity)
                            continue;

                        var rivalDistance = (rival.Position - chosenChunk.Value.Position).LengthSquared();
                        if (rivalDistance < 500.0f && rivalDistance < bestFoundChunkDistance)
                        {
                            rivals++;
                        }
                    }
                }

                int rivalThreshold = 5;

                // Less opportunistic species will avoid chunks even when there are just a few rivals
                if (speciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM / 3)
                {
                    rivalThreshold = 1;
                }
                else if (speciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3)
                {
                    rivalThreshold = 3;
                }

                // In rare instances, microbes will choose to be much more ambitious
                if (RollCheck(speciesFocus, Constants.MAX_SPECIES_FOCUS, random))
                {
                    rivalThreshold *= 2;
                }

                if (rivals > rivalThreshold)
                {
                    chosenChunk = null;
                }
            }

            return chosenChunk;
        }

        /// <summary>
        ///   Gets the nearest prey item. And builds the prey list
        /// </summary>
        /// <returns>The nearest prey item.</returns>
        private Entity GetNearestPreyItem(ref MicrobeAI ai, ref WorldPosition position,
            ref OrganelleContainer organelles, ref SpeciesMember ourSpecies,
            ref Engulfer engulfer, CompoundBag ourCompounds, float speciesFocus, float speciesAggression,
            float speciesOpportunism, Random random)
        {
            if (ai.FocusedPrey != default && ai.FocusedPrey.IsAlive)
            {
                var focused = ai.FocusedPrey;
                try
                {
                    var distanceToFocusedPrey =
                        position.Position.DistanceSquaredTo(focused.Get<WorldPosition>().Position);
                    if (!focused.Get<Health>().Dead &&
                        focused.Get<Engulfable>().PhagocytosisStep == PhagocytosisPhase.None && distanceToFocusedPrey <
                        (3500.0f * speciesFocus / Constants.MAX_SPECIES_FOCUS))
                    {
                        if (distanceToFocusedPrey < ai.PursuitThreshold)
                        {
                            // Keep chasing, but expect to keep getting closer
                            ai.LowerPursuitThreshold();
                            return focused;
                        }

                        // If prey hasn't gotten closer by now, it's probably too fast, or juking you
                        // Remember who focused prey is, so that you don't fall for this again
                        return default;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr("Invalid focused prey, resetting, error: " + e);
                }

                ai.FocusedPrey = default;
            }

            (Entity Entity, Vector3 Position, float EngulfSize)? chosenPrey = null;
            float minDistance = float.MaxValue;

            BuildMicrobesCache();

            foreach (var entry in microbesBySpecies)
            {
                // Don't try to eat members of the same species
                if (entry.Key == ourSpecies.ID)
                    continue;

                foreach (var otherMicrobeInfo in entry.Value)
                {
                    var distance = position.Position.DistanceSquaredTo(otherMicrobeInfo.Position);

                    // Early skip farther away entities than already found, or too far away entities to consider eating
                    if (distance > minDistance ||
                        distance > 2500.0f * speciesAggression / Constants.MAX_SPECIES_AGGRESSION)
                    {
                        continue;
                    }

                    if (CanTryToEatMicrobe(ref ourSpecies, ref engulfer, ref organelles, ourCompounds,
                            ref otherMicrobeInfo.Entity.Get<Engulfable>(),
                            ref otherMicrobeInfo.Entity.Get<SpeciesMember>(), speciesOpportunism, speciesFocus, random))
                    {
                        if (chosenPrey == null)
                        {
                            chosenPrey = otherMicrobeInfo;
                            minDistance = distance;
                        }
                    }
                }
            }

            if (chosenPrey != null)
            {
                ai.FocusedPrey = chosenPrey.Value.Entity;
                ai.PursuitThreshold = position.Position.DistanceSquaredTo(chosenPrey.Value.Position) * 3.0f;
            }
            else
            {
                ai.FocusedPrey = default;
            }

            return ai.FocusedPrey;
        }

        /// <summary>
        ///   Building the predator list and setting the scariest one to be predator
        /// </summary>
        private (Entity Entity, Vector3 Position, float EngulfSize)? GetNearestPredatorItem(ref Health health,
            ref SpeciesMember ourSpecies, ref Engulfer engulfer, ref WorldPosition position, float speciesFear)
        {
            var fleeThreshold = 3.0f - (2 *
                (speciesFear / Constants.MAX_SPECIES_FEAR) *
                (10 - (9 * health.CurrentHealth / health.MaxHealth)));

            (Entity Entity, Vector3 Position, float EngulfSize)? predator = null;
            float minDistance = float.MaxValue;

            BuildMicrobesCache();

            foreach (var entry in microbesBySpecies)
            {
                // Don't be scared of the same species
                if (entry.Key == ourSpecies.ID)
                    continue;

                foreach (var otherMicrobeInfo in entry.Value)
                {
                    // Based on species fear, threshold to be afraid ranges from 0.8 to 1.8 microbe size.
                    if (otherMicrobeInfo.EngulfSize > engulfer.EngulfingSize * fleeThreshold)
                    {
                        var distance = position.Position.DistanceSquaredTo(otherMicrobeInfo.Position);
                        if (predator == null || minDistance > distance)
                        {
                            predator = otherMicrobeInfo;
                            minDistance = distance;
                        }
                    }
                }
            }

            return predator;
        }

        private void PursueAndConsumeChunks(ref WorldPosition position, ref MicrobeAI ai, ref MicrobeControl control,
            ref Engulfer engulfer, in Entity entity, Vector3 chunk, float speciesActivity, Random random)
        {
            // This is a slight offset of where the chunk is, to avoid a forward-facing part blocking it
            ai.TargetPosition = chunk + new Vector3(0.5f, 0.0f, 0.5f);
            control.LookAtPoint = ai.TargetPosition;
            SetEngulfIfClose(ref control, ref engulfer, ref position, entity, chunk);

            // Just in case something is obstructing chunk engulfing, wiggle a little sometimes
            if (random.NextDouble() < 0.05)
            {
                ai.MoveWithRandomTurn(0.1f, 0.2f, position.Position, ref control, speciesActivity, random);
            }

            // If this Microbe is right on top of the chunk, stop instead of spinning
            if (position.Position.DistanceSquaredTo(chunk) < Constants.AI_ENGULF_STOP_DISTANCE)
            {
                control.SetMoveSpeed(0.0f);
            }
            else
            {
                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
            }
        }

        private void FleeFromPredators(ref WorldPosition position, ref MicrobeAI ai, ref MicrobeControl control,
            ref OrganelleContainer organelles, CompoundBag ourCompounds, in Entity entity, Vector3 predatorLocation,
            float speciesFocus, float speciesActivity, float speciesAggression, float speciesFear, Random random)
        {
            control.SetStateColonyAware(entity, MicrobeState.Normal);

            ai.TargetPosition = (2 * (position.Position - predatorLocation)) + position.Position;

            control.LookAtPoint = ai.TargetPosition;

            if (position.Position.DistanceSquaredTo(predatorLocation) < 100.0f)
            {
                if ((organelles.SlimeJets?.Count ?? 0) > 0 &&
                    RollCheck(speciesFear, Constants.MAX_SPECIES_FEAR, random))
                {
                    // There's a chance to jet away if we can
                    control.SecreteSlimeForSomeTime(ref organelles, random);
                }
                else if (RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
                {
                    // If the predator is right on top of us there's a chance to try and swing with a pilus
                    ai.MoveWithRandomTurn(2.5f, 3.0f, position.Position, ref control, speciesActivity, random);
                }
            }

            // If prey is confident enough, it will try and launch toxin at the predator
            if (speciesAggression > speciesFear &&
                position.Position.DistanceSquaredTo(predatorLocation) >
                300.0f - (5.0f * speciesAggression) + (6.0f * speciesFear) &&
                RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
            {
                LaunchToxin(ref control, ref organelles, ref position, predatorLocation, ourCompounds, speciesFocus,
                    speciesActivity);
            }

            // No matter what, I want to make sure I'm moving
            control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
        }

        private void EngagePrey(ref MicrobeAI ai, ref MicrobeControl control, ref OrganelleContainer organelles,
            ref WorldPosition position, CompoundBag ourCompounds, in Entity entity, Vector3 target, bool engulf,
            float speciesAggression,
            float speciesFocus, float speciesActivity, Random random)
        {
            control.SetStateColonyAware(entity, engulf ? MicrobeState.Engulf : MicrobeState.Normal);
            ai.TargetPosition = target;
            control.LookAtPoint = ai.TargetPosition;
            if (CanShootToxin(ourCompounds, speciesFocus))
            {
                LaunchToxin(ref control, ref organelles, ref position, target, ourCompounds, speciesFocus,
                    speciesActivity);

                if (RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION / 5, random))
                {
                    control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                }
            }
            else
            {
                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
            }

            // Predators can use slime jets as an ambush mechanism
            if (RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
            {
                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                control.SecreteSlimeForSomeTime(ref organelles, random);
            }
        }

        private void SeekCompounds(in Entity entity, ref MicrobeAI ai, ref WorldPosition position,
            ref MicrobeControl control, ref OrganelleContainer organelles, ref CompoundAbsorber absorber,
            CompoundBag compounds, float speciesActivity, float speciesFocus, Random random)
        {
            // More active species just try to get distance to avoid over-clustering
            if (RollCheck(speciesActivity, Constants.MAX_SPECIES_ACTIVITY + (Constants.MAX_SPECIES_ACTIVITY / 2),
                    random))
            {
                control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
                return;
            }

            if (random.Next(Constants.AI_STEPS_PER_SMELL) == 0)
            {
                SmellForCompounds(in entity, ref ai, ref position, ref organelles, compounds, speciesFocus);
            }

            // If the AI has smelled a compound (currently only possible with a chemoreceptor), go towards it.
            if (ai.LastSmelledCompoundPosition != null)
            {
                var distance = position.Position.DistanceSquaredTo(ai.LastSmelledCompoundPosition.Value);

                // If the compound isn't getting closer, either something else has taken it, or we're stuck
                ai.LowerPursuitThreshold();
                if (distance > ai.PursuitThreshold)
                {
                    ai.LastSmelledCompoundPosition = null;
                    RunAndTumble(ref ai, ref control, ref position, ref absorber, compounds, speciesActivity, random);
                    return;
                }

                if (distance > 3.0f)
                {
                    ai.TargetPosition = ai.LastSmelledCompoundPosition!.Value;
                    control.LookAtPoint = ai.TargetPosition;
                }
                else
                {
                    control.SetMoveSpeed(0.0f);
                    SmellForCompounds(in entity, ref ai, ref position, ref organelles, compounds, speciesFocus);
                }
            }
            else
            {
                RunAndTumble(ref ai, ref control, ref position, ref absorber, compounds, speciesActivity, random);
            }
        }

        private void SmellForCompounds(in Entity entity, ref MicrobeAI ai, ref WorldPosition position,
            ref OrganelleContainer organelles, CompoundBag compounds, float speciesFocus)
        {
            ComputeCompoundsSearchWeights(ref ai, compounds);

            var weights = ai.CompoundsSearchWeights!;

            var detections = organelles.PerformCompoundDetection(in entity, position.Position, clouds);

            if (detections is { Count: > 0 })
            {
                ai.LastSmelledCompoundPosition = detections.OrderBy(detection =>
                    weights.TryGetValue(detection.Compound, out var weight) ?
                        weight :
                        0).First().Target;
                ai.PursuitThreshold = position.Position.DistanceSquaredTo(ai.LastSmelledCompoundPosition.Value)
                    * (1 + (speciesFocus / Constants.MAX_SPECIES_FOCUS));
            }
            else
            {
                ai.LastSmelledCompoundPosition = null;
            }
        }

        /// <summary>
        ///   For doing run and tumble
        /// </summary>
        private void RunAndTumble(ref MicrobeAI ai, ref MicrobeControl control, ref WorldPosition position,
            ref CompoundAbsorber absorber, CompoundBag compounds, float speciesActivity, Random random)
        {
            // If this microbe is currently stationary, just initialize by moving in a random direction.
            // Used to get newly spawned microbes to move.
            if (control.MovementDirection.Length() == 0)
            {
                ai.MoveWithRandomTurn(0, Mathf.Pi, position.Position, ref control, speciesActivity, random);
                return;
            }

            // Run and tumble
            // A biased random walk, they turn more if they are picking up less compounds.
            // The scientifically accurate algorithm has been flipped to account for the compound
            // deposits being a lot smaller compared to the microbes
            // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

            ComputeCompoundsSearchWeights(ref ai, compounds);

            float gradientValue = 0.0f;
            foreach (var compoundWeight in ai.CompoundsSearchWeights!)
            {
                // Note this is about absorbed quantities (which is all microbe has access to) not the ones in the
                // clouds. Gradient computation is therefore cell-centered, and might be different for different cells.
                float compoundDifference = 0.0f;

                absorber.TotalAbsorbedCompounds!.TryGetValue(compoundWeight.Key, out float quantityAbsorbedThisStep);
                ai.PreviouslyAbsorbedCompounds!.TryGetValue(compoundWeight.Key, out float quantityAbsorbedPreviousStep);

                compoundDifference += quantityAbsorbedThisStep - quantityAbsorbedPreviousStep;

                compoundDifference *= compoundWeight.Value;
                gradientValue += compoundDifference;
            }

            // Implement a detection threshold to possibly rule out too tiny variations
            // TODO: possibly include cell capacity correction
            float differenceDetectionThreshold = Constants.AI_GRADIENT_DETECTION_THRESHOLD;

            // If food density is going down, back up and see if there's some more
            if (gradientValue < -differenceDetectionThreshold && random.Next(0, 10) < 9)
            {
                ai.MoveWithRandomTurn(2.5f, 3.0f, position.Position, ref control, speciesActivity, random);
            }

            // If there isn't any food here, it's a good idea to keep moving
            if (Math.Abs(gradientValue) <= differenceDetectionThreshold && random.Next(0, 10) < 5)
            {
                ai.MoveWithRandomTurn(0.0f, 0.4f, position.Position, ref control, speciesActivity, random);
            }

            // If positive last step you gained compounds, so let's move toward the source
            if (gradientValue > differenceDetectionThreshold)
            {
                // There's a decent chance to turn by 90° to explore gradient
                // 180° is useless since previous position let you absorb less compounds already
                if (random.Next(0, 10) < 4)
                {
                    ai.MoveWithRandomTurn(0.0f, 1.5f, position.Position, ref control, speciesActivity, random);
                }
            }
        }

        /// <summary>
        ///   Prioritizing compounds that are stored in lesser quantities.
        ///   If ATP-producing compounds are low (less than half storage capacities),
        ///   non ATP-related compounds are discarded.
        ///   Updates compoundsSearchWeights instance dictionary.
        /// </summary>
        private void ComputeCompoundsSearchWeights(ref MicrobeAI ai, CompoundBag storedCompounds)
        {
            if (ai.CompoundsSearchWeights == null)
            {
                ai.CompoundsSearchWeights = new Dictionary<Compound, float>();
            }
            else
            {
                ai.CompoundsSearchWeights.Clear();
            }

            // TODO: should this really assume that all stored compounds are immediately useful
            var usefulCompounds = storedCompounds.Compounds.Keys;

            // If this microbe lacks vital compounds don't bother with ammonia and phosphate
            bool lackingVital = false;
            foreach (var compound in usefulCompounds)
            {
                if (IsVitalCompound(compound, storedCompounds) && storedCompounds.GetCompoundAmount(compound) <
                    0.5f * storedCompounds.GetCapacityForCompound(compound))
                {
                    // Ammonia and phosphates are not considered useful
                    lackingVital = true;
                    break;
                }
            }

            if (!lackingVital)
            {
                foreach (var compound in usefulCompounds)
                {
                    // The priority of a compound is inversely proportional to its availability
                    // Should be tweaked with consumption
                    var compoundPriority = 1 - storedCompounds.GetCompoundAmount(compound) /
                        storedCompounds.GetCapacityForCompound(compound);

                    ai.CompoundsSearchWeights.Add(compound, compoundPriority);
                }
            }
            else
            {
                foreach (var compound in usefulCompounds)
                {
                    if (compound == ammonia || compound == phosphates)
                        continue;

                    var compoundPriority = 1 - storedCompounds.GetCompoundAmount(compound) /
                        storedCompounds.GetCapacityForCompound(compound);

                    ai.CompoundsSearchWeights.Add(compound, compoundPriority);
                }
            }
        }

        /// <summary>
        ///   Tells if a compound is vital to this microbe.
        ///   Vital compounds are *direct* ATP producers
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     TODO: what is used here is a shortcut linked to the current game state: such compounds could be used for
        ///     other processes in future versions
        ///   </para>
        /// </remarks>
        private bool IsVitalCompound(Compound compound, CompoundBag compounds)
        {
            // TODO: looking for mucilage should be prevented
            return compounds.IsUseful(compound) &&
                (compound == glucose || compound == iron);
        }

        private void SetEngulfIfClose(ref MicrobeControl control, ref Engulfer engulfer, ref WorldPosition position,
            in Entity entity, Vector3 targetPosition)
        {
            // Turn on engulf mode if close
            // Sometimes "close" is hard to discern since microbes can range from straight lines to circles
            if ((position.Position - targetPosition).LengthSquared() <= engulfer.EngulfingSize * 2.0f)
            {
                control.SetStateColonyAware(entity, MicrobeState.Engulf);
            }
            else
            {
                control.SetStateColonyAware(entity, MicrobeState.Normal);
            }
        }

        private void LaunchToxin(ref MicrobeControl control, ref OrganelleContainer organelles,
            ref WorldPosition position, Vector3 target, CompoundBag compounds, float speciesFocus,
            float speciesActivity)
        {
            // TODO: AI should be able to use all toxin vacuoles in cell colonies

            if (organelles.AgentVacuoleCount > 0 &&
                (position.Position - target).LengthSquared() <= speciesFocus * 10.0f)
            {
                if (CanShootToxin(compounds, speciesFocus))
                {
                    control.LookAtPoint = target;

                    // Hold fire until the target is lined up.
                    // TODO: verify that this calculation is now correct, this used to use just the angle to the world
                    // look at point which certainly wasn't correct when the microbe was not positioned at world origin
                    var currentLookDirection = position.Rotation.Xform(Vector3.Forward);

                    if (currentLookDirection.Normalized()
                            .AngleTo((control.LookAtPoint - position.Position).Normalized()) <
                        0.1f + speciesActivity / (Constants.AI_BASE_TOXIN_SHOOT_ANGLE_PRECISION * speciesFocus))
                    {
                        control.QueuedToxinToEmit = oxytoxy;
                    }
                }
            }
        }

        private bool CanTryToEatMicrobe(ref SpeciesMember ourSpecies, ref Engulfer engulfer,
            ref OrganelleContainer organelles, CompoundBag ourCompounds,
            ref Engulfable targetMicrobe, ref SpeciesMember targetSpecies, float speciesOpportunism, float speciesFocus,
            Random random)
        {
            var sizeRatio = engulfer.EngulfingSize / targetMicrobe.EffectiveEngulfSize();

            // Sometimes the AI will randomly decide to try in vain to eat something
            var choosingToEngulf = organelles.CanDigestObject(ref targetMicrobe) == DigestCheckResult.Ok ||
                random.NextDouble() <
                Constants.AI_BAD_ENGULF_CHANCE * speciesOpportunism / Constants.MAX_SPECIES_OPPORTUNISM;

            var choosingToAttackWithToxin = speciesOpportunism
                > Constants.MAX_SPECIES_OPPORTUNISM * 0.3f && CanShootToxin(ourCompounds, speciesFocus);

            return choosingToEngulf &&
                targetSpecies.ID != ourSpecies.ID && (
                    choosingToAttackWithToxin
                    || (sizeRatio >= Constants.ENGULF_SIZE_RATIO_REQ));
        }

        private bool CanShootToxin(CompoundBag compounds, float speciesFocus)
        {
            return compounds.GetCompoundAmount(oxytoxy) >=
                Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * speciesFocus / Constants.MAX_SPECIES_FOCUS;
        }

        private void CleanMicrobeCache()
        {
            foreach (var entry in microbesBySpecies)
            {
                if (entry.Value.Count < 1)
                {
                    // TODO: would it be possible to keep some old species here for some time as this seems to drop
                    // stuff pretty often and then cause new memory allocations
                    speciesCachesToDrop.Add(entry.Key);
                }
                else
                {
                    // Empty out the cache lists as BuildMicrobesCache rebuilds them always from scratch
                    entry.Value.Clear();
                }
            }

            // Remove unused species lists from the species cache
            foreach (var toClear in speciesCachesToDrop)
            {
                microbesBySpecies.Remove(toClear);
            }

            speciesCachesToDrop.Clear();

            microbeCacheBuilt = false;
        }

        /// <summary>
        ///   Builds a full cache of all alive, non-engulfed and non-colony member cells (colony lead cells are
        ///   included)
        /// </summary>
        private void BuildMicrobesCache()
        {
            // To allow multithreaded AI access safely
            lock (microbesBySpecies)
            {
                if (microbeCacheBuilt)
                    return;

                foreach (ref readonly var microbe in microbesSet.GetEntities())
                {
                    // Skip considering dead microbes
                    ref var health = ref microbe.Get<Health>();

                    if (health.Dead)
                        continue;

                    ref var microbeSpecies = ref microbe.Get<SpeciesMember>();

                    // TODO: determine if it is a good idea to resolve this data here immediately (at least position
                    // should be fine as it is needed by other systems as well, see ISpeciesMemberLocationData)
                    ref var position = ref microbe.Get<WorldPosition>();
                    ref var engulfer = ref microbe.Get<Engulfer>();

                    // We assume here that the engulfable size is the same as the engulfing size so we don't fetch
                    // Engulfable here

                    if (!microbesBySpecies.TryGetValue(microbeSpecies.ID, out var targetList))
                    {
                        targetList = new List<(Entity Entity, Vector3 Position, float EngulfSize)>();

                        microbesBySpecies[microbeSpecies.ID] = targetList;
                    }

                    targetList.Add((microbe, position.Position, engulfer.EngulfingSize));
                }

                microbeCacheBuilt = true;
            }
        }

        private void CleanChunkCache()
        {
            chunkDataCache.Clear();
            chunkCacheBuilt = false;
        }

        /// <summary>
        ///   Builds a full cache of all non-engulfed chunks that aren't dissolving currently
        /// </summary>
        private void BuildChunksCache()
        {
            // To allow multithreaded AI access safely
            lock (chunkDataCache)
            {
                if (chunkCacheBuilt)
                    return;

                foreach (ref readonly var chunk in chunksSet.GetEntities())
                {
                    // Ignore already despawning chunks
                    ref var timed = ref chunk.Get<TimedLife>();

                    if (timed.TimeToLiveRemaining <= 0)
                        continue;

                    // Ignore chunks that wouldn't yield any useful compounds when absorbing
                    ref var compounds = ref chunk.Get<CompoundStorage>();

                    if (!compounds.Compounds.HasAnyCompounds())
                        continue;

                    // TODO: determine if it is a good idea to resolve this data here immediately
                    ref var position = ref chunk.Get<WorldPosition>();
                    ref var engulfable = ref chunk.Get<Engulfable>();

                    chunkDataCache.Add((chunk, position.Position, engulfable.AdjustedEngulfSize, compounds.Compounds));
                }

                chunkCacheBuilt = true;
            }
        }

        private Random GetNextAIRandom()
        {
            lock (thinkRandoms)
            {
                while (usedAIThinkRandomIndex >= thinkRandoms.Count)
                {
                    thinkRandoms.Add(new Random(aiThinkRandomSource.Next()));
                }

                return thinkRandoms[usedAIThinkRandomIndex++];
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                microbesSet.Dispose();
                chunksSet.Dispose();
            }
        }
    }
}
