namespace Systems;

using System;
using System.Collections.Generic;
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
[RuntimeCost(1)]
public class MicrobeTerrainSystem : BaseSystem<World, float>
{
    private readonly WorldSimulation worldSimulation;

    private readonly QueryDescription allTerrainQuery = new QueryDescription().WithAll<MicrobeTerrainChunk>();

    [JsonProperty]
    private readonly Dictionary<Vector2I, List<SpawnedTerrainCluster>> terrainGridData = new();

    private readonly List<SpawnedTerrainCluster> blankClusterList = new();

    private readonly List<Vector2I> tempGridCells = new();

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

    /// <summary>
    ///   Used to mark entity groups for finding them
    /// </summary>
    [JsonProperty]
    private uint nextGroupId;

    public MicrobeTerrainSystem(WorldSimulation worldSimulation, World world) : base(world)
    {
        this.worldSimulation = worldSimulation;
    }

    public static Vector2I PositionToTerrainCell(Vector3 position)
    {
        return new Vector2I((int)(position.X % Constants.TERRAIN_GRID_SIZE),
            (int)(position.Z % Constants.TERRAIN_GRID_SIZE));
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
        // Process one despawn and one spawn queue item per update
        
        // Fetch group data if not already
        if (!hasRetrievedAllGroups)
        {
            bool notFound = false;
            
            

            if(!notFound)
                hasRetrievedAllGroups = true;
        }

        // Skip if in a patch with no terrain
        if (terrainConfiguration == null)
            return;

        if (playerPosition == nextPlayerPosition)
            return;

        // Check if player moved terrain grids and if so perform an operation
        var playerGrid = PositionToTerrainCell(nextPlayerPosition);
        if (PositionToTerrainCell(playerPosition) != playerGrid)
        {
            // TODO: make a spawn / despawn queue to avoid as big lag spikes
            // Despawn terrain cells out of range
            foreach (var entry in terrainGridData)
            {
                var distance = Math.Abs(entry.Key.X - playerGrid.X) + Math.Abs(entry.Key.Y - playerGrid.Y);

                if (distance > Constants.TERRAIN_SPAWN_AREA_NUMBER)
                {
                    tempGridCells.Add(entry.Key);
                    DespawnGridArea(entry.Value);
                }
            }

            // Spawn in terrain cells that are now in range
            for (int x = playerGrid.X - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                 x <= playerGrid.X + Constants.TERRAIN_SPAWN_AREA_NUMBER; ++x)
            {
                for (int z = playerGrid.Y - Constants.TERRAIN_SPAWN_AREA_NUMBER;
                     z <= playerGrid.Y + Constants.TERRAIN_SPAWN_AREA_NUMBER; ++z)
                {
                    var currentPos = new Vector2I(x, z);

                    var distance = Math.Abs(currentPos.X - playerGrid.X) + Math.Abs(currentPos.Y - playerGrid.Y);

                    if (distance <= Constants.TERRAIN_SPAWN_AREA_NUMBER &&
                        !terrainGridData.TryGetValue(currentPos, out _))
                    {
                        SpawnTerrainCell(currentPos);
                    }
                }
            }

            if (tempGridCells.Count > 0)
            {
                foreach (var tempGridCell in tempGridCells)
                {
                    terrainGridData.Remove(tempGridCell);
                }

                tempGridCells.Clear();
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
    }

    private void DespawnGridArea(List<SpawnedTerrainCluster> clusters)
    {
        foreach (var cluster in clusters)
        {
            foreach (var group in cluster.Parts)
            {
                foreach (var entity in group.GroupMembers)
                {
                    // TODO: a queue for despawning
                    if (worldSimulation.IsEntityInWorld(entity))
                        worldSimulation.DestroyEntity(entity);
                }

                group.GroupMembers.Clear();
            }
        }
    }

    private void SpawnTerrainCell(Vector2I cell)
    {
        if (terrainConfiguration == null)
        {
            // If nothing to spawn, do nothing
            terrainGridData[cell] = blankClusterList;
            return;
        }

        // TODO: spawn queue the entities instead of immediately just spawning everything

        // We use a random per cell to make sure the same cell spawns in again if the player returns. We can't avoid
        // allocations here as re-initializing the xoshiro class is not possible.
        // Cast required to avoid another warning
        // ReSharper disable once RedundantCast
        var random = new XoShiRo128starstar(baseSeed | (long)cell.X.GetHashCode() | (long)cell.Y.GetHashCode() << 32);

        // TODO: could probably have an entity-limit based correction applied here
        int clusters = random.Next(terrainConfiguration.MinClusters, terrainConfiguration.MaxClusters + 1);

        var recorder = worldSimulation.StartRecordingEntityCommands();

        var result = new List<SpawnedTerrainCluster>();

        while (clusters > 0)
        {
            --clusters;

            SpawnNewCluster(cell, result, recorder, random);
        }

        SpawnHelpers.FinalizeEntitySpawn(recorder, worldSimulation);
        hasRetrievedAllGroups = false;
    }

    private void SpawnNewCluster(Vector2I baseCell, List<SpawnedTerrainCluster> spawned, CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var cluster = terrainConfiguration!.GetRandomCluster(random);

        var minX = baseCell.X * Constants.TERRAIN_SPAWN_AREA_NUMBER + Constants.TERRAIN_EDGE_PROTECTION_SIZE +
            cluster.OverallRadius;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_SPAWN_AREA_NUMBER - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            cluster.OverallRadius;

        var minZ = baseCell.Y * Constants.TERRAIN_SPAWN_AREA_NUMBER + Constants.TERRAIN_EDGE_PROTECTION_SIZE +
            cluster.OverallRadius;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_SPAWN_AREA_NUMBER - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
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
        GD.Print("Terrain is so dense that can't find places to put more");
    }

    private SpawnedTerrainCluster SpawnCluster(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, XoShiRo128starstar random)
    {
        var groupData = new SpawnedTerrainGroup[cluster.TerrainGroups.Count];

        int index = 0;

        foreach (var terrainGroup in cluster.TerrainGroups)
        {
            var groupId = nextGroupId++;
            groupData[index] = new SpawnedTerrainGroup(terrainGroup.RelativePosition, terrainGroup.Radius, groupId);

            ++index;

            foreach (var chunk in terrainGroup.Chunks)
            {
                // TODO: position mirroring etc. slight variation flags?
                SpawnHelpers.SpawnMicrobeTerrainWithoutFinalizing(recorder, worldSimulation,
                    position + terrainGroup.RelativePosition + chunk.RelativePosition, chunk, groupId, random);
            }
        }

        return new SpawnedTerrainCluster(position, cluster.OverallRadius, groupData, cluster.OverallOverlapRadius);
    }

    /// <summary>
    ///   Small local area of terrain parts that constitutes a single area preventing spawns
    /// </summary>
    private struct SpawnedTerrainGroup(Vector3 position, float radius, uint groupId)
    {
        public Vector3 Position = position;
        public float SquaredRadius = radius * radius;

        public uint GroupId = groupId;

        public bool MembersFetched = false;

        public List<Entity> GroupMembers = new();
    }

    private struct SpawnedTerrainCluster(Vector3 centerPosition, float maxRadius, SpawnedTerrainGroup[] parts,
        float overlapRadius)
    {
        public Vector3 CenterPosition = centerPosition;
        public float MaxRadiusSquared = maxRadius * maxRadius;

        public SpawnedTerrainGroup[] Parts = parts;

        public float OverlapRadiusSquared = overlapRadius * overlapRadius;
    }
}
