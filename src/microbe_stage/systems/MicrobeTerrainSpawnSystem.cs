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

        var minX = baseCell.X * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var maxX = (baseCell.X + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var minZ = baseCell.Y * Constants.TERRAIN_GRID_SIZE + Constants.TERRAIN_EDGE_PROTECTION_SIZE;
        var maxZ = (baseCell.Y + 1) * Constants.TERRAIN_GRID_SIZE - Constants.TERRAIN_EDGE_PROTECTION_SIZE;

        var rangeX = maxX - minX;
        var rangeZ = maxZ - minZ;

        var playerCheckSquared = MathUtils.Square(playerProtectionRadius);

        for (int i = 0; i < maxSpawnAttempts; ++i)
        {
            var position = new Vector3(minX + random.NextFloat() * rangeX, 0, minZ + random.NextFloat() * rangeZ);

            if (position.DistanceSquaredTo(playerPosition) < playerCheckSquared)
            {
                // Too close to the player, don't spawn
                // This returns true now so that the player position doesn't affect the final terrain configuration
                // so that the seed leads to a reproducible terrain result
                return true;

                // continue;
            }

            var overlaps = OverlapsWithSpawned(spawned, position);

            if (overlaps)
            {
                // GD.Print(i);
                continue;
            }

            var hasSpawned =
                SpawnNewClusterWithStrategy(cluster, position, recorder, spawned, random);

            if (!hasSpawned)
            {
                // GD.Print("Not spawned terrain cluster at ", position, " after ", i, " attempts");
                continue;
            }

            // GD.Print("Spawned terrain cluster at ", position, " after ", i, " attempts");
            return true;
        }

        return false;
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

    private bool SpawnNewClusterWithStrategy(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        switch (cluster.SpawnStrategy)
        {
            case TerrainSpawnStrategy.Single:
                return SpawnSingleTerrain(cluster, position, recorder, spawned, random);
            case TerrainSpawnStrategy.Row:
                return SpawnRowTerrain(cluster, position, recorder, spawned, random);
            case TerrainSpawnStrategy.Vent:
                return SpawnVentTerrain(cluster, position, recorder, spawned, random);
        }

        return false;
    }

    private bool SpawnSingleTerrain(TerrainConfiguration.TerrainClusterConfiguration cluster, Vector3 position,
        CommandBuffer recorder,
        List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var groupSpawnData = new GroupSpawnData(position, [position], chosenGroup.MaxPossibleChunkRadius,
            nextGroupId++, cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(position, [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));
        return true;
    }

    private bool SpawnRowTerrain(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned, XoShiRo128starstar random)
    {
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

    private bool SpawnVentTerrain(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 chunkGroupPosition, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        XoShiRo128starstar random)
    {
        var chosenGroup = cluster.TerrainGroups.GetItemByIndex(random.Next(cluster.TerrainGroups.Count));
        var radius = chosenGroup.MaxPossibleChunkRadius;
        var layers = random.Next(2, 5);
        var overlapRadius = radius * (layers + 3);

        var overlaps = OverlapsWithSpawned(spawned, chunkGroupPosition, overlapRadius);
        if (overlaps)
        {
            return false;
        }

        var groupSpawnData = new List<GroupSpawnData>();

        var chunksPositions = GetNewVentTerrainPositions(radius, chunkGroupPosition, random, layers);
        groupSpawnData.Add(new GroupSpawnData(chunkGroupPosition, chunksPositions, overlapRadius,
            nextGroupId++, chosenGroup.Chunks));

        if (layers >= 0)
        {
            var newPositionDistance = overlapRadius - 1;
            var angle = random.NextFloat() * MathF.Tau;
            var secondVentPosition = chunkGroupPosition + new Vector3(MathF.Cos(angle) * newPositionDistance,
                33,
                MathF.Sin(angle) * newPositionDistance);
            layers = random.Next(2, 4);
            overlapRadius = radius * (layers + 3);
            
            groupSpawnData.Add(new GroupSpawnData(secondVentPosition, [secondVentPosition], overlapRadius,
                nextGroupId++, chosenGroup.Chunks));

        //     overlaps = OverlapsWithSpawned(spawned, chunkGroupPosition, overlapRadius);
        //     if (!overlaps)
        //     {
        //         var baseChunksPositions = GetNewVentTerrainPositions(radius, secondVentPosition, random, layers);
        //         groupSpawnData.Add(new GroupSpawnData(secondVentPosition, baseChunksPositions, overlapRadius,
        //             nextGroupId++, chosenGroup.Chunks));
        //     }
        }

        var clusterSpawnData = new ClusterSpawnData(chunkGroupPosition, groupSpawnData);

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
