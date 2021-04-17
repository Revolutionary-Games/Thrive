using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    [JsonProperty]
    private float elapsed;

    [JsonProperty]
    private float spawnItemTimer;

    [JsonIgnore]
    private CompoundCloudSpawner cloudSpawner;

    [JsonIgnore]
    private ChunkSpawner chunkSpawner;

    [JsonIgnore]
    private MicrobeSpawner microbeSpawner;

    /// <summary>
    ///   Root node to parent all spawned things to
    /// </summary>
    private Node worldRoot;

    // List of SpawnItems, used to fill the spawnItemBag when it is empty.
    [JsonIgnore]
    private List<SpawnItem> spawnItems = new List<SpawnItem>();

    // Used for a Tetris style random bag. Fill and shuffle the bag,
    // then simply pop one out until empty. Rinse and repeat.
    [JsonIgnore]
    private List<SpawnItem> spawnItemBag = new List<SpawnItem>();

    // Queue of spawn items to spawn. a few from this list get spawned every frame.
    [JsonIgnore]
    private Queue<SpawnItem> itemsToSpawn = new Queue<SpawnItem>();

    [JsonIgnore]
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

    private Dictionary<Vector3, bool> spawnedGrid = new Dictionary<Vector3, bool>();

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    public static void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadius = Constants.SPAWN_ITEM_RADIUS;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    public void Init(CompoundCloudSystem cloudSystem)
    {
        cloudSpawner = new CompoundCloudSpawner(cloudSystem);
        chunkSpawner = new ChunkSpawner(cloudSystem);
        microbeSpawner = new MicrobeSpawner(cloudSystem);
    }

    public void SetCurrentGame(GameProperties currentGame)
    {
        microbeSpawner.SetCurrentGame(currentGame);
    }

    public void AddSpawnItem(SpawnItem spawnItem)
    {
        spawnItems.Add(spawnItem);
    }

    public void ClearSpawnItems()
    {
        spawnItems.Clear();
    }

    public void ClearSpawnBag()
    {
        spawnItemBag.Clear();
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

        SpawnItems(playerPosition, playerRotation, delta);
        SpawnItemsInSpawnList();
    }

    // Takes SpawnItems list and shuffles them in bag.
    private void FillSpawnItemBag()
    {
        spawnItemBag.Clear();

        foreach (SpawnItem spawnItem in spawnItems)
        {
            spawnItemBag.Add(spawnItem);
        }

        ShuffleSpawnItemBag();
    }

    // Fisher–Yates Shuffle Alg. Perfectly Random shuffle, O(n)
    private void ShuffleSpawnItemBag()
    {
        for (int i = 0; i < spawnItemBag.Count - 2; i++)
        {
            int j = random.Next(i, spawnItemBag.Count);

            // swap i, j
            SpawnItem swap = spawnItemBag[i];

            spawnItemBag[i] = spawnItemBag[j];
            spawnItemBag[j] = swap;
        }

        spawnBagSize = spawnItemBag.Count;
        spawnItemTimer = Constants.SPAWN_BAG_RATE / spawnBagSize;
    }

    private SpawnItem SpawnItemBagPop()
    {
        if (spawnItemBag.Count == 0)
            FillSpawnItemBag();

        SpawnItem pop = spawnItemBag[0];
        spawnItemBag.RemoveAt(0);

        if (pop is CloudItem)
        {
            ((CloudItem)pop).SetCloudSpawner(cloudSpawner);
        }

        if (pop is ChunkItem)
        {
            ((ChunkItem)pop).SetChunkSpawner(chunkSpawner, worldRoot);
        }

        if (pop is MicrobeItem)
        {
            ((MicrobeItem)pop).SetMicrobeSpawner(microbeSpawner, worldRoot);
        }

        return pop;
    }

    private void SpawnItems(Vector3 playerPosition, Vector3 playerRotation, float delta)
    {
        int playerGridX = (int)playerPosition.x / Constants.SPAWN_GRID_SIZE;
        int playerGridZ = (int)playerPosition.z / Constants.SPAWN_GRID_SIZE;

        for (int i = -Constants.SPAWN_GRID_WIDTH; i <= Constants.SPAWN_GRID_WIDTH; i++)
        {
            for (int j = -Constants.SPAWN_GRID_WIDTH; j <= Constants.SPAWN_GRID_WIDTH; j++)
            {
                int spawnGridX = playerGridX + i;
                int spawnGridZ = playerGridZ + j;

                // Make Random
                float spawnEventPosX = spawnGridX * Constants.SPAWN_GRID_SIZE;
                float spawnEventPosZ = spawnGridZ * Constants.SPAWN_GRID_SIZE;

                Vector3 spawnEvent = new Vector3(spawnEventPosX, 0, spawnEventPosZ);

                if (!spawnedGrid.ContainsKey(spawnEvent) || !spawnedGrid[spawnEvent])
                {
                    SpawnEvent(spawnEvent, playerPosition);

                    spawnedGrid[spawnEvent] = true;
                }
            }
        }
    }

    private void SpawnEvent(Vector3 spawnGridPos, Vector3 playerPosition)
    {
        // Choose random place to spawn
        Vector3 spawnEventCenter = spawnGridPos +
            new Vector3(random.NextFloat() * Constants.SPAWN_GRID_SIZE - Constants.SPAWN_GRID_HALFSIZE, 0,
            (float)random.NextFloat() * Constants.SPAWN_GRID_SIZE - Constants.SPAWN_GRID_HALFSIZE);

        DespawnEntities(playerPosition);

        for (int i = 0; i < random.Next(Constants.SPAWN_EVENT_MIN, Constants.SPAWN_EVENT_MAX); i++)
        {
            float spawnRadius = (random.NextFloat() + 0.1f) * Constants.SPAWN_EVENT_RADIUS;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnModPosition = new Vector3(spawnRadius * Mathf.Sin(spawnAngle),
                0, spawnRadius * Mathf.Cos(spawnAngle));

            AddSpawnItemInToSpawnList(spawnEventCenter + spawnModPosition);
        }
    }

    private void AddSpawnItemInToSpawnList(Vector3 spawnPos)
    {
        SpawnItem spawn = SpawnItemBagPop();

        // If there are too many entities, do not add more to spawn list.
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);
        if (spawnedEntities.Count >= maxAliveEntities)
            return;

        spawn.SetSpawnPosition(spawnPos);

        itemsToSpawn.Enqueue(spawn);
    }

    private void SpawnItemsInSpawnList()
    {
        for (int i = 0; i < Constants.MAX_SPAWNS_PER_FRAME; i++)
        {
            if (itemsToSpawn.Count > 0)
            {
                SpawnItem spawn = itemsToSpawn.Dequeue();

                List<ISpawned> spawnedList = spawn.Spawn();
                if (spawnedList != null)
                {
                    foreach (ISpawned spawned in spawnedList)
                    {
                        ProcessSpawnedEntity(spawned);
                    }
                }
            }
        }
    }

    /// <summary>
    ///   Despawns entities that are far away from the player
    /// </summary>
    private void DespawnEntities(Vector3 playerPosition)
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

            int playerGridX = (int)playerPosition.x / Constants.SPAWN_GRID_SIZE;
            int playerGridZ = (int)playerPosition.z / Constants.SPAWN_GRID_SIZE;

            float minSpawnX = (playerGridX - Constants.SPAWN_GRID_WIDTH) * Constants.SPAWN_GRID_SIZE - Constants.SPAWN_EVENT_RADIUS;
            float maxSpawnX = (playerGridX + Constants.SPAWN_GRID_WIDTH + 1) * Constants.SPAWN_GRID_SIZE + Constants.SPAWN_EVENT_RADIUS;
            float minSpawnZ = (playerGridZ - Constants.SPAWN_GRID_WIDTH) * Constants.SPAWN_GRID_SIZE - Constants.SPAWN_EVENT_RADIUS;
            float maxSpawnZ = (playerGridZ + Constants.SPAWN_GRID_WIDTH + 1) * Constants.SPAWN_GRID_SIZE + Constants.SPAWN_EVENT_RADIUS;

            // If the entity is too far away from the player, despawn it.
            if (entityPosition.x < minSpawnX || entityPosition.x > maxSpawnX ||
                entityPosition.z < minSpawnZ || entityPosition.z > maxSpawnZ)
            {
                entitiesDeleted++;
                entity.DetachAndQueueFree();

                if (entitiesDeleted >= maxEntitiesToDeletePerStep)
                    break;
            }
        }
    }

    /// <summary>IT
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity)
    {
        // I don't understand why the same
        // value is used for spawning and
        // despawning, but apparently it works
        // just fine
        entity.DespawnRadius = Constants.SPAWN_ITEM_RADIUS;

        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    private float NormalToWithNegativesRadians(float radian)
    {
        return radian <= Math.PI ? radian : radian - (float)(2 * Math.PI);
    }
}
