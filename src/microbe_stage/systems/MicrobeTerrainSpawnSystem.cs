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
    private void SpawnCornerChunks(Vector2I baseCell, CommandBuffer recorder,
        List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE_X + Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE_X - Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE_Z + Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE_Z - Constants.TERRAIN_EDGE_PROTECTION_SIZE;

        List<Vector3> positions =
        [
            new(minX, 0, minZ),
            new(maxX, 0, minZ),
            new(minX, 0, maxZ),
            new(maxX, 0, maxZ),
        ];

        var cluster = terrainConfiguration!.GetRandomCluster(random);
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var groupSpawnData = new GroupSpawnData(new Vector3(minX, 0, minZ), positions,
            chosenGroup.MaxPossibleChunkRadius,
            cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(new Vector3(minX, 0, minZ), [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));
    }

    private bool SpawnNewCluster(Vector2I baseCell, List<SpawnedTerrainCluster> spawned,
        List<SpawnedTerrainCluster> spawnedNeighboringClusters,
        CommandBuffer recorder, XoShiRo128starstar random)
    {
        var cluster = terrainConfiguration!.GetRandomCluster(random);

        switch (cluster.SpawnStrategy)
        {
            case TerrainSpawnStrategy.Single:
                return SpawnSingleTerrain(baseCell, cluster, recorder, spawned, spawnedNeighboringClusters, random);
            case TerrainSpawnStrategy.Row:
                return SpawnRowTerrain(baseCell, cluster, recorder, spawned, spawnedNeighboringClusters, random);
            case TerrainSpawnStrategy.Vent:
                return SpawnVentTerrain(baseCell, cluster, recorder, spawned, spawnedNeighboringClusters, random);
        }

        return false;
    }

    private (bool SkipSpawn, Vector3? Position) GetSpawnStartingPosition(Vector2I baseCell,
        List<SpawnedTerrainCluster> spawned, List<SpawnedTerrainCluster> spawnedNeighboringClusters,
        XoShiRo128starstar random, float overlapRadius = 0f)
    {
        overlapRadius = 0f;
        var (minX, maxX, minZ, maxZ) = GetCellVertices(baseCell, overlapRadius);

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

            var overlaps = OverlapsWithSpawned(spawned, spawnedNeighboringClusters, position);

            if (overlaps)
            {
                continue;
            }

            return (false, position);
        }

        return (false, null);
    }

    private (float MinX, float MaxX, float MinZ, float MaxZ) GetCellVertices(Vector2I baseCell, float overlapRadius)
    {
        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE_X + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE_X - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            overlapRadius;
        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE_Z + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE_Z - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            overlapRadius;
        return (minX, maxX, minZ, maxZ);
    }

    private bool IsPositionInCell(Vector3 position, Vector2I cell, float overlapRadius = 0)
    {
        var (minX, maxX, minZ, maxZ) = GetCellVertices(cell, overlapRadius);

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

    private bool OverlapsWithSpawned(List<SpawnedTerrainCluster> spawned,
        List<SpawnedTerrainCluster> spawnedNeighboringClusters, Vector3 position, float overlapRadius = 0)
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

        foreach (var spawnedClusters in spawnedNeighboringClusters)
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
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        List<SpawnedTerrainCluster> spawnedNeighboringClusters, XoShiRo128starstar random)
    {
        var (skipSpawn, startingPosition) =
            GetSpawnStartingPosition(baseCell, spawned, spawnedNeighboringClusters, random);

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

    private bool SpawnRowTerrain(Vector2I baseCell, TerrainConfiguration.TerrainClusterConfiguration cluster,
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        List<SpawnedTerrainCluster> spawnedNeighboringClusters,
        XoShiRo128starstar random)
    {
        var (skipSpawn, startingPosition) =
            GetSpawnStartingPosition(baseCell, spawned, spawnedNeighboringClusters, random);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var offsetDirection = random.Next(-chosenGroup.MaxPossibleChunkRadius, chosenGroup.MaxPossibleChunkRadius);
        var chunksPositions = new List<Vector3>();
        var groupSpawnDataList = new List<GroupSpawnData>();

        var numberOfChunks = random.Next(cluster.MinChunks, cluster.MaxChunks + 1);
        for (var i = 1; i < numberOfChunks; ++i)
        {
            position = new Vector3(position.X, position.Y, position.Z);

            var overlaps = OverlapsWithSpawned(spawned, spawnedNeighboringClusters, position);

            if (overlaps)
                break;

            chunksPositions.Add(position);

            var xOffsetVariation = random.Next(-5, 5);
            var shift = Math.Clamp(offsetDirection + xOffsetVariation, -chosenGroup.MaxPossibleChunkRadius,
                chosenGroup.MaxPossibleChunkRadius);

            position.X += shift;
            position.Z += (float)Math.Sqrt(Math.Pow(chosenGroup.MaxPossibleChunkRadius, 2) - Math.Pow(shift, 2));

            var groupSpawnData = new GroupSpawnData(position, [position], chosenGroup.MaxPossibleChunkRadius,
                cluster.TerrainGroups.GetItemByIndex(0).Chunks);
            groupSpawnDataList.Add(groupSpawnData);
        }

        if (chunksPositions.Count < cluster.MinChunks)
            return false;

        var clusterSpawnData = new ClusterSpawnData(position, groupSpawnDataList);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));

        return true;
    }

    private bool SpawnVentTerrain(Vector2I baseCell, TerrainConfiguration.TerrainClusterConfiguration cluster,
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        List<SpawnedTerrainCluster> spawnedNeighboringClusters,
        XoShiRo128starstar random)
    {
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var radius = chosenGroup.MaxPossibleChunkRadius;
        var layers = random.Next(2, 5);
        var collisionRadius = radius * layers;
        var overlapRadius = radius * (layers + 2);

        var (skipSpawn, startingPosition) =
            GetSpawnStartingPosition(baseCell, spawned, spawnedNeighboringClusters, random, overlapRadius);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var overlaps = OverlapsWithSpawned(spawned, spawnedNeighboringClusters, position, overlapRadius);
        if (overlaps)
        {
            return false;
        }

        var groupSpawnData = new List<GroupSpawnData>();

        var chunksPositions = GetNewVentTerrainPositions(radius, position, random, layers);
        groupSpawnData.Add(new GroupSpawnData(position, chunksPositions, overlapRadius,
            chosenGroup.Chunks, collisionRadius));

        if (layers >= 0)
        {
            var newPositionDistance = overlapRadius - 1;
            var angle = random.NextFloat() * MathF.Tau;
            var secondVentPosition = position + new Vector3(MathF.Cos(angle) * newPositionDistance,
                0,
                MathF.Sin(angle) * newPositionDistance);
            layers = random.Next(2, 4);
            collisionRadius = radius * layers;
            overlapRadius = radius * (layers + 2);

            if (OverlapsWithSpawned(spawned, spawnedNeighboringClusters, position, overlapRadius))
            {
                var baseChunksPositions = GetNewVentTerrainPositions(radius, secondVentPosition, random, layers);
                groupSpawnData.Add(new GroupSpawnData(secondVentPosition, baseChunksPositions, overlapRadius,
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
        var segments = 6;
        var segmentRadius = radius;
        var yLevel = 10 * layers - 27;

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

            yLevel -= 10;
            segments += 6;
            segmentRadius += radius;
        }

        return chunksPositions;
    }

    private SpawnedTerrainCluster SpawnTerrainCluster(ClusterSpawnData clusterSpawnData, CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var groupData = new SpawnedTerrainGroup[clusterSpawnData.Groups.Count];
        var index = 0;

        for (var i = 0; i < clusterSpawnData.Groups.Count; i++)
        {
            var group = clusterSpawnData.Groups[i];
            var data = SpawnTerrainGroup(group, recorder, random);

            data.ExpectedMemberCount += 1;
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

    private class GroupSpawnData(Vector3 position, List<Vector3> chunksPositions, float overlapRadius,
        List<TerrainConfiguration.TerrainChunkConfiguration> chunks, float collisionRadius = 0)
    {
        public Vector3 Position = position;
        public List<Vector3> ChunksPositions = chunksPositions;
        public float OverlapRadius = overlapRadius;
        public float CollisionRadius = collisionRadius;
        public List<TerrainConfiguration.TerrainChunkConfiguration> Chunks = chunks;
    }

    private class ClusterSpawnData(Vector3 position,
        List<GroupSpawnData> groups)
    {
        public Vector3 Position = position;
        public List<GroupSpawnData> Groups = groups;
    }
}
