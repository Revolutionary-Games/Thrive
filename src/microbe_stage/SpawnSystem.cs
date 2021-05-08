using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Spawns AI cells and other environmental things as the player moves around
/// </summary>
public class SpawnSystem
{
    [JsonProperty]
    private int spawnEventCount;

    [JsonProperty]
    private int spawnGridSize;

    [JsonProperty]
    private int spawnEventRadius;

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

    [JsonProperty]
    private float spawnPatchMultiplier;

    [JsonProperty]
    private float eventDistanceFromPlayerSqr;

    [JsonProperty]
    private float elapsed;

    [JsonProperty]
    private float spawnWandererTimer;

    [JsonProperty]
    private Random random = new Random();

    [JsonIgnore]
    private int microbeBagSize;

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

    // List of MicrobeItems, used to fill the wanderMicrobeBag.
    [JsonIgnore]
    private List<SpawnItem> microbeItems = new List<SpawnItem>();

    // Used to spawn wandering random microbes when sitting still.
    [JsonIgnore]
    private List<SpawnItem> wanderMicrobeBag = new List<SpawnItem>();

    // Queue of spawn items to spawn. a few from this list get spawned every frame.
    [JsonIgnore]
    private Queue<SpawnItem> itemsToSpawn = new Queue<SpawnItem>();

    [JsonProperty]
    private Dictionary<IVector3, SpawnEvent> spawnGrid = new Dictionary<IVector3, SpawnEvent>();

    [JsonProperty]
    private IVector3 oldPlayerGrid;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    public static void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    public static AgentProjectile SpawnAgent(AgentProperties properties, float amount,
        float lifetime, Vector3 location, Vector3 direction,
        Node worldRoot, PackedScene agentScene, Node emitter)
    {
        var normalizedDirection = direction.Normalized();

        var agent = (AgentProjectile)agentScene.Instance();
        agent.Properties = properties;
        agent.Amount = amount;
        agent.TimeToLiveRemaining = lifetime;
        agent.Emitter = emitter;

        worldRoot.AddChild(agent);
        agent.Translation = location + (direction * 1.5f);

        agent.ApplyCentralImpulse(normalizedDirection *
            Constants.AGENT_EMISSION_IMPULSE_STRENGTH);

        agent.AddToGroup(Constants.TIMED_GROUP);
        return agent;
    }

    public void AddSpawnItem(SpawnItem spawnItem)
    {
        spawnItems.Add(spawnItem);
    }

    public void AddMicrobeItem(MicrobeItem microbeItem)
    {
        microbeItems.Add(microbeItem);
    }

    public void SetMicrobeBagSize()
    {
        microbeBagSize = microbeItems.Count;
        spawnWandererTimer = Constants.WANDERER_SPAWN_RATE / microbeBagSize;
    }

    public void SetSpawnData(int spawnEventCount, int spawnGridSize, float spawnPatchMultiplier)
    {
        this.spawnEventCount = spawnEventCount;
        this.spawnPatchMultiplier = spawnPatchMultiplier;
        this.spawnGridSize = spawnGridSize;
        spawnEventRadius = spawnGridSize / 4;

        eventDistanceFromPlayerSqr = (spawnEventRadius + Constants.DESPAWN_ITEM_RADIUS + 20)
            * (spawnEventRadius + Constants.DESPAWN_ITEM_RADIUS + 20);
    }

    // Adds this spot to SpawnedGrid, so that respawning is less likely to give spawn event.
    public void RespawningPlayer()
    {
        oldPlayerGrid = null;
        itemsToSpawn.Clear();
        spawnGrid.Clear();
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

        spawnItems.Clear();
        spawnItemBag.Clear();
        microbeItems.Clear();
        wanderMicrobeBag.Clear();
        itemsToSpawn.Clear();
        spawnGrid.Clear();

        oldPlayerGrid = null;
    }

    /// <summary>
    ///   Processes spawning and despawning things
    /// </summary>
    public void Process(float delta, Vector3 playerPosition)
    {
        elapsed += delta;

        // Remove the y-position from player position
        playerPosition.y = 0;

        SpawnEventGrid(playerPosition);
        SpawnWanderingMicrobes(playerPosition);
        SpawnItemsInSpawnList();
    }

    // Takes SpawnItems list and shuffles them in bag.
    private void FillBag(List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        bag.Clear();

        foreach (SpawnItem spawnItem in fullBag)
        {
            bag.Add(spawnItem);
        }

        ShuffleBag(bag);
    }

    // Fisher–Yates Shuffle Alg. Perfectly Random shuffle, O(n)
    private void ShuffleBag(List<SpawnItem> bag)
    {
        for (int i = 0; i < bag.Count - 2; i++)
        {
            int j = random.Next(i, bag.Count);

            SpawnItem swap = bag[i];
            bag[i] = bag[j];
            bag[j] = swap;
        }
    }

    private SpawnItem BagPop(List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        if (bag.Count == 0)
            FillBag(bag, fullBag);

        SpawnItem pop = bag[0];
        bag.RemoveAt(0);

        switch (pop)
        {
            case ChunkItem chunkPop:
                chunkPop.WorldNode = worldRoot;
                break;

            case MicrobeItem microbePop:
                microbePop.WorldNode = worldRoot;
                break;
        }

        return pop;
    }

    private void SpawnEventGrid(Vector3 playerPosition)
    {
        int playerGridX = (int)playerPosition.x / spawnGridSize;
        int playerGridZ = (int)playerPosition.z / spawnGridSize;
        IVector3 playerCurrentGrid = new IVector3(playerGridX, 0, playerGridZ);

        if (oldPlayerGrid != playerCurrentGrid)
        {
            for (int i = -Constants.SPAWN_GRID_WIDTH; i <= Constants.SPAWN_GRID_WIDTH; i++)
            {
                for (int j = -Constants.SPAWN_GRID_WIDTH; j <= Constants.SPAWN_GRID_WIDTH; j++)
                {
                    int spawnGridX = playerGridX + i;
                    int spawnGridZ = playerGridZ + j;

                    IVector3 spawnEventGrid = new IVector3(spawnGridX, 0, spawnGridZ);

                    if (!spawnGrid.ContainsKey(spawnEventGrid))
                    {
                        Vector3 eventGamePos = new Vector3(spawnEventGrid.X * spawnGridSize,
                            spawnEventGrid.Y * spawnGridSize, spawnEventGrid.Z * spawnGridSize);
                        SpawnEvent spawnEvent = new SpawnEvent(GetRandomEventPosition(eventGamePos),
                            spawnEventGrid);

                        if (oldPlayerGrid == null &&
                            (spawnEvent.Position - playerPosition).LengthSquared() < eventDistanceFromPlayerSqr)
                        {
                            spawnEvent.IsSpawned = true;
                        }

                        spawnGrid[spawnEventGrid] = spawnEvent;
                        DespawnEntities(playerPosition);
                    }
                }
            }

            oldPlayerGrid = playerCurrentGrid;
        }
        else
        {
            List<IVector3> eventsToRemove = new List<IVector3>();
            foreach (IVector3 key in spawnGrid.Keys)
            {
                SpawnEvent spawnEvent = spawnGrid[key];
                if (spawnEvent.GridPos.X > playerGridX + Constants.SPAWN_GRID_WIDTH
                    || spawnEvent.GridPos.X < playerGridX - Constants.SPAWN_GRID_WIDTH
                    || spawnEvent.GridPos.Z > playerGridZ + Constants.SPAWN_GRID_WIDTH
                    || spawnEvent.GridPos.Z < playerGridZ - Constants.SPAWN_GRID_WIDTH)
                {
                    eventsToRemove.Add(key);
                }

                if (!spawnEvent.IsSpawned &&
                    (spawnEvent.Position - playerPosition).LengthSquared() < eventDistanceFromPlayerSqr)
                {
                    SpawnNewEvent(spawnEvent);
                }
            }

            foreach (IVector3 key in eventsToRemove)
            {
                spawnGrid.Remove(key);
            }
        }
    }

    private void SpawnWanderingMicrobes(Vector3 playerPosition)
    {
        if (elapsed > spawnWandererTimer)
        {
            elapsed = 0;
            float spawnDistance = Constants.SPAWN_WANDERER_RADIUS;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnWanderer = new Vector3(spawnDistance * Mathf.Sin(spawnAngle),
                0, spawnDistance * Mathf.Cos(spawnAngle));

            AddSpawnItemInToSpawnList(spawnWanderer + playerPosition, wanderMicrobeBag, microbeItems);
            DespawnEntities(playerPosition);
        }
    }

    private void SpawnNewEvent(SpawnEvent spawnEvent)
    {
        spawnEvent.IsSpawned = true;
        int spawnCount = (int)(spawnEventCount * spawnPatchMultiplier) + random.Next(-1, 2);

        for (int i = 0; i < spawnCount; i++)
        {
            float weightedRandom = Mathf.Clamp(random.NextFloat() * 0.9f + 0.1f, 0, 1);
            float spawnRadius = weightedRandom * spawnEventRadius;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnModPosition = new Vector3(spawnRadius * Mathf.Sin(spawnAngle),
                0, spawnRadius * Mathf.Cos(spawnAngle));

            AddSpawnItemInToSpawnList(spawnEvent.Position + spawnModPosition, spawnItemBag, spawnItems);
        }
    }

    private Vector3 GetRandomEventPosition(Vector3 spawnGridPos)
    {
        // Choose random place to spawn
        float eventCenterX = random.NextFloat() * (spawnGridSize - spawnEventRadius * 2)
            + spawnEventRadius;

        float eventCenterZ = random.NextFloat() * (spawnGridSize - spawnEventRadius * 2)
            + spawnEventRadius;

        Vector3 eventPos = new Vector3(eventCenterX, 0, eventCenterZ);

        return spawnGridPos + eventPos;
    }

    private void AddSpawnItemInToSpawnList(Vector3 spawnPos, List<SpawnItem> bag,
        List<SpawnItem> fullBag)
    {
        SpawnItem spawn = BagPop(bag, fullBag);

        // If there are too many entities, do not add more to spawn list.
        var spawnedEntities = worldRoot.GetTree().GetNodesInGroup(Constants.SPAWNED_GROUP);
        if (spawnedEntities.Count >= maxAliveEntities)
            return;

        spawn.Position = spawnPos;

        itemsToSpawn.Enqueue(spawn);
    }

    private void SpawnItemsInSpawnList()
    {
        for (int i = 0; i < Constants.MAX_SPAWNS_PER_FRAME; i++)
        {
            if (itemsToSpawn.Count > 0)
            {
                SpawnItem spawn = itemsToSpawn.Dequeue();

                if (spawn == null)
                {
                    break;
                }

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

            int playerGridX = (int)playerPosition.x / spawnGridSize;
            int playerGridZ = (int)playerPosition.z / spawnGridSize;

            float minSpawnX = (playerGridX - Constants.SPAWN_GRID_WIDTH) * spawnGridSize;
            float maxSpawnX = (playerGridX + Constants.SPAWN_GRID_WIDTH + 1) * spawnGridSize;
            float minSpawnZ = (playerGridZ - Constants.SPAWN_GRID_WIDTH) * spawnGridSize;
            float maxSpawnZ = (playerGridZ + Constants.SPAWN_GRID_WIDTH + 1) * spawnGridSize;

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

    /// <summary>
    ///   Add the entity to the spawned group and add the despawn radius
    /// </summary>
    private void ProcessSpawnedEntity(ISpawned entity)
    {
        // I don't understand why the same
        // value is used for spawning and
        // despawning, but apparently it works
        // just fine
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;

        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    private class SpawnEvent
    {
        public bool IsSpawned;

        public SpawnEvent(Vector3 position, IVector3 gridPos, bool isSpawned = false)
        {
            GridPos = gridPos;
            Position = position;
            IsSpawned = isSpawned;
        }

        public Vector3 Position { get; private set; }
        public IVector3 GridPos { get; private set; }
    }

    // Other classes should not use IVector3
    // The spawn grid needs an integer Vector3 to avoid floating point errors.
    [TypeConverter(typeof(IVector3TypeConverter))]
    private class IVector3
    {
        public int X;
        public int Y;
        public int Z;
        public IVector3() { }

        public IVector3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static bool operator ==(IVector3 iVecA, IVector3 iVecB)
        {
            if (ReferenceEquals(iVecA, iVecB))
            {
                return true;
            }

            // Ensure that "numberA" isn't null
            if (ReferenceEquals(null, iVecA))
            {
                return false;
            }

            return iVecA.Equals(iVecB);
        }

        public static bool operator !=(IVector3 iVecA, IVector3 iVecB)
        {
            return !(iVecA == iVecB);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVector3);
        }

        public bool Equals(IVector3 other)
        {
            return other != null &&
                X == other.X &&
                Y == other.Y &&
                Z == other.Z;
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const int HashingBase = (int)2166136261;
                const int HashingMultiplier = 16777619;

                int hash = HashingBase;
                hash = (hash * HashingMultiplier) ^ X.GetHashCode();
                hash = (hash * HashingMultiplier) ^ Y.GetHashCode();
                hash = (hash * HashingMultiplier) ^ Z.GetHashCode();
                return hash;
            }
        }
    }

    private class IVector3TypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context,
            CultureInfo culture, object value)
        {
            if (value is string)
            {
                string[] coords = ((string)value).Split(", ");

                int x, y, z;
                bool p = int.TryParse(coords[0], out x);
                bool q = int.TryParse(coords[1], out y);
                bool r = int.TryParse(coords[2], out z);
                if (p && q && r)
                {
                    return new IVector3(x, y, z);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context,
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return value.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
