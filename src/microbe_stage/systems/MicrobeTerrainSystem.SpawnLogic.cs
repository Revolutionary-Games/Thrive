namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using SharedBase.Archive;
using Xoshiro.PRNG32;

/// <summary>
///   Handles spawning and despawning the terrain as the player moves in the microbe stage
/// </summary>
[RunsBefore(typeof(SpawnSystem))]
[ReadsComponent(typeof(MicrobeTerrainChunk))]
[RuntimeCost(1)]
public partial class MicrobeTerrainSystem : BaseSystem<World, float>, IArchivable
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly IWorldSimulation worldSimulation;

    private readonly QueryDescription allTerrainQuery = new QueryDescription().WithAll<MicrobeTerrainChunk>();

    private readonly Dictionary<Vector2I, List<SpawnedTerrainCluster>> terrainGridData = new();

    private readonly List<Vector2I> despawnQueue = new();
    private readonly List<Vector2I> spawnQueue = new();

    private readonly Dictionary<uint, SpawnedTerrainGroup> spawnedGroupsWithMissingMembers = new();

    private readonly List<SpawnedTerrainCluster> blankClusterList = new();

    private Vector3 playerPosition;

    private Vector3 nextPlayerPosition;

    private float playerProtectionRadius = 50;

    private int maxSpawnAttempts = 10;
    private int differentClusterTypeAttempts = 9;

    private long baseSeed;

    private TerrainConfiguration? terrainConfiguration;

    private bool hasRetrievedAllGroups;
    private int unsuccessfulFetches;

    private bool printedClustersTightWarning;

    private int spawnsPerUpdate = 2;
    private int despawnsPerUpdate = 5;

    /// <summary>
    ///   Used to mark entity groups for finding them. Wraparound shouldn't cause problems as the spawns should be
    ///   so far apart.
    /// </summary>
    private uint nextGroupId = 1;

    public MicrobeTerrainSystem(IWorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    private MicrobeTerrainSystem(IWorldSimulation worldSimulation, World world,
        Dictionary<Vector2I, List<SpawnedTerrainCluster>> existingGrid) : base(world)
    {
        this.worldSimulation = worldSimulation;
        terrainGridData = existingGrid;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.MicrobeTerrainSystem;
    public bool CanBeReferencedInArchive => true;

    public static Vector2I PositionToTerrainCell(Vector3 position)
    {
        return new Vector2I((int)Math.Floor(position.X * Constants.TERRAIN_GRID_SIZE_INV),
            (int)Math.Floor(position.Z * Constants.TERRAIN_GRID_SIZE_INV));
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MicrobeTerrainSystem)
            throw new NotSupportedException();

        writer.WriteObject((MicrobeTerrainSystem)obj);
    }

    public static MicrobeTerrainSystem ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MicrobeTerrainSystem(reader.ReadObject<IWorldSimulation>(), reader.ReadObject<World>(),
            reader.ReadObject<Dictionary<Vector2I, List<SpawnedTerrainCluster>>>());

        instance.playerPosition = reader.ReadVector3();
        instance.terrainConfiguration = reader.ReadObjectOrNull<TerrainConfiguration>();
        instance.nextGroupId = reader.ReadUInt32();

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(worldSimulation);
        writer.WriteAnyRegisteredValueAsObject(World);
        writer.WriteObject(terrainGridData);
        writer.Write(playerPosition);
        writer.WriteObjectOrNull(terrainConfiguration);
        writer.Write(nextGroupId);

        // It's probably fine to not save the spawn / despawn queues as they should be pretty short-lived
    }

    public void ReportPlayerPosition(Vector3 position)
    {
        nextPlayerPosition = position;
        nextPlayerPosition.Y = 0;
    }

    public void SetPatch(Patch currentPatch)
    {
        baseSeed = long.RotateLeft(currentPatch.DynamicDataSeed, 5);
        terrainConfiguration = currentPatch.BiomeTemplate.Terrain;
    }

    /// <summary>
    ///   Does an approximate check if a world position is inside terrain or not
    /// </summary>
    /// <param name="position">The position to check </param>
    /// <param name="checkRadius">
    ///   Can be increased to make separation with terrain a bigger requirement. Note that this has to be squared!
    /// </param>
    /// <returns>
    ///   False if the position is likely not blocked, true if there's a known terrain area that overlaps the radius
    ///   of the check area.
    /// </returns>
    [ArchiveAllowedMethod]
    public bool IsPositionBlocked(Vector3 position, float checkRadius = 5)
    {
        position.Y = 0;
        var cell = PositionToTerrainCell(position);

        if (!terrainGridData.TryGetValue(cell, out var clusters))
            return false;

        return OverlapsWithAlreadySpawned(clusters, position, checkRadius);
    }

    public override void Update(in float delta)
    {
        bool playerHasMoved = playerPosition != nextPlayerPosition;

        // Process despawn and spawn queue
        int despawns = 0;
        int spawns = 0;
        while (despawnQueue.Count > 0 && despawns < despawnsPerUpdate)
        {
            var coordinates = despawnQueue[^1];

            // If something is deleted already, it is not too serious of a problem
            if (terrainGridData.TryGetValue(coordinates, out var toDespawn))
            {
                DespawnGridArea(toDespawn);
                terrainGridData.Remove(coordinates);
                ++despawns;
            }

            despawnQueue.RemoveAt(despawnQueue.Count - 1);
        }

        // Skip if in a patch with no terrain
        if (terrainConfiguration == null)
        {
            if (spawnQueue.Count > 0)
                spawnQueue.Clear();

            return;
        }

        while (spawnQueue.Count > 0 && spawns < spawnsPerUpdate)
        {
            SpawnTerrainCell(spawnQueue[^1]);
            spawnQueue.RemoveAt(spawnQueue.Count - 1);
            ++spawns;
        }

        // Fetch group data if not already (maybe should skip fetch if we just spawned something from the queue?)
        if (!hasRetrievedAllGroups)
        {
            FetchSpawnedChunksToOurData();
        }

        if (!playerHasMoved)
            return;

#if DEBUG
        printedClustersTightWarning = false;
#endif

        // Check if the player moved terrain grids and if so, perform an operation
        var playerGrid = PositionToTerrainCell(nextPlayerPosition);
        if (PositionToTerrainCell(playerPosition) != playerGrid)
        {
            // Update the position here to have it ready in spawn calculations
            playerPosition = nextPlayerPosition;

            // Queue despawning of terrain cells that are out of range
            foreach (var entry in terrainGridData)
            {
                var minX = playerGrid.X - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                var maxX = playerGrid.X + Constants.TERRAIN_SPAWN_AREA_NUMBER;
                var minZ = playerGrid.Y - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                var maxZ = playerGrid.Y + Constants.TERRAIN_SPAWN_AREA_NUMBER;

                var shouldDespawn = entry.Key.X < minX || entry.Key.X > maxX ||
                    entry.Key.Y < minZ || entry.Key.Y > maxZ;

                if (shouldDespawn)
                {
                    // We don't process any despawns immediately here, as they shouldn't be totally time-critical
                    despawnQueue.Add(entry.Key);
                }
            }

            // Initial spawn if data is likely empty
            bool initialSpawn = terrainGridData.Count - despawnQueue.Count < 1;

            // Spawn in terrain cells that are now in range
            for (int x = playerGrid.X - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                 x <= playerGrid.X + Constants.TERRAIN_SPAWN_AREA_NUMBER; ++x)
            {
                for (int z = playerGrid.Y - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                     z <= playerGrid.Y + Constants.TERRAIN_SPAWN_AREA_NUMBER; ++z)
                {
                    var currentPos = new Vector2I(x, z);

                    if (!terrainGridData.TryGetValue(currentPos, out _))
                    {
                        // Limited spawns per frame. But if initially spawning in terrain then we want to spawn
                        // everything at once to not leave an empty world for a little bit
                        if (spawns < spawnsPerUpdate || initialSpawn)
                        {
                            SpawnTerrainCell(currentPos);
                            ++spawns;
                        }
                        else
                        {
                            // And queue the other ones.
                            // This contains check here is in case the queue is long, and the player is moving
                            // superfast causing duplicate terrain load requests.
                            if (!spawnQueue.Contains(currentPos))
                            {
                                spawnQueue.Add(currentPos);
                            }
                        }
                    }
                    else
                    {
                        // Make sure existing data won't be deleted
                        despawnQueue.Remove(currentPos);
                    }
                }
            }
        }

        playerPosition = nextPlayerPosition;
    }

    /// <summary>
    ///   Despawn all current terrain (used when the player is changing patches)
    /// </summary>
    public void DespawnAll()
    {
        ClearQueue();

        playerPosition = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        // This allocates a lambda, but despawning everything should be rare enough that that is fine
        World.Query(allTerrainQuery, entity =>
        {
            if (worldSimulation.IsEntityInWorld(entity))
            {
                worldSimulation.DestroyEntity(entity);
            }
        });

        // Clear all data about the deleted entities
        terrainGridData.Clear();
    }

    /// <summary>
    ///   Stops queued spawns / actions from happening
    /// </summary>
    public void ClearQueue()
    {
        spawnQueue.Clear();
        despawnQueue.Clear();
    }

    private void DespawnGridArea(List<SpawnedTerrainCluster> clusters)
    {
        // Try to despawn normally first and then re-fetch data if missing any members
        bool missing = false;
        foreach (var cluster in clusters)
        {
            foreach (var group in cluster.TerrainGroups)
            {
                foreach (var entity in group.GroupMembers)
                {
                    if (worldSimulation.IsEntityInWorld(entity))
                        worldSimulation.DestroyEntity(entity);
                }

                if (!group.MembersFetched)
                {
                    missing = true;
                    continue;
                }

                group.GroupMembers.Clear();
            }
        }

        if (!missing)
            return;

        // Need to perform a fetch cycle to be able to despawn properly
        GD.Print("Doing a fetch cycle before despawning terrain so that we have them all");
        FetchSpawnedChunksToOurData();

        foreach (var cluster in clusters)
        {
            foreach (var group in cluster.TerrainGroups)
            {
                foreach (var entity in group.GroupMembers)
                {
                    if (worldSimulation.IsEntityInWorld(entity))
                        worldSimulation.DestroyEntity(entity);
                }

                if (!group.MembersFetched)
                {
                    GD.PrintErr("Despawning terrain group with unfetched members, will not be able to despawn " +
                        "all entities correctly");
                }

                group.GroupMembers.Clear();
            }
        }
    }

    private void SpawnTerrainCell(Vector2I cell)
    {
        // Safety return if terrain is already generated
        if (terrainGridData.ContainsKey(cell))
        {
            GD.Print($"Already spawned terrain for: {cell}");
            return;
        }

        if (terrainConfiguration == null)
        {
            // If nothing to spawn, do nothing
            terrainGridData[cell] = blankClusterList;
            return;
        }

        // TODO: do we need to queue individual bits of terrain rather than entire grid cells?

        // We use a random per cell to make sure the same cell spawns in again if the player returns. We can't avoid
        // allocations here as re-initializing the xoshiro class is not possible.
        // Cast required to avoid another warning
        // ReSharper disable once RedundantCast
        var random = new XoShiRo128starstar(baseSeed ^ (long)cell.X.GetHashCode() ^ (long)cell.Y.GetHashCode() << 32);

        // TODO: could probably have an entity-limit based correction applied here?
        int clusters = random.Next(terrainConfiguration.MinClusters, terrainConfiguration.MaxClusters + 1);
        var wantedClusters = clusters;

        var recorder = worldSimulation.StartRecordingEntityCommands();

        var result = new List<SpawnedTerrainCluster>();

        while (clusters > 0)
        {
            --clusters;

            bool succeeded = false;

            int retries = differentClusterTypeAttempts;
            for (int i = 0; i < retries; ++i)
            {
                // Try a few random clusters in case one fits
                if (SpawnNewCluster(cell, result, recorder, random))
                {
                    succeeded = true;
                    break;
                }
            }

            if (succeeded)
                continue;

            // If ran out of attempts, we'll just ignore as the terrain is too crowded
            if (!printedClustersTightWarning)
            {
                GD.Print("Terrain is so dense that can't find places to put more");
                printedClustersTightWarning = true;
            }
        }

#if DEBUG
        if (result.Count < wantedClusters)
        {
            GD.Print($"Could only spawn {result.Count} terrain clusters out of wanted {wantedClusters}");
        }
#endif

        SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);

        terrainGridData[cell] = result;
        hasRetrievedAllGroups = false;
        unsuccessfulFetches = 0;
    }

    private void FetchSpawnedChunksToOurData()
    {
        foreach (var entry in terrainGridData)
        {
            foreach (var terrainCluster in entry.Value)
            {
                foreach (var spawnedTerrainGroup in terrainCluster.TerrainGroups)
                {
                    if (!spawnedTerrainGroup.MembersFetched)
                    {
                        spawnedGroupsWithMissingMembers[spawnedTerrainGroup.GroupId] = spawnedTerrainGroup;
                    }
                }
            }
        }

        if (spawnedGroupsWithMissingMembers.Count < 1)
        {
            hasRetrievedAllGroups = true;
            return;
        }

        var query = new TerrainChunkFetchQuery(spawnedGroupsWithMissingMembers);
        World.InlineEntityQuery<TerrainChunkFetchQuery, MicrobeTerrainChunk>(allTerrainQuery, ref query);

        if (spawnedGroupsWithMissingMembers.Count < 1 || unsuccessfulFetches > 2)
        {
            // This gives up after some time in case some detection fails
            if (spawnedGroupsWithMissingMembers.Count > 0)
            {
                GD.PrintErr("Failed to find a terrain chunk that should have spawned");
                spawnedGroupsWithMissingMembers.Clear();
            }

            hasRetrievedAllGroups = true;
        }
        else
        {
            hasRetrievedAllGroups = false;
            spawnedGroupsWithMissingMembers.Clear();
            ++unsuccessfulFetches;
        }
    }

    // This is internal to make archive registration work
    internal struct SpawnedTerrainCluster(Vector3 centerPosition, SpawnedTerrainGroup[] terrainGroups) : IArchivable
    {
        public const ushort SERIALIZATION_VERSION_CLUSTER = 3;

        public Vector3 CenterPosition = centerPosition;

        public SpawnedTerrainGroup[] TerrainGroups = terrainGroups;

        public ushort CurrentArchiveVersion => SERIALIZATION_VERSION_CLUSTER;
        public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainCluster;
        public bool CanBeReferencedInArchive => false;

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
        {
            if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainCluster)
                throw new NotSupportedException();

            writer.WriteObject((SpawnedTerrainCluster)obj);
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static SpawnedTerrainCluster ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
        {
            if (version is > SERIALIZATION_VERSION_CLUSTER or <= 0)
                throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_CLUSTER);

            var center = reader.ReadVector3();
            if (version < 3)
                _ = reader.ReadFloat();
            var parts = reader.ReadObject<SpawnedTerrainGroup[]>();
            if (version < 3)
                _ = reader.ReadFloat();
            var instance = new SpawnedTerrainCluster(center, parts);

            return instance;
        }

        public static object ReadFromArchiveBoxed(ISArchiveReader reader, ushort version, int referenceId)
        {
            return ReadFromArchive(reader, version, referenceId);
        }

        public void WriteToArchive(ISArchiveWriter writer)
        {
            writer.Write(CenterPosition);
            writer.WriteObject(TerrainGroups);
        }
    }

    private struct TerrainChunkFetchQuery : IForEachWithEntity<MicrobeTerrainChunk>
    {
        // TODO: determine if using a dictionary or list is faster here
        private readonly Dictionary<uint, SpawnedTerrainGroup> thingsToLookFor;

        public TerrainChunkFetchQuery(Dictionary<uint, SpawnedTerrainGroup> thingsToLookFor)
        {
            this.thingsToLookFor = thingsToLookFor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Entity entity, ref MicrobeTerrainChunk chunk)
        {
            // Quickly skip chunks we aren't interested in scanning
            if (!thingsToLookFor.TryGetValue(chunk.TerrainGroupId, out var group))
                return;

            // Found a member
            if (!group.GroupMembers.Contains(entity))
            {
                group.GroupMembers.Add(entity);
                if (group.GroupMembers.Count >= group.ExpectedMemberCount)
                {
                    // Don't need to look at this any more if found all members
                    group.MembersFetched = true;
                    thingsToLookFor.Remove(chunk.TerrainGroupId);
                }
            }
        }
    }
}

/// <summary>
///   Small local area of terrain parts that constitutes a single area preventing spawns. Needs to be a class as
///   references to this are processed.
/// </summary>
internal class SpawnedTerrainGroup(Vector3 position, float radius, uint groupId) : IArchivable
{
    public const ushort SERIALIZATION_VERSION = 2;

    public Vector3 Position = position;
    public float Radius = radius;

    public uint GroupId = groupId;

    public bool MembersFetched;

    public int ExpectedMemberCount;

    public List<Entity> GroupMembers = new();

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainGroup;
    public bool CanBeReferencedInArchive => false;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.SpawnedTerrainGroup)
            throw new NotSupportedException();

        writer.WriteObject((SpawnedTerrainGroup)obj);
    }

    public static SpawnedTerrainGroup ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var position = reader.ReadVector3();
        var radius = reader.ReadFloat();

        var instance = new SpawnedTerrainGroup(position, radius, reader.ReadUInt32())
        {
            MembersFetched = reader.ReadBool(),
            ExpectedMemberCount = reader.ReadInt32(),
            GroupMembers = reader.ReadObject<List<Entity>>(),
        };

        // Override the values that got incorrectly doubled from loading an old save
        if (version < 2)
        {
            instance.Radius = MathF.Sqrt(radius);
        }

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Position);
        writer.Write(Radius);
        writer.Write(GroupId);
        writer.Write(MembersFetched);
        writer.Write(ExpectedMemberCount);
        writer.WriteObject(GroupMembers);
    }
}
