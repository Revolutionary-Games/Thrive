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

    /// <summary>
    /// List of SpawnItems, used to fill the spawnItemBag when it is empty.
    /// </summary>
    [JsonIgnore]
    private List<SpawnItem> spawnItems = new List<SpawnItem>();

    /// <summary>
    /// Used for a Tetris style random bag. Fill and shuffle the bag,
    /// then simply pop one out until empty. Rinse and repeat.
    /// </summary>
    [JsonIgnore]
    private List<SpawnItem> spawnItemBag = new List<SpawnItem>();

    /// <summary>
    /// List of MicrobeItems, used to fill the wanderMicrobeBag.
    /// </summary>
    [JsonIgnore]
    private List<SpawnItem> microbeItems = new List<SpawnItem>();

    /// <summary>
    /// Used to spawn wandering random microbes when sitting still.
    /// </summary>
    [JsonIgnore]
    private List<SpawnItem> wanderMicrobeBag = new List<SpawnItem>();

    /// <summary>
    /// Queue of spawn items to spawn. a few from this list get spawned every frame.
    /// </summary>
    [JsonIgnore]
    private Queue<SpawnItem> itemsToSpawn = new Queue<SpawnItem>();

    private IEnumerator<ISpawned> queuedSpawn;

    /// <summary>
    /// Used to store SpawnEvents and which grid coordinate the SpawnEvent is in
    /// Generally, this forms a square of grid coordinates around the player.
    /// </summary>
    [JsonProperty]
    private Dictionary<IVector3, SpawnEvent> spawnGrid = new Dictionary<IVector3, SpawnEvent>();

    [JsonProperty]
    private IVector3 oldPlayerGrid;

    public SpawnSystem(Node root)
    {
        worldRoot = root;
    }

    /// <summary>
    /// Sets Despawn radius and adds entity to SPAWNED_GROUP
    /// </summary>
    public static void AddEntityToTrack(ISpawned entity)
    {
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    /// <summary>
    /// Adds item to spawnItems, which is used for spawning Spawn Events
    /// </summary>
    public void AddSpawnItem(SpawnItem spawnItem)
    {
        spawnItems.Add(spawnItem);
    }

    /// <summary>
    /// Adds Microbe to microbeItems, which is used for spawning wandering microbes.
    /// </summary>
    /// <param name="microbeItem">Microbe Item</param>
    public void AddMicrobeItem(MicrobeItem microbeItem)
    {
        microbeItems.Add(microbeItem);
    }

    /// <summary>
    /// Call once microbeItems is full. stores microbeBagSize and sets the spawnWanderTimer.
    /// </summary>
    public void SetMicrobeBagSize()
    {
        microbeBagSize = microbeItems.Count;
        spawnWandererTimer = Constants.WANDERER_SPAWN_RATE / microbeBagSize;
    }

    /// <summary>
    /// Set spawn data that changes per patch and calculates other variables from that data.
    /// </summary>
    public void SetSpawnData(int spawnEventCount, int spawnGridSize, float spawnPatchMultiplier)
    {
        this.spawnEventCount = spawnEventCount;
        this.spawnPatchMultiplier = spawnPatchMultiplier;
        this.spawnGridSize = spawnGridSize;
        spawnEventRadius = spawnGridSize / 4;

        eventDistanceFromPlayerSqr = (spawnEventRadius + Constants.DESPAWN_ITEM_RADIUS + 20)
            * (spawnEventRadius + Constants.DESPAWN_ITEM_RADIUS + 20);
    }

    /// <summary>
    /// Clears spawnGrid and itemsToSpawn.
    /// Also sets oldPlayerGrid to null, meaning the player has jumped to a new possition.
    /// This makes it so spawns do not happen on screen after the jump.
    /// </summary>
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

    /// <summary>
    /// Takes SpawnItems list and shuffles them in bag.
    /// </summary>
    /// <param name="bag">Bag to be shuffled</param>
    /// <param name="fullBag">Full bag to get SpawnItems from</param>
    private void FillBag(List<SpawnItem> bag, List<SpawnItem> fullBag)
    {
        bag.Clear();

        foreach (SpawnItem spawnItem in fullBag)
        {
            bag.Add(spawnItem);
        }

        ShuffleBag(bag);
    }

    /// <summary>
    /// Fisher–Yates Shuffle Alg. Perfectly Random shuffle, O(n)
    /// </summary>
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

    /// <summary>
    /// "Pops" 0th item out of shuffled bag.
    /// If bag is empty, shuffle bag.
    /// </summary>
    /// <param name="bag">Bag to pop from</param>
    /// <param name="fullBag">Full bag needed to fill other bag if it runs out</param>
    /// <returns>Next Spawn Item</returns>
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

    /// <summary>
    /// If entered a new grid, create new SpawnEvents for the new close by grids
    /// and despawn far away entities .
    /// Else, remove SpawnEvents that are too far away,
    /// and spawn SpawnEvents that are close enough to player.
    /// </summary>
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

    /// <summary>
    /// If it is time to spawn a wandering microbe, spawn one in a random spot around the player.
    /// Also Despawn far away entities.
    /// </summary>
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
            // Weighted random will avoid the dead center of the spawn event to discourage
            // SpawnItems spawning on top of eachother.
            float weightedRandom = Mathf.Clamp(random.NextFloat() * 0.9f + 0.1f, 0, 1);
            float spawnRadius = weightedRandom * spawnEventRadius;
            float spawnAngle = random.NextFloat() * 2 * Mathf.Pi;

            Vector3 spawnModPosition = new Vector3(spawnRadius * Mathf.Sin(spawnAngle),
                0, spawnRadius * Mathf.Cos(spawnAngle));

            AddSpawnItemInToSpawnList(spawnEvent.Position + spawnModPosition, spawnItemBag, spawnItems);
        }
    }

    /// <summary>
    /// Chooses a random place inside of a spawnGrid such that the resulting
    /// Spawn Event's spawn radius will not extend into another grid cell.
    /// This means 2 Spawn Events can not overlap.
    /// </summary>
    /// <param name="spawnGridPos">Spawn Grid Position</param>
    /// <returns>Spawn Event Position</returns>
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
        if (queuedSpawn != null)
        {
            SpawnQueuedSpawn();
        }
        else if (itemsToSpawn.Count > 0)
        {
            SpawnItem spawn = itemsToSpawn.Dequeue();

            if (spawn == null)
            {
                return;
            }

            var enumeraable = spawn.Spawn();
            if (enumeraable != null)
            {
                queuedSpawn = enumeraable.GetEnumerator();
                SpawnQueuedSpawn();
            }
        }
    }

    private void SpawnQueuedSpawn()
    {
        for (int i = 0; i < Constants.MAX_SPAWNS_PER_FRAME; i++)
        {
            if (!queuedSpawn.MoveNext())
            {
                queuedSpawn.Dispose();
                queuedSpawn = null;
                return;
            }

            ProcessSpawnedEntity(queuedSpawn.Current);
        }
    }

    /// <summary>
    /// Despawns entities that are far away from the player
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
        entity.DespawnRadius = Constants.DESPAWN_ITEM_RADIUS;
        entity.SpawnedNode.AddToGroup(Constants.SPAWNED_GROUP);
    }

    /// <summary>
    /// Contains all the data needed to spawn a cluster of SpawnItems.
    /// </summary>
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

    /// <summary>
    /// An Integer equivelant of Vector3.
    /// This is used for storing spawn grid coordinates and
    /// to easily check if 2 spawn grid coordinates are the same.
    /// </summary>
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

        /// <summary>
        /// Used by IVector3TypeConverter to deseralize IVector3
        /// </summary>
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

    /// <summary>
    /// TypeConverter for IVector3
    /// </summary>
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
