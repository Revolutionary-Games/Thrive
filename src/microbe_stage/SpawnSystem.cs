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
    private float elapsed = 0;

    [JsonProperty]
    private float spawnItemTimer;

    private CompoundCloudSpawner cloudSpawner;
    private ChunkSpawner chunkSpawner;
    private MicrobeSpawner microbeSpawner;

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    // Used for a Tetris style random bag. Fill and shuffle the bag,
    // then simply pop one out until empty. Rinse and repeat.
    [JsonProperty]
    private List<SpawnItem> spawnItemBag = new List<SpawnItem>();

    [JsonProperty]
    private int spawnBagSize;

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
    ///   Estimate count of existing spawned entities, cached to make delayed spawns cheaper
    /// </summary>
    private int estimateEntityCount;

    public SpawnSystem(Node root, CompoundCloudSystem cloudSystem,
        GameProperties currentGame, int spawnRadius)
    {
        worldRoot = root;
        cloudSpawner = new CompoundCloudSpawner(cloudSystem, spawnRadius);
        chunkSpawner = new ChunkSpawner(cloudSystem, spawnRadius);
        microbeSpawner = new MicrobeSpawner(cloudSystem, currentGame, spawnRadius);
    }

    // Needs no params constructor for loading saves?

    /// <summary>
    ///   Adds an externally spawned entity to be despawned
    /// </summary>
    public static void AddEntityToTrack(ISpawned entity,
        float radius = Constants.MICROBE_SPAWN_RADIUS)
    {
        entity.DespawnRadius = (int)radius;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    public void FillSpawnItemBag()
    {
        GD.Print("Filling the Cloud Bag");
        spawnItemBag.Clear();

        // Fill compound cloud items
        foreach (Compound key in cloudSpawner.GetCompounds())
        {
            // If a compound is set to N%, add N CloudItems to the SpawnItemBag
            // At most this will add 100 to SpawnItemBag.
            int cloudCount = cloudSpawner.GetCloudItemCount(key);
            GD.Print("Adding " + cloudCount + " of " + key.Name);
            for (int i = 0; i < Math.Min(cloudCount, 100); i++)
            {
                spawnItemBag.Add(new CloudItem(cloudSpawner, key, cloudSpawner.GetCloudAmount(key)));
            }
        }

        // Fill chunk items

        // Fill microbe items

        shuffleSpawnItemBag();
    }

    void shuffleSpawnItemBag()
    {
 // Fisher–Yates Shuffle Alg. Perfectly Random shuffle, O(n)
        for (int i = 0; i < spawnItemBag.Count - 2; i++)
        {
            int j = random.Next(i, spawnItemBag.Count);

            // swap i, j
            SpawnItem swap = spawnItemBag[i];

            spawnItemBag[i] = spawnItemBag[j];
            spawnItemBag[j] = swap;
        }
        spawnBagSize = spawnItemBag.Count;
        spawnItemTimer = Constants.CLOUD_SPAWN_TIME / spawnBagSize;
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
                entity.DetachAndQueueFree();
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

        SpawnClouds(playerPosition, playerRotation, delta);

    }

    public void NewPatchSpawn(Vector3 playerPosition)
    {
        for (int i = 0; i < Constants.FREE_CLOUDS_IN_NEW_PATCH; i++)
        {
            float displacementDistance = random.NextFloat() * cloudSpawner.MinSpawnRadius;
            float displacementRotation = NormalToWithNegativesRadians(random.NextFloat() * 2 * (float)Math.PI);

            Vector3 displacement = GetDisplacementVector(displacementRotation, displacementDistance);
            SpawnItem spawnItem = SpawnItemBagPop();

            //do spawns here
        }
    }

    private SpawnItem SpawnItemBagPop()
    {
        if (spawnItemBag.Count == 0)
            FillSpawnItemBag();

        SpawnItem pop = spawnItemBag[0];
        spawnItemBag.RemoveAt(0);

        return pop;
    }

    // Takes a random rotation and distance, turns into a vector
    private Vector3 GetDisplacementVector(float displacementRotation, float displacementDistance)
    {
        float distanceX = Mathf.Sin(displacementRotation) * displacementDistance;
        float distanceZ = Mathf.Cos(displacementRotation) * displacementDistance;

        // Distance from the player.
        Vector3 displacement = new Vector3(distanceX, 0, distanceZ);
        return displacement;
    }

      private void SpawnClouds(Vector3 playerPosition, Vector3 playerRotation, float delta)
    {
        spawnItemTimer -= delta;
        if (spawnItemTimer <= 0)
        {
            Spawn(playerPosition, playerRotation);
            spawnItemTimer = Constants.CLOUD_SPAWN_TIME / spawnBagSize;
        }
    }

    private void Spawn(Vector3 playerPosition, Vector3 playerRotation)
    {
        SpawnItem spawn = SpawnItemBagPop();

        if(spawn is CloudItem)
        {
             CloudItem cloudSpawn = (CloudItem)spawn;

            float minRadius = cloudSpawner.MinSpawnRadius;
            float maxRadius = cloudSpawner.SpawnRadius;

            float displacementDistance = random.NextFloat() * (maxRadius - minRadius) + minRadius;
            float displacementRotation = WeightedRandomRotation(playerRotation.y);

            Vector3 displacement = GetDisplacementVector(displacementRotation, displacementDistance);

            Compound compound = cloudSpawn.GetCompound();

            cloudSpawner.SpawnCloud(playerPosition + displacement,
                compound, cloudSpawner.GetCloudAmount(compound));
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
            var distance = (playerPosition - entityPosition).Length();

            // If the entity is too far away from the player, despawn it.
            if (distance > spawned.DespawnRadius)
            {
                entitiesDeleted++;
                entity.DetachAndQueueFree();

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
        entity.DespawnRadius = spawnType.SpawnRadius;

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

    public void ClearCloudSpawner()
    {
        cloudSpawner.ClearBiomeCompounds();
    }

    public void ClearChunkSpawner()
    {

    }

    public void ClearMicrobeSpawner()
    {

    }

    public void AddBiomeCompound(Compound compound, int percent, float amount)
    {
        cloudSpawner.AddBiomeCompound(compound, percent, amount);
    }
}
