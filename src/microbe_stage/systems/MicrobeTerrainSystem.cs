namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Components;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG32;

/// <summary>
///   Handles spawning and despawning the terrain as the player moves in the microbe stage
/// </summary>
[RunsBefore(typeof(SpawnSystem))]
[ReadsComponent(typeof(MicrobeTerrainChunk))]
[RuntimeCost(1)]
public class MicrobeTerrainSystem : BaseSystem<World, float>
{
    private readonly WorldSimulation worldSimulation;

    private readonly QueryDescription allTerrainQuery = new QueryDescription().WithAll<MicrobeTerrainChunk>();

    [JsonProperty]
    private readonly Dictionary<Vector2I, List<SpawnedTerrainCluster>> terrainGridData = new();

    private readonly List<Vector2I> despawnQueue = new();
    private readonly List<Vector2I> spawnQueue = new();

    private readonly Dictionary<uint, SpawnedTerrainGroup> spawnedGroupsWithMissingMembers = new();

    private readonly List<SpawnedTerrainCluster> blankClusterList = new();

    [JsonProperty]
    private Vector3 playerPosition;

    private Vector3 nextPlayerPosition;

    private float playerProtectionRadiusSquared = 20 * 20;

    private int maxSpawnAttempts = 10;

    [JsonProperty]
    private long baseSeed;

    [JsonProperty]
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
    [JsonProperty]
    private uint nextGroupId = 1;

    public MicrobeTerrainSystem(WorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    public static Vector2I PositionToTerrainCell(Vector3 position)
    {
        return new Vector2I((int)(position.X * Constants.TERRAIN_GRID_SIZE_INV),
            (int)(position.Z * Constants.TERRAIN_GRID_SIZE_INV));
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
    ///   Does an approximate check if a world position would be inside terrain or not
    /// </summary>
    /// <param name="position">The position to check </param>
    /// <param name="checkRadiusSquared">
    ///   Can be increased to make separation with terrain a bigger requirement. Note that this has to be squared!
    /// </param>
    /// <returns>
    ///   False if position is likely not blocked, true if there's a known terrain area that overlaps the radius
    ///   of the check area.
    /// </returns>
    public bool IsPositionBlocked(Vector3 position, float checkRadiusSquared = 5)
    {
        position.Y = 0;
        var cell = PositionToTerrainCell(position);

        if (!terrainGridData.TryGetValue(cell, out var clusters))
            return false;

        // Find if too close to any terrain group
        foreach (var cluster in clusters)
        {
            // Can filter by cluster distance first to cut down on overall checks
            if (position.DistanceSquaredTo(cluster.CenterPosition) - cluster.MaxRadiusSquared > checkRadiusSquared)
                continue;

            foreach (var terrainGroup in cluster.Parts)
            {
                // And then check individual group in a cluster the spawn position is too close to
                if (position.DistanceSquaredTo(terrainGroup.Position) - terrainGroup.SquaredRadius <=
                    checkRadiusSquared)
                {
                    // Found a colliding part
                    return true;
                }
            }
        }

        // No terrain encountered
        return false;
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

        // Check if player moved terrain grids and if so perform an operation
        var playerGrid = PositionToTerrainCell(nextPlayerPosition);
        if (PositionToTerrainCell(playerPosition) != playerGrid)
        {
            // Update position here to have it ready in spawn calculations
            playerPosition = nextPlayerPosition;

            // Queue despawning of terrain cells that are out of range
            foreach (var entry in terrainGridData)
            {
                var distance = Math.Abs(entry.Key.X - playerGrid.X) + Math.Abs(entry.Key.Y - playerGrid.Y);

                if (distance > Constants.TERRAIN_SPAWN_AREA_NUMBER)
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

                    var distance = Math.Abs(currentPos.X - playerGrid.X) + Math.Abs(currentPos.Y - playerGrid.Y);

                    if (distance <= Constants.TERRAIN_SPAWN_AREA_NUMBER)
                    {
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
        // Try to despawn normally first, and then re-fetch data if missing any members
        bool missing = false;
        foreach (var cluster in clusters)
        {
            foreach (var group in cluster.Parts)
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
            foreach (var group in cluster.Parts)
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

            SpawnNewCluster(cell, result, recorder, random);
        }

        // TODO: might want to remove this print entirely as it's only really useful when tweaking chunk spawn rates
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

    private void SpawnNewCluster(Vector2I baseCell, List<SpawnedTerrainCluster> spawned, CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var cluster = terrainConfiguration!.GetRandomCluster(random);

        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE +
            cluster.OverallRadius;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            cluster.OverallRadius;

        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE +
            cluster.OverallRadius;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            cluster.OverallRadius;

        var rangeX = maxX - minX;
        var rangeZ = maxZ - minZ;

        var currentOverlapSquared = cluster.OverallOverlapRadius * cluster.OverallOverlapRadius;

        // Find a good spot, avoiding overlap with already spawned, or the player position
        for (int i = 0; i < maxSpawnAttempts; ++i)
        {
            var position = new Vector3(minX + random.NextFloat() * rangeX, 0, minZ + random.NextFloat() * rangeZ);

            if (position.DistanceSquaredTo(playerPosition) - playerProtectionRadiusSquared <
                cluster.OverallRadius * cluster.OverallRadius)
            {
                // Too close to the player, don't spawn
                continue;
            }

            bool overlaps = false;

            // Then check against already spawned terrain
            foreach (var alreadySpawned in spawned)
            {
                if (position.DistanceSquaredTo(alreadySpawned.CenterPosition) - alreadySpawned.OverlapRadiusSquared <
                    currentOverlapSquared)
                {
                    // Will overlap existing
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
                continue;

            // No problems, can spawn the cluster
            spawned.Add(SpawnCluster(cluster, position, recorder, random));
            return;
        }

        // If ran out of attempts, we'll just ignore as the terrain is too crowded

        if (!printedClustersTightWarning)
        {
            GD.Print("Terrain is so dense that can't find places to put more");
            printedClustersTightWarning = true;
        }
    }

    private SpawnedTerrainCluster SpawnCluster(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, XoShiRo128starstar random)
    {
        var groupData = new SpawnedTerrainGroup[cluster.TerrainGroups.Count];

        int index = 0;

        var clusterRotation = Quaternion.Identity;

        if (cluster.RandomizeRotation)
            clusterRotation = new Quaternion(Vector3.Up, random.NextSingle() * MathF.Tau);

        foreach (var terrainGroup in cluster.TerrainGroups)
        {
            var groupId = nextGroupId++;
            var data = new SpawnedTerrainGroup(position + terrainGroup.RelativePosition, terrainGroup.Radius, groupId);

            var groupRotation = Quaternion.Identity;

            if (terrainGroup.RandomizeRotation)
                groupRotation = new Quaternion(Vector3.Up, random.NextSingle() * MathF.Tau);

            foreach (var chunk in terrainGroup.Chunks)
            {
                var rotation = clusterRotation * groupRotation;

                var yOffset = new Vector3(0, random.NextSingle() * Constants.TERRAIN_HEIGHT_RANDOMNESS, 0);

                SpawnHelpers.SpawnMicrobeTerrainWithoutFinalizing(recorder, worldSimulation,
                    position + rotation * (terrainGroup.RelativePosition + chunk.RelativePosition) + yOffset,
                    rotation, chunk, groupId, random);

                data.ExpectedMemberCount += 1;
            }

            groupData[index] = data;
            ++index;
        }

        return new SpawnedTerrainCluster(position, cluster.OverallRadius, groupData, cluster.OverallOverlapRadius);
    }

    private void FetchSpawnedChunksToOurData()
    {
        foreach (var entry in terrainGridData)
        {
            foreach (var terrainCluster in entry.Value)
            {
                foreach (var spawnedTerrainGroup in terrainCluster.Parts)
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

    private struct SpawnedTerrainCluster(Vector3 centerPosition, float maxRadius, SpawnedTerrainGroup[] parts,
        float overlapRadius)
    {
        public Vector3 CenterPosition = centerPosition;
        public float MaxRadiusSquared = maxRadius * maxRadius;

        public SpawnedTerrainGroup[] Parts = parts;

        public float OverlapRadiusSquared = overlapRadius * overlapRadius;
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
                    // Don't need to look in this any more if found all members
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
internal class SpawnedTerrainGroup(Vector3 position, float radius, uint groupId)
{
    public Vector3 Position = position;
    public float SquaredRadius = radius * radius;

    public uint GroupId = groupId;

    public bool MembersFetched;

    public int ExpectedMemberCount;

    public List<Entity> GroupMembers = new();
}
