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
        CommandBuffer recorder,
        XoShiRo128starstar random)
    {
        var cluster = terrainConfiguration!.GetRandomCluster(random);

        switch (cluster.SpawnStrategy)
        {
            case TerrainSpawnStrategy.Single:
                return SpawnSingleTerrain(baseCell, cluster, recorder, spawned, random);
            case TerrainSpawnStrategy.Row:
                return SpawnRowTerrain(baseCell, cluster, recorder, spawned, random);
            case TerrainSpawnStrategy.Vent:
                return SpawnVentTerrain(baseCell, cluster, recorder, spawned, random);
        }

        return false;
    }

    private (bool SkipSpawn, Vector3? Position) GetSpawnStartingPosition(Vector2I baseCell,
        List<SpawnedTerrainCluster> spawned,
        XoShiRo128starstar random, float overlapRadius = 0f)
    {
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

            var overlaps = OverlapsWithSpawned(spawned, position);

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
        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
            overlapRadius;
        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE + overlapRadius;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE -
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

    private bool OverlapsWithSpawned(List<SpawnedTerrainCluster> spawned, Vector3 position, float overlapRadius = 0)
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
        CommandBuffer recorder,
        List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        var (skipSpawn, startingPosition) = GetSpawnStartingPosition(baseCell, spawned, random);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var groupSpawnData = new GroupSpawnData(position, [position], chosenGroup.MaxPossibleChunkRadius,
            nextGroupId++, cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(position, [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));
        return true;
    }

    private bool SpawnRowTerrain(Vector2I baseCell, TerrainConfiguration.TerrainClusterConfiguration cluster,
        CommandBuffer recorder, List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        var (skipSpawn, startingPosition) = GetSpawnStartingPosition(baseCell, spawned, random);

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

            var overlaps = OverlapsWithSpawned(spawned, position);

            if (overlaps)
                break;

            chunksPositions.Add(position);

            var xOffsetVariation = random.Next(-5, 5);
            var shift = Math.Clamp(offsetDirection + xOffsetVariation, -chosenGroup.MaxPossibleChunkRadius,
                chosenGroup.MaxPossibleChunkRadius);

            position.X += shift;
            position.Z += (float)Math.Sqrt(Math.Pow(chosenGroup.MaxPossibleChunkRadius, 2) - Math.Pow(shift, 2));

            var groupSpawnData = new GroupSpawnData(position, [position], chosenGroup.MaxPossibleChunkRadius,
                nextGroupId++, cluster.TerrainGroups.GetItemByIndex(0).Chunks);
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
        XoShiRo128starstar random)
    {
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var radius = chosenGroup.MaxPossibleChunkRadius;
        var layers = random.Next(2, 5);
        var overlapRadius = radius * (layers + 2);

        var (skipSpawn, startingPosition) = GetSpawnStartingPosition(baseCell, spawned, random, overlapRadius);

        if (skipSpawn)
            return true;

        if (startingPosition is null)
            return false;

        var position = startingPosition.Value;

        var overlaps = OverlapsWithSpawned(spawned, position, overlapRadius);
        if (overlaps)
        {
            return false;
        }

        var groupSpawnData = new List<GroupSpawnData>();

        var chunksPositions = GetNewVentTerrainPositions(radius, position, random, layers);
        groupSpawnData.Add(new GroupSpawnData(position, chunksPositions, overlapRadius,
            nextGroupId++, chosenGroup.Chunks));

        if (layers >= 0)
        {
            var newPositionDistance = overlapRadius - 1;
            var angle = random.NextFloat() * MathF.Tau;
            var secondVentPosition = position + new Vector3(MathF.Cos(angle) * newPositionDistance,
                0,
                MathF.Sin(angle) * newPositionDistance);
            layers = random.Next(2, 4);
            overlapRadius = radius * (layers + 3);

            if (IsPositionInCell(secondVentPosition, baseCell, overlapRadius) &&
                !OverlapsWithSpawned(spawned, position, overlapRadius))
            {
                var baseChunksPositions = GetNewVentTerrainPositions(radius, secondVentPosition, random, layers);
                groupSpawnData.Add(new GroupSpawnData(secondVentPosition, baseChunksPositions, overlapRadius,
                    nextGroupId++, chosenGroup.Chunks));
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
        var yLevel = 10 * layers - 25;

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

        GD.Print(groupSpawnData.Position, ",    ", groupSpawnData.OverlapRadius);

        foreach (var position in groupSpawnData.ChunksPositions)
        {
            var chunk = groupSpawnData.Chunks.GetItemByIndex(random.Next(groupSpawnData.Chunks.Count));
            var yOffset = new Vector3(0, random.NextSingle() * Constants.TERRAIN_HEIGHT_RANDOMNESS, 0);

            SpawnHelpers.SpawnMicrobeTerrainWithoutFinalizing(recorder, worldSimulation,
                position + yOffset, chunk, groupId, random);

            data.ExpectedMemberCount += 1;
        }

        return data;
    }

    private class GroupSpawnData(Vector3 position, List<Vector3> chunksPositions, float overlapRadius, uint groupId,
        List<TerrainConfiguration.TerrainChunkConfiguration> chunks)
    {
        public Vector3 Position = position;
        public List<Vector3> ChunksPositions = chunksPositions;
        public float OverlapRadius = overlapRadius;
        public uint GroupId = groupId;
        public int ExpectedMemberCount = 0;
        public List<TerrainConfiguration.TerrainChunkConfiguration> Chunks = chunks;
    }

    private class ClusterSpawnData(Vector3 position,
        List<GroupSpawnData> groups)
    {
        public Vector3 Position = position;
        public List<GroupSpawnData> Groups = groups;
    }
}
