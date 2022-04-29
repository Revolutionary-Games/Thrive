﻿using System;
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
    ///   This limits the number of things that can be spawned in a single spawn radius.
    ///   Used to limit items spawning in a circle when the player doesn't move.
    /// </summary>
    [JsonProperty]
    private int maxEntitiesInSpawnRadius = 15;

    /// <summary>
    ///   Max tries per spawner to avoid very high spawn densities lagging
    /// </summary>
    [JsonProperty]
    private int maxTriesPerSpawner = 500;

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

    /// <summary>
    ///   Estimate count of existing spawn entities within the current spawn radius of the player;
    ///   Used to prevent a "spawn belt" of densely spawned entities when player doesn't move.
    /// </summary>
    [JsonProperty]
    private int estimateEntityCountInSpawnRadius;

    /// <summary>
    ///   Last recorded position of the player. Positions are recorded upon moving more than the stationary threshold.
    /// </summary>
    [JsonProperty]
    private Vector3 lastRecordedPlayerPosition = Vector3.Zero;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
        spawnTypes = new ShuffleBag<Spawner>(random);
    }

    // Needs no params constructor for loading saves?

    /// <summary>
    ///   Adds an externally spawned entity to be despawned
    /// </summary>
    public static void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadiusSquared = Constants.MICROBE_DESPAWN_RADIUS_SQUARED;
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
        spawner.SpawnFrequency = 122;
        spawner.SpawnRadiusSquared = spawnRadius * spawnRadius;

        float minSpawnRadius = spawnRadius * Constants.MIN_SPAWN_RADIUS_RATIO;
        spawner.MinSpawnRadiusSquared = minSpawnRadius * minSpawnRadius;

        spawner.SetFrequencyFromDensity(spawnDensity);
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

        int spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

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

            SpawnEntities(playerPosition, playerRotation, estimateEntityCount, spawnsLeftThisFrame);
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

    private void SpawnEntities(Vector3 playerPosition, Vector3 playerRotation, int existing, int spawnsLeftThisFrame)
    {
        // If there are already too many entities, don't spawn more
        if (existing >= Constants.DEFAULT_MAX_SPAWNED_ENTITIES)
            return;

        // Here we want to check that the player moved to not basically spawn in circle around the player.
        // Solution inspired by gwen is to check if the player moves out of a square/cycle around their current
        // registered position (note that the cloud system also used to work like this -hhyyrylainen).
        // Not perfect however as going on and off could still break this.
        float squaredDistanceToLastPosition = (playerPosition - lastRecordedPlayerPosition).LengthSquared();
        bool immobilePlayer = squaredDistanceToLastPosition < Constants.PLAYER_IMMOBILITY_ZONE_RADIUS_SQUARED;

        if (immobilePlayer)
        {
            // If the player is staying inside a circle around their previous position,
            // only spawn up to the local spawn cap
            if (estimateEntityCountInSpawnRadius > maxEntitiesInSpawnRadius)
                return;
        }
        else
        {
            // The player moved, so let's update their position and reset counts in spawn radius
            lastRecordedPlayerPosition = playerPosition;
            estimateEntityCountInSpawnRadius = 0;
        }

        int spawned = 0;

        foreach (var spawnType in spawnTypes)
        {
            /*
            To actually spawn a given entity for a given attempt, two
            conditions should be met. The first condition is a random
            chance that adjusts the spawn frequency to the appropriate
            amount. The second condition is whether the entity will
            spawn in a valid position. It is checked when the first
            condition is met and a position for the entity has been
            decided.

            To allow more than one entity of each type to spawn per
            spawn cycle, the SpawnSystem attempts to spawn each given
            entity multiple times depending on the spawnFrequency.
            numAttempts stores how many times the SpawnSystem attempts
            to spawn the given entity.
            */
            int numAttempts = Mathf.Clamp(spawnType!.SpawnFrequency * 2, 1, maxTriesPerSpawner);

            for (int i = 0; i < numAttempts; i++)
            {
                if (random.Next(0, numAttempts + 1) < spawnType.SpawnFrequency)
                {
                    /*
                    First condition passed. Choose a location for the entity.

                    A random location in the square of side length 2*spawnRadius
                    centered on the player is chosen. The corners
                    of the square are outside the spawning region, but they
                    will fail the second condition, so entities still only
                    spawn within the spawning region.
                    */
                    float displacementDistance = random.NextFloat() * spawnType.SpawnRadius;

                    // If the player moves, weight the rotation to be in front of him for encounter.
                    // Else compute a uniform rotation to avoid clustering
                    float displacementRotation = ComputeRandomRadianRotation(playerRotation.y, !immobilePlayer);

                    float distanceX = Mathf.Sin(displacementRotation) * displacementDistance;
                    float distanceZ = Mathf.Cos(displacementRotation) * displacementDistance;

                    // Distance from the player.
                    Vector3 displacement = new Vector3(distanceX, 0, distanceZ);
                    float squaredDistance = displacement.LengthSquared();

                    if (squaredDistance <= spawnType.SpawnRadiusSquared &&
                        squaredDistance >= spawnType.MinSpawnRadiusSquared)
                    {
                        // Second condition passed. Spawn the entity.
                        if (SpawnWithSpawner(spawnType, playerPosition + displacement, playerPosition, existing,
                                ref spawnsLeftThisFrame, ref spawned))
                        {
                            estimateEntityCountInSpawnRadius += spawned;

                            return;
                        }
                    }
                }
            }
        }

        estimateEntityCountInSpawnRadius += spawned;

        var metrics = PerformanceMetrics.Instance;

        if (metrics.Visible)
            metrics.ReportSpawns(spawned);
    }

    /// <summary>
    ///   Does a single spawn with a spawner
    /// </summary>
    /// <returns>True if we have exceeded the spawn limit and no further spawns should be done this frame</returns>
    private bool SpawnWithSpawner(Spawner spawnType, Vector3 location, Vector3 playerPosition, int existing,
        ref int spawnsLeftThisFrame, ref int spawned)
    {
        var enumerable = spawnType.Spawn(worldRoot, location, playerPosition);

        if (enumerable == null)
            return false;

        var spawner = enumerable.GetEnumerator();

        while (spawner.MoveNext())
        {
            if (spawner.Current == null)
                throw new NullReferenceException("spawn enumerator is not allowed to return null");

            // Spawned something
            ProcessSpawnedEntity(spawner.Current, spawnType);
            spawned += 1;
            --spawnsLeftThisFrame;

            // Check if we are out of quota for this frame

            // TODO: this is a bit awkward if this stops compound clouds from spawning as well...
            if (spawned + existing >= Constants.DEFAULT_MAX_SPAWNED_ENTITIES)
            {
                // We likely couldn't spawn things next frame anyway if we are at the entity limit,
                // so the spawner is not stored here
                return true;
            }

            if (spawnsLeftThisFrame <= 0)
            {
                // This spawner might still have something left to spawn next frame, so store it
                queuedSpawns = new QueuedSpawn(spawner, spawnType);
                return true;
            }
        }

        // Can still spawn more stuff
        return false;
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
