namespace Systems;

using System;
using System.Collections.Generic;
using AngleSharp.Common;
using Arch.Buffer;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Handles terrain spawning shapes logic
/// </summary>
public partial class MicrobeTerrainSystem
{
    private bool SpawnNewCluster(Vector2I baseCell, List<SpawnedTerrainCluster> spawned,
        CommandBuffer recorder, XoShiRo128starstar random)
    {
        var cluster = terrainConfiguration!.GetRandomCluster(random);

        switch (cluster.SpawnStrategy)
        {
            case TerrainSpawnStrategy.Single:
                return SpawnSingleTerrain(baseCell, cluster, recorder, spawned, random);
            case TerrainSpawnStrategy.Vent:
                return SpawnVentTerrain(baseCell, cluster, recorder, spawned, random);
        }

        return false;
    }

    private (bool SkipSpawn, Vector3? Position) GetSpawnStartingPosition(Vector2I baseCell,
        List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random, float overlapRadius = 0.0f)
    {
        var (minX, maxX, minZ, maxZ) = GetCellBordersForGroup(baseCell, overlapRadius);

        var rangeX = maxX - minX;
        var rangeZ = maxZ - minZ;

        var playerCheckSquared = MathUtils.Square(playerProtectionRadius);

        for (int i = 0; i < maxSpawnAttempts; ++i)
        {
            var position = new Vector3(minX + random.NextFloat() * rangeX, 0, minZ + random.NextFloat() * rangeZ);

            if (position.DistanceSquaredTo(playerPosition) + MathUtils.Square(overlapRadius) < playerCheckSquared)
            {
                // Too close to the player, don't spawn
                // This returns true now so that the player position doesn't affect the final terrain configuration
                // so that the seed leads to a reproducible terrain result
                return (true, null);
            }

            var overlaps = OverlapsWithAlreadySpawned(spawned, position);

            if (overlaps)
            {
                continue;
            }

            return (false, position);
        }

        return (false, null);
    }

    private bool IsPositionInCell(Vector3 position, Vector2I cell, float overlapRadius = 0)
    {
        var (minX, maxX, minZ, maxZ) = GetCellBordersForGroup(cell, overlapRadius);

        var playerCheckSquared = MathUtils.Square(playerProtectionRadius);

        if (position.DistanceSquaredTo(playerPosition) + MathUtils.Square(overlapRadius) < playerCheckSquared)
        {
            // Too close to the player, don't spawn
            // This returns true now so that the player position doesn't affect the final terrain configuration
            // so that the seed leads to a reproducible terrain result
            return false;
        }

        return position.X >= minX && position.X <= maxX &&
            position.Z >= minZ && position.Z <= maxZ;
    }

    private (float MinX, float MaxX, float MinZ, float MaxZ) GetCellBordersForGroup(Vector2I baseCell,
        float overlapRadius)
    {
        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE_X + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE_X - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            overlapRadius;
        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE_Z + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE_Z - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            overlapRadius;
        return (minX, maxX, minZ, maxZ);
    }

    private bool OverlapsWithAlreadySpawned(List<SpawnedTerrainCluster> spawned,
        Vector3 position, float overlapRadius = 0)
    {
        foreach (var spawnedClusters in spawned)
        {
            foreach (var spawnedGroup in spawnedClusters.TerrainGroups)
            {
                var distanceSquared = position.DistanceSquaredTo(spawnedGroup.Position);
                if (distanceSquared <= MathUtils.Square(spawnedGroup.Radius + overlapRadius))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool SpawnSingleTerrain(Vector2I baseCell, TerrainConfiguration.TerrainClusterConfiguration cluster,
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        var (skipSpawn, startingPosition) =
            GetSpawnStartingPosition(baseCell, spawned, random);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var groupSpawnData = new GroupSpawnData(position, [position], chosenGroup.MaxPossibleChunkRadius,
            cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(position, [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));
        return true;
    }

    private bool SpawnVentTerrain(Vector2I baseCell, TerrainConfiguration.TerrainClusterConfiguration cluster,
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        XoShiRo128starstar random)
    {
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var radius = chosenGroup.MaxPossibleChunkRadius;
        var layers = random.Next(Constants.TERRAIN_VENT_RINGS_MIN, Constants.TERRAIN_VENT_RINGS_MAX);
        var collisionRadius = radius * layers;
        var overlapRadius = radius * (layers + Constants.TERRAIN_VENT_OVERLAP_MARGIN);

        var (skipSpawn, startingPosition) =
            GetSpawnStartingPosition(baseCell, spawned, random, overlapRadius);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var overlaps = OverlapsWithAlreadySpawned(spawned, position, overlapRadius);
        if (overlaps)
        {
            return false;
        }

        var groupSpawnData = new List<GroupSpawnData>();
        var chunksPositions = GetNewVentTerrainPositions(radius, position, random, layers);

        groupSpawnData.Add(new GroupSpawnData(position, chunksPositions, overlapRadius,
            chosenGroup.Chunks, collisionRadius));

        // Generates a second smaller vent right beside the first one if it fits the grid and doesn't overlap
        // with other terrain. Therefore TERRAIN_SECOND_VENT_CHANCE is lower in practise
        if (layers >= Constants.TERRAIN_SECOND_VENT_RINGS_MIN_THRESHOLD &&
            random.NextFloat() <= Constants.TERRAIN_SECOND_VENT_CHANCE)
        {
            var newPositionDistance = overlapRadius - 1;
            var angle = random.NextFloat() * MathF.Tau;
            var secondVentPosition = position + new Vector3(MathF.Cos(angle) * newPositionDistance,
                0,
                MathF.Sin(angle) * newPositionDistance);

            layers = random.Next(Constants.TERRAIN_VENT_RINGS_MIN, layers + 1);
            collisionRadius = radius * layers;
            overlapRadius = radius * (layers + Constants.TERRAIN_VENT_OVERLAP_MARGIN);

            if (!OverlapsWithAlreadySpawned(spawned, position, overlapRadius)
                && IsPositionInCell(secondVentPosition, baseCell, overlapRadius))
            {
                var secondVentChunksPositions = GetNewVentTerrainPositions(radius, secondVentPosition, random, layers);
                groupSpawnData.Add(new GroupSpawnData(secondVentPosition, secondVentChunksPositions, overlapRadius,
                    chosenGroup.Chunks, collisionRadius));
            }
        }

        var clusterSpawnData = new ClusterSpawnData(position, groupSpawnData);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));

        return true;
    }

    private List<Vector3> GetNewVentTerrainPositions(float radius,
        Vector3 chunkGroupPosition,
        XoShiRo128starstar random, int layers)
    {
        var segments = Constants.TERRAIN_VENT_SEGMENTS;
        var segmentRadius = radius;
        var yLevel = Constants.TERRAIN_VENT_RING_HEIGHT_REDUCTION * layers + Constants.TERRAIN_VENT_OUTER_RING_HEIGHT;

        var chunksPositions = new List<Vector3>();

        for (var j = 0; j < layers; ++j)
        {
            for (var i = 0; i < segments; ++i)
            {
                var angle = i * MathF.Tau / segments;
                var x = chunkGroupPosition.X + MathF.Cos(angle) * segmentRadius + random.Next(-2, 2);
                var z = chunkGroupPosition.Z + MathF.Sin(angle) * segmentRadius + random.Next(-2, 2);
                var position = new Vector3(x, yLevel + random.Next(-1, 1), z);
                chunksPositions.Add(position);
            }

            yLevel -= Constants.TERRAIN_VENT_RING_HEIGHT_REDUCTION;
            segments += Constants.TERRAIN_VENT_SEGMENTS;
            segmentRadius += radius;
        }

        return chunksPositions;
    }

    private SpawnedTerrainCluster SpawnTerrainCluster(ClusterSpawnData clusterSpawnData, CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var groupData = new SpawnedTerrainGroup[clusterSpawnData.Groups.Count];
        var index = 0;

        for (var i = 0; i < clusterSpawnData.Groups.Count; ++i)
        {
            var group = clusterSpawnData.Groups[i];
            var data = SpawnTerrainGroup(group, recorder, random);

            groupData[index] = data;
            ++index;
        }

        return new SpawnedTerrainCluster(clusterSpawnData.Position, groupData);
    }

    private SpawnedTerrainGroup SpawnTerrainGroup(GroupSpawnData groupSpawnData, CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var groupId = nextGroupId++;
        var data = new SpawnedTerrainGroup(groupSpawnData.Position,
            groupSpawnData.OverlapRadius,
            groupId);

        var skipDefaultCollisionsLoading = groupSpawnData.CollisionRadius > 0;
        if (skipDefaultCollisionsLoading)
        {
            SpawnHelpers.SpawnCollisionWithoutFinalizing(recorder, worldSimulation,
                groupSpawnData.Position, groupId, groupSpawnData.CollisionRadius);
        }

        foreach (var position in groupSpawnData.ChunksPositions)
        {
            var chunk = groupSpawnData.Chunks.GetItemByIndex(random.Next(groupSpawnData.Chunks.Count));
            var yOffset = new Vector3(0, random.NextSingle() * Constants.TERRAIN_HEIGHT_RANDOMNESS, 0);

            SpawnHelpers.SpawnMicrobeTerrainWithoutFinalizing(recorder, worldSimulation,
                position + yOffset, chunk, groupId, random, skipDefaultCollisionsLoading);

            data.ExpectedMemberCount += 1;
        }

        return data;
    }

    /// <summary>
    ///   Data required to spawn a terrain group
    /// </summary>
    /// <param name="position">Main position of the group</param>
    /// <param name="chunksPositions">Positions of all the chunks of the group</param>
    /// <param name="overlapRadius">Radius of the group in which no other chunk can spawn</param>
    /// <param name="chunks">Data of the terrain chunks to spawn</param>
    /// <param name="collisionRadius">
    ///   Used to create one sphere collision shape of the group if greater than 0. If that condition is met,
    ///   other chunk's collision meshes won't be loaded
    /// </param>
    private class GroupSpawnData(Vector3 position, List<Vector3> chunksPositions, float overlapRadius,
        List<TerrainConfiguration.TerrainChunkConfiguration> chunks, float collisionRadius = 0)
    {
        public readonly Vector3 Position = position;
        public readonly List<Vector3> ChunksPositions = chunksPositions;
        public readonly float OverlapRadius = overlapRadius;
        public readonly float CollisionRadius = collisionRadius;
        public readonly List<TerrainConfiguration.TerrainChunkConfiguration> Chunks = chunks;
    }

    /// <summary>
    ///   Data required to spawn a terrain cluster. The cluster can create more complex shapes by combining
    ///   multiple groups together
    /// </summary>
    /// <param name="position">Main position of the cluster</param>
    /// <param name="groups">Groups that compose the cluster</param>
    private class ClusterSpawnData(Vector3 position,
        List<GroupSpawnData> groups)
    {
        public readonly Vector3 Position = position;
        public readonly List<GroupSpawnData> Groups = groups;
    }
}
