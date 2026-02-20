namespace Systems;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;
using Godot;
using Nito.Collections;
using SharedBase.Archive;
using Xoshiro.PRNG64;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
[ReadsComponent(typeof(Spawned))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(SpatialAttachSystem))]
[RunsAfter(typeof(CountLimitedDespawnSystem))]
public partial class SpawnSystem : BaseSystem<World, float>, ISpawnSystem, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly QueryDescription spawnedQuery = new QueryDescription().WithAll<Spawned>();

    /// <summary>
    ///   Used to check if a spawn position is likely bad and should be retried
    /// </summary>
    private readonly IsSpawnPositionBad badSpawnPositionCheck;

    /// <summary>
    ///   Sets how often the spawn system runs and checks things
    /// </summary>
    private float interval = 1.0f;

    private float elapsed;

    private float despawnElapsed;

    private IWorldSimulation worldSimulation;

    private Vector3 playerPosition;

    private ShuffleBag<Spawner> spawnTypes;

    private XoShiRo256starstar random;

    /// <summary>
    ///   This is used to spawn only a few entities per frame with minimal changes needed to code that wants to
    ///   spawn a bunch of stuff at once
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This isn't saved, but the likelihood that losing out on spawning some things is not supercritical.
    ///     Also, it is probably the case that this isn't even used on most frames, so it is perhaps uncommon
    ///     that there are queued things when saving.
    ///     In addition, it would be very hard to make sure all the possible queued spawn type data (as it is mostly
    ///     based on temporary lambdas) is saved.
    ///   </para>
    /// </remarks>
    private Deque<SpawnQueue> queuedSpawns = new();

    /// <summary>
    ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
    /// </summary>
    private float estimateEntityCount;

    /// <summary>
    ///   Estimate count of existing spawn entities within the current spawn radius of the player;
    ///   Used to prevent a "spawn belt" of densely spawned entities when the player doesn't move.
    /// </summary>
    private HashSet<Vector2I> coordinatesSpawned = new();

    private float entitiesDeleted;
    private float spawnedEntityWeight;
    private int despawnedCount;

    private float spawnRadiusCheck = 5.5f;
    private int maxDifferentPositionsCheck = 20;

    public SpawnSystem(IWorldSimulation worldSimulation, World world, IsSpawnPositionBad badSpawnPositionCheck) :
        base(world)
    {
        this.worldSimulation = worldSimulation;
        this.badSpawnPositionCheck = badSpawnPositionCheck;

        random = new XoShiRo256starstar();

        spawnTypes = new ShuffleBag<Spawner>(random);
    }

    public delegate bool IsSpawnPositionBad(Vector3 position, float spawnRadius);

    public bool IsEnabled { get; set; } = true;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpawnSystem;
    public bool CanBeReferencedInArchive => true;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpawnSystem)
            throw new NotSupportedException();

        writer.WriteObject((SpawnSystem)obj);
    }

    public static SpawnSystem ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new SpawnSystem(reader.ReadObject<IWorldSimulation>(), reader.ReadObject<World>(),
            reader.ReadDelegate<IsSpawnPositionBad>() ?? throw new NullArchiveObjectException());

        instance.interval = reader.ReadFloat();
        instance.elapsed = reader.ReadFloat();
        instance.despawnElapsed = reader.ReadFloat();
        instance.random = reader.ReadObject<XoShiRo256starstar>();
        instance.coordinatesSpawned = reader.ReadObject<HashSet<Vector2I>>();

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(worldSimulation);
        writer.WriteAnyRegisteredValueAsObject(World);
        writer.WriteDelegate(badSpawnPositionCheck);
        writer.Write(interval);
        writer.Write(elapsed);
        writer.Write(despawnElapsed);
        writer.WriteAnyRegisteredValueAsObject(random);
        writer.WriteObject(coordinatesSpawned);
    }

    public override void Update(in float delta)
    {
        if (!IsEnabled)
            return;

        elapsed += delta;
        despawnElapsed += delta;

        float spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

        // If we have queued spawns to do spawn those
        HandleQueuedSpawns(ref spawnsLeftThisFrame);

        if (spawnsLeftThisFrame <= 0)
            return;

        // This is now an if to make sure that the spawn system is only run once per frame to avoid spawning a bunch
        // of stuff all at once after a lag spike
        // NOTE: that as QueueFree is used, it's not safe to just switch this to a loop
        if (elapsed >= interval)
        {
            elapsed -= interval;

            estimateEntityCount = CallDespawnQuery();

            spawnTypes.RemoveAll(s => s.DestroyQueued);

            SpawnAllTypes(ref spawnsLeftThisFrame);
        }
        else if (despawnElapsed > Constants.DESPAWN_INTERVAL)
        {
            despawnElapsed = 0;

            CallDespawnQuery();
        }
    }

    /// <summary>
    ///   Adds a new spawner. Sets up the spawn radius, this radius squared,
    ///   and frequency fields based on the parameters of this
    ///   function.
    /// </summary>
    public void AddSpawnType(Spawner spawner, float spawnDensity, int spawnRadius)
    {
        spawner.SpawnRadius = spawnRadius;
        spawner.SpawnRadiusSquared = spawnRadius * spawnRadius;

        float minSpawnRadius = spawnRadius * Constants.MIN_SPAWN_RADIUS_RATIO;
        spawner.MinSpawnRadiusSquared = minSpawnRadius * minSpawnRadius;
        spawner.Density = spawnDensity;

        spawnTypes.Add(spawner);
    }

    /// <summary>
    ///   Removes a spawn type immediately. Note that it's easier to just set DestroyQueued to true on a spawner.
    /// </summary>
    public void RemoveSpawnType(Spawner spawner)
    {
        spawnTypes.Remove(spawner);
    }

    public void DespawnAll()
    {
        ClearSpawnQueue();

        float despawned = 0.0f;

        // This allocates a lambda, but despawning everything should be rare enough that that is fine
        World.Query(spawnedQuery, (Entity entity, ref Spawned spawned) =>
        {
            if (spawned.DisallowDespawning)
                return;

            if (worldSimulation.IsEntityInWorld(entity))
            {
                despawned += spawned.EntityWeight;
                worldSimulation.DestroyEntity(entity);
            }
        });

        var debugOverlay = DebugOverlays.Instance;
        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportDespawns(despawned);

        ClearSpawnCoordinates();
    }

    /// <summary>
    ///   Clears all of the queued spawns. For use when the queue might contain something that
    ///   should not be allowed to spawn.
    /// </summary>
    public void ClearSpawnQueue()
    {
        foreach (var queuedSpawn in queuedSpawns)
            queuedSpawn.Dispose();

        queuedSpawns.Clear();
    }

    /// <summary>
    ///   Forgets all record of where clouds have spawned, so clouds can spawn anywhere.
    /// </summary>
    public void ClearSpawnCoordinates()
    {
        coordinatesSpawned.Clear();
    }

    public void ReportPlayerPosition(Vector3 position)
    {
        playerPosition = position;

        // Remove the y-position from the player position
        playerPosition.Y = 0;
    }

    public void NotifyExternalEntitySpawned(in Entity entity, CommandBuffer commandBuffer, float despawnRadiusSquared,
        float entityWeight)
    {
        if (entityWeight <= 0)
            throw new ArgumentException("weight needs to be positive", nameof(entityWeight));

        commandBuffer.Add(entity, new Spawned
        {
            DespawnRadiusSquared = despawnRadiusSquared,
            EntityWeight = entityWeight,
        });

        // Update entity count estimate to keep this about up to date, this will be corrected within a few seconds
        // with the next spawn cycle to be exactly correct
        estimateEntityCount += entityWeight;
    }

    public bool IsUnderEntityLimitForReproducing()
    {
        return estimateEntityCount < Settings.Instance.MaxSpawnedEntities.Value *
            Constants.REPRODUCTION_ALLOW_EXCEED_ENTITY_LIMIT_MULTIPLIER;
    }

    /// <summary>
    ///   Ensures that the entity limit is not overfilled by a lot after player reproduction by force despawning
    ///   things
    /// </summary>
    public void EnsureEntityLimitAfterPlayerReproduction(Vector3 keepEntitiesNear, Entity doNotDespawn)
    {
        float extra = 0;

        if (doNotDespawn.IsAliveAndHas<Spawned>())
        {
            // Take the just spawned thing we shouldn't despawn into account in the entity count as our estimate
            // won't likely include it yet
            extra = doNotDespawn.Get<Spawned>().EntityWeight;
        }

        var entityLimit = Settings.Instance.MaxSpawnedEntities.Value;

        float limitExcess = estimateEntityCount + extra - entityLimit *
            Constants.REPRODUCTION_PLAYER_ALLOWED_ENTITY_LIMIT_EXCEED;

        if (limitExcess < 1)
            return;

        // We need to despawn something
        GD.Print("After player reproduction entity limit is exceeded, will force despawn something");

        float playerReproductionWeight = 0;

        var playerReproducedEntities = new List<(Entity Entity, Vector3 Position, float Weight)>();

        // We use simple lambda allocations here as this is not called very often
        World.Query(new QueryDescription().WithAll<PlayerOffspring, Spawned, WorldPosition>(),
            (Entity entity, ref Spawned spawned, ref WorldPosition position) =>
            {
                if (worldSimulation.IsQueuedForDeletion(entity))
                    return;

                if (spawned.DisallowDespawning)
                    return;

                playerReproductionWeight += spawned.EntityWeight;

                if (doNotDespawn == Entity.Null || entity != doNotDespawn)
                {
                    playerReproducedEntities.Add((entity, position.Position, spawned.EntityWeight));
                }
            });

        // Despawn one player reproduced copy first if the player-reproduced copies are taking up a ton of space
        if (playerReproductionWeight > entityLimit * Constants.PREFER_DESPAWN_PLAYER_REPRODUCED_COPY_AFTER &&
            playerReproducedEntities.Count > 0)
        {
            var despawn = playerReproducedEntities
                .OrderByDescending(s => s.Position.DistanceSquaredTo(keepEntitiesNear)).First();

            estimateEntityCount -= despawn.Weight;
            limitExcess -= despawn.Weight;

            worldSimulation.DestroyEntity(despawn.Entity);
        }

        if (limitExcess <= 1)
            return;

        var candidateDespawns = new List<(Entity Entity, float Weight, Vector3 Position)>();

        // We take weight as well as distance into account here to not just despawn a ton of really far away objects
        // with weight of 1
        World.Query(new QueryDescription().WithAll<Spawned, WorldPosition>(), (Entity entity, ref Spawned spawned,
            ref WorldPosition position) =>
        {
            if (worldSimulation.IsQueuedForDeletion(entity))
                return;

            candidateDespawns.Add((entity, spawned.EntityWeight, position.Position));
        });

        using var deSpawnableEntities = candidateDespawns
            .OrderByDescending(t =>
                Math.Log(t.Position.DistanceSquaredTo(keepEntitiesNear)) + Math.Log(t.Weight))
            .GetEnumerator();

        // Then try to despawn enough stuff for us to get under the limit
        while (limitExcess >= 1)
        {
            (Entity Entity, float EntityWeight, Vector3 Position) bestCandidate;

            if (deSpawnableEntities.MoveNext())
            {
                bestCandidate = deSpawnableEntities.Current;
            }
            else
            {
                // If we couldn't despawn anything sensible, give up
                GD.PrintErr("Force despawning could not find enough things to despawn");
                break;
            }

            if (doNotDespawn != Entity.Null && bestCandidate.Entity == doNotDespawn)
                continue;

            if (worldSimulation.IsQueuedForDeletion(bestCandidate.Entity))
                continue;

            var weight = bestCandidate.EntityWeight;
            estimateEntityCount -= weight;
            limitExcess -= weight;
            worldSimulation.DestroyEntity(bestCandidate.Entity);
        }
    }

    private void HandleQueuedSpawns(ref float spawnsLeftThisFrame)
    {
        float spawnedCount = 0.0f;

        // Spawn from the queue
        while (spawnsLeftThisFrame > 0 && queuedSpawns.Count > 0)
        {
            var spawn = queuedSpawns.First();

            bool finished = false;

            while (estimateEntityCount < Settings.Instance.MaxSpawnedEntities.Value &&
                   spawnsLeftThisFrame > 0)
            {
                // Disallow spawning too close to the player
                spawn.CheckIsSpawningStillPossible(playerPosition);

                if (spawn.Ended)
                {
                    finished = true;
                    break;
                }

                // Next can be spawned
                var (recorder, weight) = spawn.SpawnNext(out var current);

                AddSpawnedComponent(current, recorder, weight, spawn.RelatedSpawnType);
                SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);

                estimateEntityCount += weight;
                spawnsLeftThisFrame -= weight;
                spawnedCount += weight;
            }

            if (finished)
            {
                // Finished spawning everything from this enumerator, if we didn't finish, we save this spawn for the
                // next queued spawns handling cycle
                queuedSpawns.RemoveFromFront();
                spawn.Dispose();
            }
            else
            {
                break;
            }
        }

        if (spawnedCount > 0)
        {
            var debugOverlay = DebugOverlays.Instance;

            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportSpawns(spawnedCount);
        }
    }

    private void SpawnAllTypes(ref float spawnsLeftThisFrame)
    {
        var playerCoordinatePoint = new Tuple<int, int>(MathUtils.RoundToInt(playerPosition.X /
            Constants.SPAWN_SECTOR_SIZE), MathUtils.RoundToInt(playerPosition.Z / Constants.SPAWN_SECTOR_SIZE));

        // Spawn for all sectors immediately outside a 3x3 box around the player
        var sectorsToSpawn = new List<Vector2I>(12);
        for (int y = -1; y <= 1; ++y)
        {
            sectorsToSpawn.Add(new Vector2I(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2 + y));
        }

        for (int x = -1; x <= 1; ++x)
        {
            sectorsToSpawn.Add(new Vector2I(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2 + x));
        }

        for (int y = -1; y <= 1; ++y)
        {
            sectorsToSpawn.Add(new Vector2I(playerCoordinatePoint.Item1 + y, playerCoordinatePoint.Item2 - 2));
        }

        for (int x = -1; x <= 1; ++x)
        {
            sectorsToSpawn.Add(new Vector2I(playerCoordinatePoint.Item1 + x, playerCoordinatePoint.Item2 + 2));
        }

        foreach (var newSector in sectorsToSpawn)
        {
            if (coordinatesSpawned.Add(newSector))
            {
                SpawnInSector(newSector, ref spawnsLeftThisFrame);
            }
        }

        // Only spawn microbes around the player if below the threshold.
        // This is to prioritize spawning in sectors.
        float entitiesThreshold = Settings.Instance.MaxSpawnedEntities.Value *
            Constants.ENTITY_SPAWNING_AROUND_PLAYER_THRESHOLD;
        if (estimateEntityCount < entitiesThreshold)
        {
            SpawnMicrobesAroundPlayer(playerPosition, ref spawnsLeftThisFrame);
        }
    }

    /// <summary>
    ///   Handles all spawning for this section of the play area, as it will look when the player enters. Does NOT
    ///   handle recording that the sector was spawned.
    /// </summary>
    /// <param name="sector">
    ///   X/Y coordinates of the sector to be spawned, in <see cref="Constants.SPAWN_SECTOR_SIZE" /> units
    /// </param>
    /// <param name="spawnsLeftThisFrame">Determines how many spawns are still allowed in this frame</param>
    private void SpawnInSector(Vector2I sector, ref float spawnsLeftThisFrame)
    {
        float spawns = 0.0f;

        foreach (var spawnType in spawnTypes)
        {
            if (SpawnsBlocked(spawnType))
                continue;

            var sectorCenter = new Vector3(sector.X * Constants.SPAWN_SECTOR_SIZE, 0,
                sector.Y * Constants.SPAWN_SECTOR_SIZE);

            bool spawned = false;

            for (int i = 0; i < maxDifferentPositionsCheck; ++i)
            {
                // Distance from the sector center.
                var displacement = new Vector3(random.NextSingle() * Constants.SPAWN_SECTOR_SIZE -
                    Constants.SPAWN_SECTOR_SIZE * 0.5f, 0,
                    random.NextSingle() * Constants.SPAWN_SECTOR_SIZE - Constants.SPAWN_SECTOR_SIZE * 0.5f);

                var finalPosition = sectorCenter + displacement;

                // Skip spawning stuff into the terrain
                if (badSpawnPositionCheck.Invoke(finalPosition, spawnRadiusCheck))
                    continue;

                spawned = true;
                spawns += SpawnWithSpawner(spawnType, sectorCenter + displacement, ref spawnsLeftThisFrame);
                break;
            }

            if (!spawned)
                GD.Print($"Skipped a spawn due to not finding position outside the terrain for {spawnType.Name}");
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportSpawns(spawns);
    }

    private void SpawnMicrobesAroundPlayer(Vector3 playerLocation, ref float spawnsLeftThisFrame)
    {
        var angle = random.NextSingle() * 2 * MathF.PI;

        float spawns = 0.0f;
        foreach (var spawnType in spawnTypes)
        {
            if (!SpawnsBlocked(spawnType) && spawnType is MicrobeSpawner)
            {
                for (int i = 0; i < maxDifferentPositionsCheck; ++i)
                {
                    var position = playerLocation + new Vector3(MathF.Cos(angle) * Constants.SPAWN_SECTOR_SIZE * 2, 0,
                        MathF.Sin(angle) * Constants.SPAWN_SECTOR_SIZE * 2);

                    if (badSpawnPositionCheck.Invoke(position, spawnRadiusCheck))
                        continue;

                    spawns += SpawnWithSpawner(spawnType, position, ref spawnsLeftThisFrame);
                    break;
                }
            }
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportSpawns(spawns);
    }

    /// <summary>
    ///   Checks whether we're currently blocked from spawning this type
    /// </summary>
    private bool SpawnsBlocked(Spawner spawnType)
    {
        return spawnType.SpawnsEntities && estimateEntityCount >= Settings.Instance.MaxSpawnedEntities.Value;
    }

    /// <summary>
    ///   Does a single spawn operation with a spawner. Does NOT check we're under the entity limit.
    /// </summary>
    private float SpawnWithSpawner(Spawner spawnType, Vector3 location, ref float spawnsLeftThisFrame)
    {
        float spawns = 0.0f;

        if (random.NextSingle() > spawnType.Density)
        {
            return spawns;
        }

        var spawnQueue = spawnType.Spawn(worldSimulation, location, this);

        // Non-entity type spawn
        if (spawnQueue == null)
            return spawns;

        bool finished = false;

        while (spawnsLeftThisFrame > 0)
        {
            spawnQueue.CheckIsSpawningStillPossible(playerPosition);

            if (spawnQueue.Ended)
            {
                finished = true;
                break;
            }

            var (recorder, weight) = spawnQueue.SpawnNext(out var current);

            AddSpawnedComponent(current, recorder, weight, spawnType);
            SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);

            spawns += weight;
            estimateEntityCount += weight;
            spawnsLeftThisFrame -= weight;
        }

        if (!finished)
        {
            // Store the remaining items in the enumerator for later
            queuedSpawns.AddToBack(spawnQueue);
        }
        else
        {
            spawnQueue.Dispose();
        }

        return spawns;
    }

    private float CallDespawnQuery()
    {
        entitiesDeleted = 0.0f;
        spawnedEntityWeight = 0.0f;
        despawnedCount = 0;

        DespawnEntitiesQuery(World);

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportDespawns(entitiesDeleted);

        return spawnedEntityWeight - entitiesDeleted;
    }

    /// <summary>
    ///   Despawns entities that are far away from the player
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Updates <see cref="estimateEntityCount"/>, which must be set to 0 before triggering the query
    ///   </para>
    /// </remarks>
    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DespawnEntities(ref Spawned spawned, ref WorldPosition position, Entity entity)
    {
        if (spawned.DisallowDespawning)
            return;

        var entityWeight = spawned.EntityWeight;
        spawnedEntityWeight += entityWeight;

        // Keep counting all entities to have an accurate count at the end of this loop, even if we are no
        // longer allowed to despawn things
        if (despawnedCount >= Constants.MAX_DESPAWNS_PER_FRAME)
            return;

        // TODO: need to check if this accidentally despawns partial colonies
        var squaredDistance = (playerPosition - position.Position).LengthSquared();

        // If the entity is too far away from the player, despawn it.
        if (squaredDistance > spawned.DespawnRadiusSquared)
        {
            entitiesDeleted += entityWeight;
            worldSimulation.DestroyEntity(entity);

            ++despawnedCount;
        }
    }

    private void AddSpawnedComponent(in Entity entity, CommandBuffer commandBuffer, float weight, Spawner spawnType)
    {
        float radius = spawnType.SpawnRadius + Constants.DESPAWN_RADIUS_OFFSET;

        commandBuffer.Add(entity, new Spawned
        {
            DespawnRadiusSquared = radius * radius,
            EntityWeight = weight,
        });
    }
}
