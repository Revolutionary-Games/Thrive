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

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    private List<Spawner> spawnTypes = new List<Spawner>();

    [JsonProperty]
    private Random random = new Random();

    /// <summary>
    ///   Delete a max of this many entities per step to reduce lag
    ///   from deleting tons of entities at once.
    /// </summary>
    [JsonProperty]
    private int maxEntitiesToDeletePerStep = Constants.MAX_DESPAWNS_PER_FRAME;

    /// <summary>
    ///   This limits the total number of things that can be spawned.
    /// </summary>
    [JsonProperty]
    private int maxAliveEntities = 1000;

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
    private QueuedSpawn queuedSpawns;

    /// <summary>
    ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
    /// </summary>
    private int estimateEntityCount;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    // Needs no params constructor for loading saves?

    /// <summary>
    ///   Adds an externally spawned entity to be despawned
    /// </summary>
    public static void AddEntityToTrack(ISpawned entity,
        float radius = Constants.MICROBE_SPAWN_RADIUS)
    {
        entity.DespawnRadiusSqr = (int)(radius * radius);
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    /// <summary>
    ///   Adds a new spawner. Sets up the spawn radius, radius sqr,
    ///   and frequency fields based on the parameters of this
    ///   function.
    /// </summary>
    public void AddSpawnType(Spawner spawner, float spawnDensity, int spawnRadius)
    {
        spawner.SpawnRadius = spawnRadius;
        spawner.SpawnFrequency = 122;
        spawner.SpawnRadiusSqr = spawnRadius * spawnRadius;

        float minSpawnRadius = spawnRadius * Constants.MIN_SPAWN_RADIUS_RATIO;
        spawner.MinSpawnRadiusSqr = minSpawnRadius * minSpawnRadius;

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
    }

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll()
    {
        queuedSpawns = null;
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            if (!entity.IsQueuedForDeletion())
                entity.QueueFree();
        }
    }

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta, Vector3 playerPosition, Vector3 playerRotation)
    {
        elapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        int spawnsLeftThisFrame = Constants.MAX_SPAWNS_PER_FRAME;

        // If we have queued spawns to do spawn those
        if (queuedSpawns != null)
        {
            spawnsLeftThisFrame = HandleQueuedSpawns(spawnsLeftThisFrame);

            if (spawnsLeftThisFrame <= 0)
                return;
        }

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
    }

    private int HandleQueuedSpawns(int spawnsLeftThisFrame)
    {
        // If we don't have room, just abandon spawning
        if (estimateEntityCount >= maxAliveEntities)
        {
            queuedSpawns.Spawns.Dispose();
            queuedSpawns = null;
            return spawnsLeftThisFrame;
        }

        // Spawn from the queue
        while (estimateEntityCount < maxAliveEntities && spawnsLeftThisFrame > 0)
        {
            if (!queuedSpawns.Spawns.MoveNext())
            {
                // Ended
                queuedSpawns.Spawns.Dispose();
                queuedSpawns = null;
                break;
            }

            // Next was spawned
            ProcessSpawnedEntity(queuedSpawns.Spawns.Current, queuedSpawns.SpawnType);

            ++estimateEntityCount;
            --spawnsLeftThisFrame;
        }

        return spawnsLeftThisFrame;
    }

    private void SpawnEntities(Vector3 playerPosition, Vector3 playerRotation, int existing, int spawnsLeftThisFrame)
    {
        // If  there are already too many entities, don't spawn more
        if (existing >= maxAliveEntities)
            return;

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
            int numAttempts = Math.Min(Math.Max(spawnType.SpawnFrequency * 2, 1),
                maxTriesPerSpawner);

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
                    float displacementRotation = WeightedRandomRotation(playerRotation.y);

                    float distanceX = Mathf.Sin(displacementRotation) * displacementDistance;
                    float distanceZ = Mathf.Cos(displacementRotation) * displacementDistance;

                    // Distance from the player.
                    Vector3 displacement = new Vector3(distanceX, 0, distanceZ);
                    float squaredDistance = displacement.LengthSquared();

                    if (squaredDistance <= spawnType.SpawnRadiusSqr &&
                        squaredDistance >= spawnType.MinSpawnRadiusSqr)
                    {
                        // Second condition passed. Spawn the entity.
                        if (SpawnWithSpawner(spawnType, playerPosition + displacement, existing,
                            ref spawnsLeftThisFrame, ref spawned))
                        {
                            return;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Does a single spawn with a spawner
    /// </summary>
    /// <returns>True if we have exceeded the spawn limit and no further spawns should be done this frame</returns>
    private bool SpawnWithSpawner(Spawner spawnType, Vector3 location, int existing, ref int spawnsLeftThisFrame,
        ref int spawned)
    {
        var enumerable = spawnType.Spawn(worldRoot, location);

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

            // TODO: this is a bit awkward if this
            // stops compound clouds from spawning as
            // well...
            if (spawned + existing >= maxAliveEntities)
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
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);

        foreach (Node entity in spawnedEntities)
        {
            var spawned = entity as ISpawned;

            if (spawned == null)
            {
                GD.PrintErr("A node has been put in the spawned group " +
                    "but it isn't derived from ISpawned");
                continue;
            }

            var entityPosition = ((Spatial)entity).Translation;
            var squaredDistance = (playerPosition - entityPosition).LengthSquared();

            // If the entity is too far away from the player, despawn it.
            if (squaredDistance > spawned.DespawnRadiusSqr)
            {
                entitiesDeleted++;
                entity.QueueFree();

                if (entitiesDeleted >= maxEntitiesToDeletePerStep)
                    break;
            }
        }

        return spawnedEntities.Count - entitiesDeleted;
    }

    /// <summary>
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity, Spawner spawnType)
    {
        // I don't understand why the same
        // value is used for spawning and
        // despawning, but apparently it works
        // just fine
        entity.DespawnRadiusSqr = spawnType.SpawnRadiusSqr;

        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    /// <summary>
    ///   Returns a random rotation (in radians)
    ///   It is more likely to return a rotation closer to the target rotation than not
    /// </summary>
    private float WeightedRandomRotation(float targetRotation)
    {
        targetRotation = WithNegativesToNormalRadians(targetRotation);

        float rotation1 = random.NextFloat() * 2 * Mathf.Pi;
        float rotation2 = random.NextFloat() * 2 * Mathf.Pi;

        if (DistanceBetweenRadians(rotation1, targetRotation) < DistanceBetweenRadians(rotation2, targetRotation))
            return NormalToWithNegativesRadians(rotation1);

        return NormalToWithNegativesRadians(rotation2);
    }

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
