using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Nito.Collections;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
[JsonObject(IsReference = true)]
public class SpawnSystem : ISpawnSystem
{
    /// <summary>
    ///   Sets how often the spawn system runs and checks things
    /// </summary>
    [JsonProperty]
    private float interval = 1.0f;

    [JsonProperty]
    private float elapsed;

    [JsonProperty]
    private float despawnElapsed;

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    private ShuffleBag<Spawner> spawnTypes;

    [JsonProperty]
    private Random random = new();

    /// <summary>
    ///   This is used to spawn only a few entities per frame with minimal changes needed to code that wants to
    ///   spawn a bunch of stuff at once
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This isn't saved but the likelihood that losing out on spawning some things is not super critical.
    ///     Also it is probably the case that this isn't even used on most frames so it is perhaps uncommon
    ///     that there are queued things when saving.
    ///   </para>
    /// </remarks>
    private Deque<QueuedSpawn> queuedSpawns = new();

    /// <summary>
    ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
    /// </summary>
    private int estimateEntityCount;

    /// <summary>
    ///   Estimate count of existing spawn entities within the current spawn radius of the player;
    ///   Used to prevent a "spawn belt" of densely spawned entities when player doesn't move.
    /// </summary>
    [JsonProperty]
    private HashSet<Int2> coordinatesSpawned = new();

    public SpawnSystem(Node root)
    {
        worldRoot = root;
        spawnTypes = new ShuffleBag<Spawner>(random);
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
    ///   Removes a spawn type immediately. Note that it's easier to just set DestroyQueued to true on an spawner.
    /// </summary>
    public void RemoveSpawnType(Spawner spawner)
    {
        spawnTypes.Remove(spawner);
    }

    public void Init()
    {
        Clear();
    }

    public void Clear()
    {
        spawnTypes.Clear();

        foreach (var queuedSpawn in queuedSpawns)
            queuedSpawn.Dispose();

        queuedSpawns.Clear();

        elapsed = 0;
        despawnElapsed = 0;
    }

    public void DespawnAll()
    {
        ClearSpawnQueue();
        int despawned = 0;

        foreach (var spawned in worldRoot.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP))
        {
            if (!spawned.EntityNode.IsQueuedForDeletion())
            {
                spawned.DestroyDetachAndQueueFree();
                ++despawned;
            }
        }

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

    public void Process(float delta, Vector3 playerPosition)
    {
        elapsed += delta;
        despawnElapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        int spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

        // If we have queued spawns to do spawn those
        HandleQueuedSpawns(ref spawnsLeftThisFrame, playerPosition);

        if (spawnsLeftThisFrame <= 0)
            return;

        // This is now an if to make sure that the spawn system is
        // only ran once per frame to avoid spawning a bunch of stuff
        // all at once after a lag spike
        // NOTE: that as QueueFree is used it's not safe to just switch this to a loop
        if (elapsed >= interval)
        {
            elapsed -= interval;

            estimateEntityCount = DespawnEntities(playerPosition);

            spawnTypes.RemoveAll(entity => entity.DestroyQueued);

            SpawnEntities(playerPosition, ref spawnsLeftThisFrame, estimateEntityCount);
        }
        else if (despawnElapsed > Constants.DESPAWN_INTERVAL)
        {
            despawnElapsed = 0;

            DespawnEntities(playerPosition);
        }
    }

    public void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadiusSquared = Constants.MICROBE_DESPAWN_RADIUS_SQUARED;
        entity.EntityNode.AddToGroup(Constants.SPAWNED_GROUP);

        // Update entity count estimate to keep this about up to date, this will be corrected within a few seconds
        // with the next spawn cycle to be exactly correct
        ++estimateEntityCount;
    }

    public bool IsUnderEntityLimitForReproducing()
    {
        return estimateEntityCount < Settings.Instance.MaxSpawnedEntities.Value *
            Constants.REPRODUCTION_ALLOW_EXCEED_ENTITY_LIMIT_MULTIPLIER;
    }

    private void HandleQueuedSpawns(ref int spawnsLeftThisFrame, Vector3 playerPosition)
    {
        int spawned = 0;

        // Spawn from the queue
        while (spawnsLeftThisFrame > 0 && queuedSpawns.Count > 0)
        {
            var spawn = queuedSpawns.First();
            var enumerator = spawn.Spawns;

            bool finished = false;

            while (estimateEntityCount < Settings.Instance.MaxSpawnedEntities &&
                   spawnsLeftThisFrame > 0)
            {
                if (!enumerator.MoveNext())
                {
                    finished = true;
                    break;
                }

                if (enumerator.Current == null)
                    throw new Exception("Queued spawn enumerator returned null");

                // Discard the whole spawn if we're too close to the player
                var entityPosition = ((Spatial)enumerator.Current).GlobalTransform.origin;
                if ((playerPosition - entityPosition).Length() < Constants.SPAWN_SECTOR_SIZE)
                {
                    enumerator.Current.DestroyDetachAndQueueFree();
                    finished = true;
                    break;
                }

                // Next was spawned
                ProcessSpawnedEntity(enumerator.Current, spawn.SpawnType);

                ++estimateEntityCount;
                --spawnsLeftThisFrame;
                ++spawned;
            }

            if (finished)
            {
                // Finished spawning everything from this enumerator, if we didn't finish we save this spawn for the
                // next queued spawns handling cycle
                queuedSpawns.RemoveFromFront();
                spawn.Dispose();
            }
            else
            {
                break;
            }
        }

        if (spawned > 0)
        {
            var debugOverlay = DebugOverlays.Instance;

            if (debugOverlay.PerformanceMetricsVisible)
                debugOverlay.ReportSpawns(spawned);
        }
    }

    private void SpawnEntities(Vector3 playerPosition, ref int spawnsLeftThisFrame, int existing)
    {
        // If there are already too many entities, don't spawn more
        if (existing >= Settings.Instance.MaxSpawnedEntities)
            return;

        var playerCoordinatePoint = new Tuple<int, int>(Mathf.RoundToInt(playerPosition.x /
            Constants.SPAWN_SECTOR_SIZE), Mathf.RoundToInt(playerPosition.z / Constants.SPAWN_SECTOR_SIZE));

        // Spawn for all sectors immediately outside a 3x3 box around the player
        var sectorsToSpawn = new List<Int2>(12);
        for (int y = -1; y <= 1; y++)
        {
            sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2 + y));
        }

        for (int x = -1; x <= 1; x++)
        {
            sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2 + x));
        }

        for (int y = -1; y <= 1; y++)
        {
            sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + y, playerCoordinatePoint.Item2 - 2));
        }

        for (int x = -1; x <= 1; x++)
        {
            sectorsToSpawn.Add(new Int2(playerCoordinatePoint.Item1 + x, playerCoordinatePoint.Item2 + 2));
        }

        foreach (var newSector in sectorsToSpawn)
        {
            if (coordinatesSpawned.Add(newSector))
            {
                SpawnInSector(newSector, ref spawnsLeftThisFrame);
            }
        }

        SpawnMicrobesAroundPlayer(playerPosition, ref spawnsLeftThisFrame);
    }

    /// <summary>
    ///   Handles all spawning for this section of the play area, as it will look when the player enters. Does NOT
    ///   handle recording that the sector was spawned.
    /// </summary>
    /// <param name="sector">
    ///   X/Y coordinates of the sector to be spawned, in <see cref="Constants.SPAWN_SECTOR_SIZE" /> units
    /// </param>
    /// <param name="spawnsLeftThisFrame">How many spawns are still allowed this frame</param>
    private void SpawnInSector(Int2 sector, ref int spawnsLeftThisFrame)
    {
        int spawns = 0;

        foreach (var spawnType in spawnTypes)
        {
            var sectorCenter = new Vector3(sector.x * Constants.SPAWN_SECTOR_SIZE, 0,
                sector.y * Constants.SPAWN_SECTOR_SIZE);

            // Distance from the sector center.
            var displacement = new Vector3(random.NextFloat() * Constants.SPAWN_SECTOR_SIZE -
                (Constants.SPAWN_SECTOR_SIZE / 2), 0,
                random.NextFloat() * Constants.SPAWN_SECTOR_SIZE - (Constants.SPAWN_SECTOR_SIZE / 2));

            spawns += SpawnWithSpawner(spawnType, sectorCenter + displacement, ref spawnsLeftThisFrame);
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportSpawns(spawns);
    }

    private void SpawnMicrobesAroundPlayer(Vector3 playerLocation, ref int spawnsLeftThisFrame)
    {
        var angle = random.NextFloat() * 2 * Mathf.Pi;

        int spawns = 0;
        foreach (var spawnType in spawnTypes)
        {
            if (spawnType is MicrobeSpawner)
            {
                spawns += SpawnWithSpawner(spawnType,
                    playerLocation + new Vector3(Mathf.Cos(angle) * Constants.SPAWN_SECTOR_SIZE * 2, 0,
                        Mathf.Sin(angle) * Constants.SPAWN_SECTOR_SIZE * 2), ref spawnsLeftThisFrame);
            }
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportSpawns(spawns);
    }

    /// <summary>
    ///   Does a single spawn with a spawner
    /// </summary>
    private int SpawnWithSpawner(Spawner spawnType, Vector3 location, ref int spawnsLeftThisFrame)
    {
        var spawns = 0;

        if (random.NextFloat() > spawnType.Density)
        {
            return spawns;
        }

        if (spawnType is CompoundCloudSpawner || estimateEntityCount < Settings.Instance.MaxSpawnedEntities)
        {
            var enumerable = spawnType.Spawn(worldRoot, location, this);

            if (enumerable == null)
                return spawns;

            bool finished = false;

            var spawner = enumerable.GetEnumerator();

            while (spawnsLeftThisFrame > 0)
            {
                if (!spawner.MoveNext())
                {
                    finished = true;
                    break;
                }

                if (spawner.Current == null)
                    throw new NullReferenceException("spawn enumerator is not allowed to return null");

                ProcessSpawnedEntity(spawner.Current, spawnType);
                ++spawns;
                ++estimateEntityCount;
                --spawnsLeftThisFrame;
            }

            if (!finished)
            {
                // Store the remaining items in the enumerator for later
                queuedSpawns.AddToBack(new QueuedSpawn(spawnType, spawner));
            }
            else
            {
                spawner.Dispose();
            }
        }

        return spawns;
    }

    /// <summary>
    ///   Despawns entities that are far away from the player
    /// </summary>
    /// <returns>The number of alive entities, used to limit the total</returns>
    private int DespawnEntities(Vector3 playerPosition)
    {
        int entitiesDeleted = 0;
        int spawnedCount = 0;

        foreach (var spawned in worldRoot.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP))
        {
            ++spawnedCount;

            // Global position must be used here as otherwise colony members are despawned
            // TODO: check if it would be better to remove the spawned group tag from colony members (and add it back
            // when leaving the colony) or this could only get direct descendants of the world root and ignore nested
            // nodes in the spawned group
            var entityPosition = ((Spatial)spawned).GlobalTransform.origin;
            var squaredDistance = (playerPosition - entityPosition).LengthSquared();

            // If the entity is too far away from the player, despawn it.
            if (squaredDistance > spawned.DespawnRadiusSquared)
            {
                ++entitiesDeleted;
                spawned.DestroyDetachAndQueueFree();

                if (entitiesDeleted >= Constants.MAX_DESPAWNS_PER_FRAME)
                    break;
            }
        }

        var debugOverlay = DebugOverlays.Instance;

        if (debugOverlay.PerformanceMetricsVisible)
            debugOverlay.ReportDespawns(entitiesDeleted);

        return spawnedCount - entitiesDeleted;
    }

    /// <summary>
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity, Spawner spawnType)
    {
        float radius = spawnType.SpawnRadius + Constants.DESPAWN_RADIUS_OFFSET;
        entity.DespawnRadiusSquared = (int)(radius * radius);

        entity.EntityNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    /// <summary>
    ///   Returns a random rotation (in radians)
    ///   If weighted, it is more likely to return a rotation closer to the target rotation than not
    /// </summary>
    private float ComputeRandomRadianRotation(float targetRotation, bool weighted)
    {
        float rotation1 = random.NextFloat() * 2 * Mathf.Pi;

        if (weighted)
        {
            targetRotation = WithNegativesToNormalRadians(targetRotation);
            float rotation2 = random.NextFloat() * 2 * Mathf.Pi;

            if (DistanceBetweenRadians(rotation2, targetRotation) < DistanceBetweenRadians(rotation1, targetRotation))
                return NormalToWithNegativesRadians(rotation2);
        }

        return NormalToWithNegativesRadians(rotation1);
    }

    // TODO Could use to be moved to mathUtils?
    private float NormalToWithNegativesRadians(float radian)
    {
        return radian <= Math.PI ? radian : radian - (float)(2 * Math.PI);
    }

    private float WithNegativesToNormalRadians(float radian)
    {
        return radian >= 0 ? radian : (float)(2 * Math.PI) - radian;
    }

    private float DistanceBetweenRadians(float p1, float p2)
    {
        float distance = Math.Abs(p1 - p2);
        return distance <= Math.PI ? distance : (float)(2 * Math.PI) - distance;
    }

    private class QueuedSpawn : IDisposable
    {
        public QueuedSpawn(Spawner spawnType, IEnumerator<ISpawned> spawns)
        {
            SpawnType = spawnType;
            Spawns = spawns;
        }

        public Spawner SpawnType { get; }

        public IEnumerator<ISpawned> Spawns { get; }

        public void Dispose()
        {
            Spawns.Dispose();
        }
    }
}
