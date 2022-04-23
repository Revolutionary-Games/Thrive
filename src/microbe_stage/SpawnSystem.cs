using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
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
    ///   <para>
    ///     TODO: it might be nice to use a struct instead and a field indicating if this is valid to not recreate
    ///     this object so much
    ///   </para>
    /// </remarks>
    private QueuedSpawn? queuedSpawns;

    /// <summary>
    ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
    /// </summary>
    private int estimateEntityCount;

    private int spawnsLeftThisFrame;

    /// <summary>
    ///   Estimate count of existing spawn entities within the current spawn radius of the player;
    ///   Used to prevent a "spawn belt" of densely spawned entities when player doesn't move.
    /// </summary>
    [JsonProperty]
    private HashSet<Tuple<int, int>> coordinatesSpawned = new();

    public SpawnSystem(Node root)
    {
        worldRoot = root;
        spawnTypes = new ShuffleBag<Spawner>(random);
    }

    // Needs no params constructor for loading saves?

    /// <summary>
    ///   Adds an externally spawned entity to be despawned
    /// </summary>
    public static void AddEntityToTrack(ISpawned entity,
        float radius = Constants.MICROBE_SPAWN_RADIUS)
    {
        entity.DespawnRadiusSquared = (int)(radius * radius) + Constants.DESPAWN_RADIUS_OFFSET_SQUARED;
        entity.EntityNode.AddToGroup(Constants.SPAWNED_GROUP);
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
        spawner.Denstity = spawnDensity;

        spawnTypes.Add(spawner);
    }

    /// <summary>
    ///   Removes a spawn type immediately. Note that it's easier to
    ///   just set DestroyQueued to true on an spawner.
    /// </summary>
    public void RemoveSpawnType(Spawner spawner)
    {
        spawnTypes.Remove(spawner);
    }

    /// <summary>
    ///   Prepares the spawn system for a new game
    /// </summary>
    public void Init()
    {
        Clear();
    }

    /// <summary>
    ///   Clears the spawners
    /// </summary>
    public void Clear()
    {
        spawnTypes.Clear();
        queuedSpawns = null;
        elapsed = 0;
        despawnElapsed = 0;
    }

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll()
    {
        queuedSpawns = null;

        int despawned = 0;

        foreach (var spawned in worldRoot.GetChildrenToProcess<ISpawned>(Constants.SPAWNED_GROUP))
        {
            if (!spawned.EntityNode.IsQueuedForDeletion())
            {
                spawned.DestroyDetachAndQueueFree();
                ++despawned;
            }
        }

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
            metrics.ReportDespawns(despawned);

        coordinatesSpawned = new HashSet<Tuple<int, int>>();
    }

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta, Vector3 playerPosition, Vector3 playerRotation)
    {
        elapsed += delta;
        despawnElapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

        // If we have queued spawns to do spawn those

        spawnsLeftThisFrame = HandleQueuedSpawns(spawnsLeftThisFrame);

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

            SpawnEntities(playerPosition, estimateEntityCount);
        }
        else if (despawnElapsed > Constants.DESPAWN_INTERVAL)
        {
            despawnElapsed = 0;

            DespawnEntities(playerPosition);
        }
    }

    private int HandleQueuedSpawns(int spawnsLeftThisFrame)
    {
        int initialSpawns = spawnsLeftThisFrame;

        if (queuedSpawns == null)
            return spawnsLeftThisFrame;

        // If we don't have room, just abandon spawning
        if (estimateEntityCount >= Constants.DEFAULT_MAX_SPAWNED_ENTITIES)
        {
            queuedSpawns.Spawns.Dispose();
            queuedSpawns = null;
            return spawnsLeftThisFrame;
        }

        // Spawn from the queue
        while (estimateEntityCount < Constants.DEFAULT_MAX_SPAWNED_ENTITIES && spawnsLeftThisFrame > 0)
        {
            if (!queuedSpawns.Spawns.MoveNext())
            {
                // Ended
                queuedSpawns.Spawns.Dispose();
                queuedSpawns = null;
                break;
            }

            // Next was spawned
            ProcessSpawnedEntity(
                queuedSpawns.Spawns.Current ?? throw new Exception("Queued spawn enumerator returned null"),
                queuedSpawns.SpawnType);

            ++estimateEntityCount;
            --spawnsLeftThisFrame;
        }

        if (initialSpawns != spawnsLeftThisFrame)
        {
            var metrics = PerformanceMetrics.Instance;

            if (metrics.Visible)
                metrics.ReportSpawns(initialSpawns - spawnsLeftThisFrame);
        }

        return spawnsLeftThisFrame;
    }

    private void SpawnEntities(Vector3 playerPosition, int existing)
    {
        // If there are already too many entities, don't spawn more
        if (existing >= Constants.DEFAULT_MAX_SPAWNED_ENTITIES)
            return;

        var playerCoordinatePoint = new Tuple<int, int>(Mathf.RoundToInt(playerPosition.x /
            Constants.SPAWN_SECTOR_SIZE), Mathf.RoundToInt(playerPosition.z / Constants.SPAWN_SECTOR_SIZE));

        // Spawn for all sectors immediately outside a 3x3 box around the player
        var sectorsToSpawn = new List<Tuple<int, int>>();
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2 - 1));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 - 2, playerCoordinatePoint.Item2 + 1));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2 - 1));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 + 2, playerCoordinatePoint.Item2 + 1));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 - 1, playerCoordinatePoint.Item2 - 2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1, playerCoordinatePoint.Item2 - 2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 + 1, playerCoordinatePoint.Item2 - 2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 - 1, playerCoordinatePoint.Item2 + 2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1, playerCoordinatePoint.Item2 + 2));
        sectorsToSpawn.Add(new Tuple<int, int>(playerCoordinatePoint.Item1 + 1, playerCoordinatePoint.Item2 + 2));

        foreach (Tuple<int, int> newSector in sectorsToSpawn)
        {
            if (!coordinatesSpawned.Contains(newSector))
            {
                coordinatesSpawned.Add(newSector);
                SpawnInSector(newSector);
            }
        }

        SpawnMicrobesAroundPlayer(playerPosition);
    }

    /// <summary>
    ///   Handles all spawning for this section of the play area, as it will look when the player enters. Does NOT
    ///   handle recording that the sector was spawned.
    /// </summary>
    /// <param name="sector">X/Y coordiates of the sector to be spawned, in SECTOR_SIZE units</param>
    private void SpawnInSector(Tuple<int, int> sector)
    {
        foreach (var spawnType in spawnTypes)
        {
            Vector3 sectorCenter = new Vector3(sector.Item1 * Constants.SPAWN_SECTOR_SIZE, 0,
                sector.Item2 * Constants.SPAWN_SECTOR_SIZE);

            // Distance from the sector center.
            Vector3 displacement = new Vector3(random.NextFloat() * Constants.SPAWN_SECTOR_SIZE -
                (Constants.SPAWN_SECTOR_SIZE / 2), 0,
                random.NextFloat() * Constants.SPAWN_SECTOR_SIZE - (Constants.SPAWN_SECTOR_SIZE / 2));

            // Second condition passed. Spawn the entity.
            SpawnWithSpawner(spawnType, sectorCenter + displacement);
        }
    }

    private void SpawnMicrobesAroundPlayer(Vector3 playerLocation)
    {
        var angle = random.NextFloat() * 2 * Mathf.Pi;

        var spawns = 0;
        foreach (var spawnType in spawnTypes)
        {
            if (spawnType is MicrobeSpawner)
            {
                spawns += SpawnWithSpawner(spawnType,
                    playerLocation + new Vector3(Mathf.Cos(angle) * Constants.SPAWN_SECTOR_SIZE * 2, 0,
                        Mathf.Sin(angle) * Constants.SPAWN_SECTOR_SIZE * 2));
            }
        }

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
            metrics.ReportSpawns(spawns);
    }

    /// <summary>
    ///   Does a single spawn with a spawner
    /// </summary>
    private int SpawnWithSpawner(Spawner spawnType, Vector3 location)
    {
        var spawns = 0;

        if (random.NextFloat() > spawnType.Denstity)
        {
            return spawns;
        }

        if (spawnType is CompoundCloudSpawner || estimateEntityCount < Constants.DEFAULT_MAX_SPAWNED_ENTITIES)
        {
            var enumerable = spawnType.Spawn(worldRoot, location);

            if (enumerable == null)
                return spawns;

            using var spawner = enumerable.GetEnumerator();
            while (spawner.MoveNext())
            {
                if (spawner.Current == null)
                    throw new NullReferenceException("spawn enumerator is not allowed to return null");

                ProcessSpawnedEntity(spawner.Current, spawnType);
                spawns++;
                estimateEntityCount++;
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

        // Despawn entities
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
                entitiesDeleted++;
                spawned.DestroyDetachAndQueueFree();

                if (entitiesDeleted >= Constants.MAX_DESPAWNS_PER_FRAME)
                    break;
            }
        }

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
            metrics.ReportDespawns(entitiesDeleted);

        return spawnedCount - entitiesDeleted;
    }

    /// <summary>
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity, Spawner spawnType)
    {
        entity.DespawnRadiusSquared = spawnType.SpawnRadiusSquared + Constants.DESPAWN_RADIUS_OFFSET_SQUARED;

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

    private class QueuedSpawn
    {
        public Spawner SpawnType;
        public IEnumerator<ISpawned> Spawns;

        public QueuedSpawn(IEnumerator<ISpawned> spawner, Spawner spawnType)
        {
            Spawns = spawner;
            SpawnType = spawnType;
        }
    }
}
