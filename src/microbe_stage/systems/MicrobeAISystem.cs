﻿namespace Systems;

using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using Xoshiro.PRNG64;
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
[ReadsComponent(typeof(StrainAffected))]
[ReadsComponent(typeof(Engulfable))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(OrganelleComponentFetchSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsBefore(typeof(MicrobeEmissionSystem))]
[RunsConditionally("RunAI")]
[RunsWithCustomCode("{0}.ReportPotentialPlayerPosition(reportedPlayerPosition);\n{0}.Update(delta);")]
[RuntimeCost(9)]
public sealed class MicrobeAISystem : AEntitySetSystem<float>, ISpeciesMemberLocationData
{
    private readonly IReadonlyCompoundClouds clouds;
    private readonly IDaylightInfo lightInfo;

    // TODO: for actual consistency these should probably be in the MicrobeAI component so that each AI entity
    // consistently uses its own random instance, instead of just a few being used per update for whatever set of
    // microbes want to update right this second
    // TODO: save these for more consistency after loading a save?
    /// <summary>
    ///   Stored random instances for use by the individual AI methods which may run in multiple threads
    /// </summary>
    private readonly List<XoShiRo256starstar> thinkRandoms = new();

    // New access to the world stuff for AI to see
    private readonly EntitySet microbesSet;
    private readonly EntitySet chunksSet;

    private readonly List<uint> speciesCachesToDrop = new();

    private readonly Dictionary<uint, List<(Entity Entity, Vector3 Position, float EngulfSize)>> microbesBySpecies =
        new();

    private readonly List<(Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)>
        chunkDataCache = new();

    private readonly List<(Entity Entity, Vector3 Position, CompoundBag Compounds)>
        terrainChunkDataCache = new();

    private readonly Dictionary<Species, bool> speciesUsingVaryingCompounds = new();
    private readonly HashSet<BioProcess> varyingCompoundsTemporary = new();

    private GameWorld? gameWorld;
    private bool currentlyNight;

    private bool microbeCacheBuilt;
    private bool chunkCacheBuilt;

    private Vector3? potentiallyKnownPlayerPosition;

    private XoShiRo256starstar aiThinkRandomSource = new();

    private bool printedPlayerControlMessage;

    private int usedAIThinkRandomIndex;

    private bool skipAI;

    public MicrobeAISystem(IReadonlyCompoundClouds cloudSystem, IDaylightInfo lightInfo, World world,
        IParallelRunner runner) :
        base(world, runner, Constants.SYSTEM_NORMAL_ENTITIES_PER_THREAD)
    {
        clouds = cloudSystem;
        this.lightInfo = lightInfo;

        // Microbes that aren't colony non-leaders (and also not eaten)
        // The WorldPosition require is here to just ensure that the AI won't accidentally throw an exception if
        // it sees an entity with no position
        microbesSet = world.GetEntities().With<WorldPosition>().With<SpeciesMember>()
            .With<Health>().With<Engulfer>().With<Engulfable>().Without<AttachedToEntity>().AsSet();

        // Chunks that aren't cells or attached so that they also aren't eaten already
        chunksSet = world.GetEntities().With<WorldPosition>().With<CompoundStorage>()
            .Without<SpeciesMember>().Without<AttachedToEntity>().AsSet();
    }

    public void OverrideAIRandomSeed(long seed)
    {
        lock (thinkRandoms)
        {
            thinkRandoms.Clear();
            usedAIThinkRandomIndex = 0;

            aiThinkRandomSource = new XoShiRo256starstar(seed);
        }
    }

    public void ReportPotentialPlayerPosition(Vector3? playerPosition)
    {
        potentiallyKnownPlayerPosition = playerPosition;
    }

    public void SetWorld(GameWorld gameWorld)
    {
        this.gameWorld = gameWorld;
    }

    public IReadOnlyList<(Entity Entity, Vector3 Position, float EngulfSize)>? GetSpeciesMembers(Species species)
    {
        BuildMicrobesCache();
        var id = species.ID;

        return microbesBySpecies.GetValueOrDefault(id);
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
            CleanSpeciesUsingVaryingCompound();
        }

        if (gameWorld == null)
            throw new InvalidOperationException("Current world not set for AI");

        if (gameWorld.WorldSettings.DayNightCycleEnabled && gameWorld.Map.CurrentPatch?.HasDayAndNight == true)
        {
            currentlyNight = lightInfo.IsNightCurrently;
        }
        else
        {
            currentlyNight = false;
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

        var strain = 0.0f;

        if (entity.Has<StrainAffected>())
        {
            strain = entity.Get<StrainAffected>().CurrentStrain;
        }

        // This shouldn't be needed thanks to the check that this doesn't run on attached entities
        // ref var engulfable = ref entity.Get<Engulfable>();
        // if (engulfable.PhagocytosisStep != PhagocytosisPhase.None)
        //     return;

        AIThink(GetNextAIRandom(), in entity, ref ai, ref health, strain);
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
    private void AIThink(Random random, in Entity entity, ref MicrobeAI ai, ref Health health, float strain)
    {
        ref var absorber = ref entity.Get<CompoundAbsorber>();

        if (absorber.TotalAbsorbedCompounds == null)
            throw new InvalidOperationException("AI microbe doesn't have compound absorb tracking on");

        ai.PreviouslyAbsorbedCompounds ??= new Dictionary<Compound, float>(absorber.TotalAbsorbedCompounds);

        ChooseActions(in entity, ref ai, ref absorber, ref health, strain, random);

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
        ref Health health, float strain, Random random)
    {
        // Fetch all the components that are usually needed
        ref var position = ref entity.Get<WorldPosition>();

        ref var ourSpecies = ref entity.Get<SpeciesMember>();

        ref var organelles = ref entity.Get<OrganelleContainer>();

        ref var cellProperties = ref entity.Get<CellProperties>();

        ref var signaling = ref entity.Get<CommandSignaler>();

        ref var engulfer = ref entity.Get<Engulfer>();

        ref var control = ref entity.Get<MicrobeControl>();

        ref var compoundStorage = ref entity.Get<CompoundStorage>();

        var compounds = compoundStorage.Compounds;

        // Adjusted behaviour values (calculated here as these are needed by various methods)
        var speciesBehaviour = ourSpecies.Species.Behaviour;
        float speciesAggression = speciesBehaviour.Aggression *
            (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.5f : 1.0f);

        float speciesFear = speciesBehaviour.Fear *
            (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 0.75f : 1.0f);

        float speciesActivity = speciesBehaviour.Activity *
            (signaling.ReceivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.25f : 1.0f);

        // Adjust activity for night if it is currently night
        // TODO: also check if the current species relies on varying compounds (otherwise it shouldn't react to it
        // being night)
        if (currentlyNight && GetIsSpeciesUsingVaryingCompounds(ourSpecies.Species))
        {
            speciesActivity *= MicrobeInternalCalculations.GetActivityNightModifier(speciesBehaviour.Activity);
        }

        float speciesFocus = speciesBehaviour.Focus;
        float speciesOpportunism = speciesBehaviour.Opportunism;

        control.Sprinting = false;

        // If nothing is engulfing me right now, see if there's something that might want to hunt me
        (Entity Entity, Vector3 Position, float EngulfSize)? predator =
            GetNearestPredatorItem(ref health, ref ourSpecies, ref engulfer, ref position, speciesFear);
        if (predator.HasValue && position.Position.DistanceSquaredTo(predator.Value.Position) <
            1500.0 * speciesFear / Constants.MAX_SPECIES_FEAR)
        {
            // If microbe secretes mucus and predator is still there skip taking any action
            if (control.State == MicrobeState.MucocystShield)
                return;

            FleeFromPredators(ref position, ref ai, ref control, ref organelles, ref compoundStorage, entity,
                predator.Value.Position, predator.Value.Entity, speciesFocus,
                speciesActivity, speciesAggression, speciesFear, strain, random);
            return;
        }

        // If there are no predators stop secreting mucus
        if (control.State == MicrobeState.MucocystShield)
        {
            control.SetMucocystState(ref organelles, ref compoundStorage, entity, false);
        }

        var radiationAmount = compounds.GetCompoundAmount(Compound.Radiation);
        var radiationFraction = radiationAmount / compounds.GetCapacityForCompound(Compound.Radiation);

        if (radiationFraction > Constants.RADIATION_DAMAGE_THRESHOLD * 0.7f)
        {
            if (RunFromNearestRadioactiveChunk(ref position, ref ai, ref control))
            {
                return;
            }
        }

        float atpLevel = compounds.GetCompoundAmount(Compound.ATP);

        // If this microbe is out of ATP, pick an amount of time to rest
        if (atpLevel < 1.0f)
        {
            // Keep the maximum at 95% full, as there is flickering when near full
            ai.ATPThreshold = 0.95f * speciesFocus / Constants.MAX_SPECIES_FOCUS;
        }

        // Allow the microbe to engulf the prey even if out of ATP
        if (CheckForHuntingConditions(ref ai, ref position, ref organelles, ref ourSpecies, ref engulfer,
                ref cellProperties, ref control, ref health, ref compoundStorage, entity, speciesFocus,
                speciesAggression, speciesActivity, speciesOpportunism, strain, random, true))
        {
            return;
        }

        if (ai.ATPThreshold > MathUtils.EPSILON)
        {
            if (atpLevel < compounds.GetCapacityForCompound(Compound.ATP) * ai.ATPThreshold)
            {
                bool outOfSomething = false;
                foreach (var compound in compounds.Compounds)
                {
                    if (IsVitalCompound(compound.Key, compounds) && compound.Value <= MathUtils.EPSILON)
                    {
                        outOfSomething = true;
                        break;
                    }
                }

                // If we have enough of everything that makes atp, wait a bit to generate some more
                if (!outOfSomething)
                {
                    control.SetMoveSpeed(0.0f);
                    return;
                }
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
                // The threshold is not too high so that not all cells are forced to move
                if (position.Position.DistanceSquaredTo(potentiallyKnownPlayerPosition.Value) >
                    Math.Pow(Constants.SPAWN_SECTOR_SIZE, 2) * Constants.ON_STAGE_THRESHOLD_AROUND_PLAYER &&
                    !isSessile)
                {
                    // And some randomness is added here so that each cell doesn't literally want to move to the exact
                    // same position
                    // TODO: this probably gets re-called for each cell quite often so this randomness may not be
                    // very efficient as the values will average out over multiple calls
                    var randomness =
                        new Vector3(random.NextSingle() * Constants.ON_STAGE_DESTINATION_RANDOMNESS -
                            Constants.ON_STAGE_DESTINATION_RANDOMNESS * 0.5f, 0,
                            random.NextSingle() * Constants.ON_STAGE_DESTINATION_RANDOMNESS -
                            Constants.ON_STAGE_DESTINATION_RANDOMNESS * 0.5f);

                    ai.MoveToLocation(potentiallyKnownPlayerPosition.Value + randomness, ref control, entity);
                    return;
                }

                ai.HasBeenNearPlayer = true;
            }
        }

        var isIronEater = organelles.IronBreakdownEfficiency > 0;

        // Siderophore is an experimental feature
        if (!gameWorld!.WorldSettings.ExperimentalFeatures)
            isIronEater = false;

        // If there are no threats, look for a chunk to eat
        // TODO: still consider engulfing things if we're in a colony that can engulf (has engulfer cells)
        if (cellProperties.MembraneType.CanEngulf)
        {
            var targetChunk = GetNearestChunkItem(in entity, ref engulfer, ref control, ref position, compounds,
                speciesFocus, speciesOpportunism, random, isIronEater, strain, out var isChunkBigIron);

            if (targetChunk != null)
            {
                PursueAndConsumeChunks(ref position, ref ai, ref control, ref engulfer, entity,
                    targetChunk.Value.Position, speciesActivity, random, ref organelles, isIronEater, isChunkBigIron);
                return;
            }
        }

        // Check if species can hunt any prey and if so - engage in chase
        if (CheckForHuntingConditions(ref ai, ref position, ref organelles, ref ourSpecies, ref engulfer,
                ref cellProperties, ref control, ref health, ref compoundStorage, entity, speciesFocus,
                speciesAggression,
                speciesActivity, speciesOpportunism, strain, random, false))
        {
            return;
        }

        // There is no reason to be engulfing at this stage
        control.SetStateColonyAware(entity, MicrobeState.Normal);

        // If the microbe has radiation protection it means it has melanosomes and can stay near the radioactive chunks
        // to produce ATP
        if (organelles.RadiationProtection > 0)
        {
            if (GoNearRadioactiveChunk(ref position, ref ai, ref control, speciesFocus, random))
            {
                return;
            }
        }

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

    private bool CheckForHuntingConditions(ref MicrobeAI ai, ref WorldPosition position,
        ref OrganelleContainer organelles, ref SpeciesMember ourSpecies,
        ref Engulfer engulfer, ref CellProperties cellProperties, ref MicrobeControl control, ref Health health,
        ref CompoundStorage compoundStorage, in Entity entity, float speciesFocus, float speciesAggression,
        float speciesActivity, float speciesOpportunism, float strain, Random random, bool outOfAtp)
    {
        var compounds = compoundStorage.Compounds;

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
                return false;
            }

            bool engulfPrey = cellProperties.CanEngulfObject(ref ourSpecies, ref engulfer, possiblePrey) ==
                EngulfCheckResult.Ok && position.Position.DistanceSquaredTo(prey) <
                10.0f * engulfer.EngulfingSize;

            // If out of ATP and the prey is out of reach to engulf, do nothing
            if (outOfAtp && !engulfPrey)
            {
                return false;
            }

            EngagePrey(ref ai, ref control, ref organelles, ref position, ref compoundStorage, ref health, entity,
                prey, engulfPrey, speciesAggression, speciesFocus, speciesActivity, strain, random);
            return true;
        }

        return false;
    }

    private (Entity Entity, Vector3 Position, CompoundBag Compounds)? GetNearestRadioactiveChunk(
        ref WorldPosition position, float maxDistance)
    {
        (Entity Entity, Vector3 Position, CompoundBag Compounds)? chosenChunk = null;
        float bestFoundChunkDistance = float.MaxValue;

        BuildChunksCache();

        foreach (var chunk in terrainChunkDataCache)
        {
            if (!chunk.Compounds.Compounds.Keys.Contains(Compound.Radiation))
            {
                continue;
            }

            var distance = (chunk.Position - position.Position).LengthSquared();

            if (distance > bestFoundChunkDistance)
                continue;

            if (distance > maxDistance)
                continue;

            chosenChunk = chunk;
        }

        return chosenChunk;
    }

    private bool RunFromNearestRadioactiveChunk(ref WorldPosition position, ref MicrobeAI ai,
        ref MicrobeControl control)
    {
        var chosenChunk = GetNearestRadioactiveChunk(ref position, 500.0f);

        if (chosenChunk == null)
        {
            return false;
        }

        var oppositeDirection = position.Position + (position.Position - chosenChunk.Value.Position);
        oppositeDirection = oppositeDirection.Normalized() * 500.0f;

        ai.TargetPosition = oppositeDirection;
        control.LookAtPoint = ai.TargetPosition;

        control.SetMoveSpeedTowardsPoint(ref position, ai.TargetPosition, Constants.AI_BASE_MOVEMENT);
        control.Sprinting = true;

        return true;
    }

    private bool GoNearRadioactiveChunk(ref WorldPosition position, ref MicrobeAI ai,
        ref MicrobeControl control, float speciesFocus, Random random)
    {
        var maxDistance = 30000.0f * speciesFocus / Constants.MAX_SPECIES_FOCUS + 3000.0f;
        var chosenChunk = GetNearestRadioactiveChunk(ref position, maxDistance);

        if (chosenChunk == null)
        {
            return false;
        }

        // Range from 0.8 to 1.2
        var randomMultiplier = (float)random.NextDouble() * 0.4f + 0.8f;

        // If the microbe is close to the chunk it doesn't need to go any closer
        if (position.Position.DistanceSquaredTo(chosenChunk.Value.Position) < 800.0f * randomMultiplier)
        {
            control.SetMoveSpeed(0.0f);
            return true;
        }

        ai.TargetPosition = chosenChunk.Value.Position;
        control.LookAtPoint = ai.TargetPosition;

        control.SetMoveSpeedTowardsPoint(ref position, ai.TargetPosition, Constants.AI_BASE_MOVEMENT);

        return true;
    }

    private (Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)? GetNearestChunkItem(
        in Entity entity, ref Engulfer engulfer, ref MicrobeControl control, ref WorldPosition position,
        CompoundBag ourCompounds, float speciesFocus, float speciesOpportunism, Random random, bool ironEater,
        float strain, out bool isBigIron)
    {
        (Entity Entity, Vector3 Position, float EngulfSize, CompoundBag Compounds)? chosenChunk = null;
        float bestFoundChunkDistance = float.MaxValue;

        BuildChunksCache();

        isBigIron = false;

        // Retrieve nearest potential chunk
        foreach (var chunk in chunkDataCache)
        {
            if (!ironEater)
            {
                // Skip too big things

                if (engulfer.EngulfingSize < chunk.EngulfSize * Constants.ENGULF_SIZE_RATIO_REQ)
                    continue;
            }

            // And too distant things
            var distance = (chunk.Position - position.Position).LengthSquared();

            if (distance > bestFoundChunkDistance)
                continue;

            if (distance > (20000.0 * speciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0)
                continue;

            foreach (var p in chunk.Compounds.Compounds)
            {
                if (ourCompounds.IsUseful(p.Key) && SimulationParameters.GetCompound(p.Key).Digestible)
                {
                    if (chosenChunk == null)
                    {
                        chosenChunk = chunk;
                        bestFoundChunkDistance = distance;

                        if (ironEater)
                        {
                            // TODO: this should have a more robust check (than just pure size)
                            if (p.Key == Compound.Iron && chunk.EngulfSize > 50)
                            {
                                isBigIron = true;
                            }
                        }
                    }

                    break;
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
                        ++rivals;
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
            else if (rivals > rivalThreshold / 2 && speciesOpportunism > Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3
                     && strain <= Constants.MAX_STRAIN_PER_ENTITY * 0.75)
            {
                // Opportunistic species should sprint to the chunk if there are many rivals
                control.Sprinting = true;
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
                    3500.0f * speciesFocus / Constants.MAX_SPECIES_FOCUS)
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
                // Based on species fear, threshold to be afraid of, ranges from 0.8 to 1.8 microbe size.
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
        ref Engulfer engulfer, in Entity entity, Vector3 chunk, float speciesActivity, Random random,
        ref OrganelleContainer organelles, bool isIronEater = false, bool chunkIsIron = false)
    {
        // This is a slight offset of where the chunk is, to avoid a forward-facing part blocking it
        ai.TargetPosition = chunk + new Vector3(0.5f, 0.0f, 0.5f);
        control.LookAtPoint = ai.TargetPosition;

        // Check if using siderophore
        if (isIronEater && chunkIsIron && gameWorld!.WorldSettings.ExperimentalFeatures)
        {
            control.EmitSiderophore(ref organelles, entity);
        }
        else
        {
            SetEngulfIfClose(ref control, ref engulfer, ref position, entity, chunk);
        }

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
            control.SetMoveSpeedTowardsPoint(ref position, chunk, Constants.AI_BASE_MOVEMENT);
        }
    }

    private void FleeFromPredators(ref WorldPosition position, ref MicrobeAI ai, ref MicrobeControl control,
        ref OrganelleContainer organelles, ref CompoundStorage compoundStorage, in Entity entity,
        Vector3 predatorLocation, Entity predatorEntity, float speciesFocus, float speciesActivity,
        float speciesAggression, float speciesFear, float strain, Random random)
    {
        var ourCompounds = compoundStorage.Compounds;
        control.SetStateColonyAware(entity, MicrobeState.Normal);

        ai.TargetPosition = (2 * (position.Position - predatorLocation)) + position.Position;

        control.LookAtPoint = ai.TargetPosition;

        // TODO: shouldn't this distance value scale with predator's and prey's cell radius?
        // When the players cell is really big this distance value might not be enough
        if (position.Position.DistanceSquaredTo(predatorLocation) < 100.0f)
        {
            bool shouldSprint = true;

            var predatorState = predatorEntity.Get<MicrobeControl>().State;

            // Calculate mucilage required to activate the mucocyst
            var mucilageCapactiy = ourCompounds.GetCapacityForCompound(Compound.Mucilage);
            var mucilageRequired = mucilageCapactiy * Constants.MUCOCYST_ACTIVATION_MUCILAGE_FRACTION;

            if ((organelles.SlimeJets?.Count ?? 0) > 0 &&
                RollCheck(speciesFear, Constants.MAX_SPECIES_FEAR, random))
            {
                // There's a chance to jet away if we can
                control.SecreteSlimeForSomeTime(ref organelles, random);
            }
            else if (ourCompounds.GetCompoundAmount(Compound.Mucilage) > mucilageRequired &&
                     organelles.MucocystCount > 0 && (strain >= Constants.MAX_STRAIN_PER_ENTITY * 0.70 ||
                         RollCheck(speciesFear, Constants.MAX_SPECIES_FEAR, random) ||
                         predatorState == MicrobeState.Engulf))
            {
                // If the microbe is exhausted, too close to predator or the predator starts to engulf, use mucus
                control.SetMucocystState(ref organelles, ref compoundStorage, entity, true);

                // Don't take any other action
                return;
            }
            else if (RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
            {
                // If the predator is right on top of us there's a chance to try and swing with a pilus
                ai.MoveWithRandomTurn(2.5f, 3.0f, position.Position, ref control, speciesActivity, random);

                // If attacking the predator do no sprint
                shouldSprint = false;
            }

            if (shouldSprint)
            {
                if (strain <= Constants.MAX_STRAIN_PER_ENTITY * 0.75 ||
                    (strain <= Constants.MAX_STRAIN_PER_ENTITY &&
                        position.Position.DistanceSquaredTo(predatorLocation) < 25.0f))
                {
                    // Otherwise just sprint from the predator. If the predator is too close sprint until fully strained
                    control.Sprinting = true;
                }
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
        ref WorldPosition position, ref CompoundStorage compoundStorage, ref Health health, in Entity entity,
        Vector3 target, bool engulf, float speciesAggression, float speciesFocus, float speciesActivity,
        float strain, Random random)
    {
        var ourCompounds = compoundStorage.Compounds;

        if (engulf)
        {
            control.EnterEngulfModeForcedState(ref health, ref compoundStorage, entity, Compound.ATP);
        }
        else
        {
            control.SetStateColonyAware(entity, MicrobeState.Normal);
        }

        ai.TargetPosition = target;
        control.LookAtPoint = ai.TargetPosition;
        if (CanShootToxin(ourCompounds, speciesFocus))
        {
            LaunchToxin(ref control, ref organelles, ref position, target, ourCompounds, speciesFocus,
                speciesActivity);

            if (RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION / 5, random))
            {
                control.SetMoveSpeedTowardsPoint(ref position, target, Constants.AI_BASE_MOVEMENT);
            }
        }
        else
        {
            control.SetMoveSpeedTowardsPoint(ref position, target, Constants.AI_BASE_MOVEMENT);

            if (strain <= Constants.MAX_STRAIN_PER_ENTITY * 0.5 ||
                (strain <= Constants.MAX_STRAIN_PER_ENTITY * 0.9
                    && RollCheck(speciesAggression, Constants.MAX_SPECIES_AGGRESSION, random)))
            {
                // Aggressive species should sprint while hunting the prey even if it costs more strain
                control.Sprinting = true;
            }
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
        if (RollCheck(speciesActivity, Constants.MAX_SPECIES_ACTIVITY + Constants.MAX_SPECIES_ACTIVITY / 2,
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
            ai.LastSmelledCompoundPosition = detections.OrderBy(d =>
                weights.TryGetValue(d.Compound, out var weight) ?
                    weight :
                    0).First().Target;
            ai.PursuitThreshold = position.Position.DistanceSquaredTo(ai.LastSmelledCompoundPosition.Value)
                * (1 + speciesFocus / Constants.MAX_SPECIES_FOCUS);
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
            ai.MoveWithRandomTurn(0, MathF.PI, position.Position, ref control, speciesActivity, random);
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
                if (compound is Compound.Ammonia or Compound.Phosphates)
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
        return compounds.IsUseful(compound) && compound is Compound.Glucose or Compound.Iron;
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
                var currentLookDirection = position.Rotation * Vector3.Forward;

                if (currentLookDirection.Normalized()
                        .AngleTo((control.LookAtPoint - position.Position).Normalized()) <
                    0.1f + speciesActivity / (Constants.AI_BASE_TOXIN_SHOOT_ANGLE_PRECISION * speciesFocus))
                {
                    control.QueuedToxinToEmit = Compound.Oxytoxy;
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
                || sizeRatio >= Constants.ENGULF_SIZE_RATIO_REQ);
    }

    private bool CanShootToxin(CompoundBag compounds, float speciesFocus)
    {
        return compounds.GetCompoundAmount(Compound.Oxytoxy) >=
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * speciesFocus / Constants.MAX_SPECIES_FOCUS;
    }

    private void CleanMicrobeCache()
    {
        // Skip when cache hasn't been updated in the meantime, this avoids unnecessarily clearing out a bunch of
        // data from the cache as after we clear the lists, the lists won't be filled again until the cache is
        // rebuild
        if (!microbeCacheBuilt)
            return;

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

    private bool GetIsSpeciesUsingVaryingCompounds(Species species)
    {
        // TODO: switch this to thread local data storage
        lock (speciesUsingVaryingCompounds)
        {
            if (speciesUsingVaryingCompounds.TryGetValue(species, out var result))
                return result;

            if (gameWorld == null)
                throw new InvalidOperationException("Game world should be set here");

            var patch = gameWorld.Map.CurrentPatch;

            if (patch == null)
            {
                GD.PrintErr("Current patch should be set for the microbe AI");
                patch = gameWorld.Map.Patches.First().Value;
            }

            if (species is MicrobeSpecies microbeSpecies)
            {
                // TODO: thread local storage for this cache
                result = MicrobeInternalCalculations.UsesDayVaryingCompounds(microbeSpecies.Organelles, patch.Biome,
                    varyingCompoundsTemporary);
            }
            else if (species is MulticellularSpecies multicellularSpecies)
            {
                // TODO: should this use the actual cell from the species that is running the AI? This isn't fully
                // accurate.
                // TODO: thread local storage for this cache
                result = MicrobeInternalCalculations.UsesDayVaryingCompounds(multicellularSpecies.Cells[0].Organelles,
                    patch.Biome, varyingCompoundsTemporary);
            }
            else
            {
                result = false;
                GD.PrintErr("Unhandled species type in microbe AI system varying compounds check");
            }

            speciesUsingVaryingCompounds[species] = result;
            return result;
        }
    }

    private void CleanSpeciesUsingVaryingCompound()
    {
        speciesUsingVaryingCompounds.Clear();
    }

    /// <summary>
    ///   Builds a full cache of all non-engulfed chunks that aren't dissolving currently
    /// </summary>
    private void BuildChunksCache()
    {
        // To allow multithreaded AI access safely
        // As the chunk lock is always held when building all of the chunk caches,
        // the other individual cache objects don't need separate locks to protect them
        lock (chunkDataCache)
        {
            if (chunkCacheBuilt)
                return;

            foreach (ref readonly var chunk in chunksSet.GetEntities())
            {
                if (chunk.Has<TimedLife>())
                {
                    // Ignore already despawning chunks
                    ref var timed = ref chunk.Get<TimedLife>();

                    if (timed.TimeToLiveRemaining <= 0)
                        continue;
                }

                // Ignore chunks that wouldn't yield any useful compounds when absorbing
                ref var compounds = ref chunk.Get<CompoundStorage>();

                if (!compounds.Compounds.HasAnyCompounds())
                    continue;

                // TODO: determine if it is a good idea to resolve this data here immediately
                ref var position = ref chunk.Get<WorldPosition>();

                if (chunk.Has<Engulfable>())
                {
                    ref var engulfable = ref chunk.Get<Engulfable>();
                    chunkDataCache.Add((chunk, position.Position, engulfable.AdjustedEngulfSize,
                        compounds.Compounds));
                }
                else
                {
                    terrainChunkDataCache.Add((chunk, position.Position, compounds.Compounds));
                }
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
                thinkRandoms.Add(new XoShiRo256starstar(aiThinkRandomSource.Next()));
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
