using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    /// <summary>
    ///   Sets how often the spawn system runs and checks things
    /// </summary>
    private float interval = 1.0f;

    private float elapsed = 0.0f;

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    private List<ISpawner> spawnTypes = new List<ISpawner>();

    private Vector3 previousPlayerPosition = new Vector3(0, 0, 0);

    private Random random = new Random();

    /// <summary>
    ///   Delete a max of this many entities per step to reduce lag
    ///   from deleting tons of entities at once.
    /// </summary>
    private int maxEntitiesToDeletePerStep = 2;

    /// <summary>
    ///   This limits the total number of things that can be spawned.
    /// </summary>
    private int maxAliveEntities = 1000;

    /// <summary>
    ///   Max tries per spawner to avoid very high spawn densities lagging
    /// </summary>
    private int maxTriesPerSpawner = 500;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

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
    public void AddSpawnType(ISpawner spawner, float spawnDensity, int spawnRadius)
    {
        spawner.SpawnRadius = spawnRadius;
        spawner.SpawnFrequency = 122;
        spawner.SpawnRadiusSqr = spawnRadius * spawnRadius;
        spawner.SetFrequencyFromDensity(spawnDensity);
        spawnTypes.Add(spawner);
    }

    /// <summary>
    ///   Removes a spawn type immediately. Note that it's easier to
    ///   just set DestroyQueued to true on an spawner.
    /// </summary>
    public void RemoveSpawnType(ISpawner spawner)
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
        previousPlayerPosition = new Vector3(0, 0, 0);
        elapsed = 0;
    }

    /// <summary>
    ///   Despawns all spawned entities
    /// </summary>
    public void DespawnAll()
    {
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
    public void Process(float delta, Vector3 playerPosition)
    {
        elapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        // This is now an if to make sure that the spawn system is
        // only ran once per frame to avoid spawning a bunch of stuff
        // all at once after a lag spike
        // NOTE: that as QueueFree is used it's not safe to just switch this to a loop
        if (elapsed >= interval)
        {
            elapsed -= interval;

            int existing = DespawnEntities(playerPosition);

            SpawnEntities(playerPosition, existing);

            previousPlayerPosition = playerPosition;
        }
    }

    private void SpawnEntities(Vector3 playerPosition, int existing)
    {
        // If  there are already too many entities, don't spawn more
        if (existing >= maxAliveEntities)
            return;

        int spawned = 0;

        foreach (ISpawner spawnType in spawnTypes)
        {
            /*
            To actually spawn a given entity for a given attempt, two
            conditions should be met. The first condition is a random
            chance that adjusts the spawn frequency to the approprate
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

                    A random location in the square of sidelength 2*spawnRadius
                    centered on the player is chosen. The corners
                    of the square are outside the spawning region, but they
                    will fail the second condition, so entities still only
                    spawn within the spawning region.
                    */
                    float distanceX = (float)random.NextDouble() * spawnType.SpawnRadius -
                        (float)random.NextDouble() * spawnType.SpawnRadius;
                    float distanceZ = (float)random.NextDouble() * spawnType.SpawnRadius -
                        (float)random.NextDouble() * spawnType.SpawnRadius;

                    // Distance from the player.
                    Vector3 displacement = new Vector3(distanceX, 0, distanceZ);
                    float squaredDistance = displacement.LengthSquared();

                    // Distance from the location of the player in the previous
                    // spawn cycle.
                    Vector3 previousDisplacement = displacement + playerPosition -
                        previousPlayerPosition;
                    float previousSquaredDistance = previousDisplacement.LengthSquared();

                    if (squaredDistance <= spawnType.SpawnRadiusSqr &&
                        previousSquaredDistance > spawnType.SpawnRadiusSqr)
                    {
                        // Second condition passed. Spawn the entity.
                        var entities = spawnType.Spawn(worldRoot,
                            playerPosition + displacement);

                        // Add the entity to the spawned group and add the despawn radius
                        if (entities != null)
                        {
                            foreach (var entity in entities)
                            {
                                // TODO: I don't understand why the same
                                // value is used for spawning and
                                // despawning, but apparently it works
                                // just fine
                                entity.DespawnRadiusSqr = spawnType.SpawnRadiusSqr;

                                entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
                            }

                            spawned += entities.Count;

                            // TODO: this is a bit awkward if this
                            // stops compound clouds from spawning as
                            // well...
                            if (spawned + existing >= maxAliveEntities)
                                return;
                        }
                    }
                }
            }
        }
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
}
