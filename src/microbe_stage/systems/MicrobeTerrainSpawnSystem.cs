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
    // private uint nextGroupId = 1;
    // private readonly IWorldSimulation worldSimulation;
    // private readonly TerrainConfiguration terrainConfiguration;
    // private float playerProtectionRadius;
    // private int maxSpawnAttempts;
    // private Vector3 playerPosition;
    //
    // public MicrobeTerrainSystemSpawn(IWorldSimulation worldSimulation,
    //     TerrainConfiguration terrainConfiguration, float playerProtectionRadius, Vector3 playerPosition, int maxSpawnAttempts)
    // {
    //     this.worldSimulation = worldSimulation;
    //     this.terrainConfiguration = terrainConfiguration;
    //     this.playerProtectionRadius = playerProtectionRadius;
    //     this.maxSpawnAttempts = maxSpawnAttempts;
    // }

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

        var currentOverlap = cluster.OverlapRadius;

        var playerCheckSquared = MathUtils.Square(playerProtectionRadius + cluster.MaxPossibleChunkRadius);

        for (int i = 0; i < maxSpawnAttempts; ++i)
        {
            var position = new Vector3(minX + random.NextFloat() * rangeX, 0, minZ + random.NextFloat() * rangeZ);

            // Keep randomness more consistent
            float slideAngleIfNeeded = random.NextFloat() * MathF.PI * 2;

            if (position.DistanceSquaredTo(playerPosition) < playerCheckSquared)
            {
                // Too close to the player, don't spawn
                // This returns true now so that the player position doesn't affect the final terrain configuration
                // so that the seed leads to a reproducible terrain result
                return true;

                // continue;
            }

            bool overlaps = false;
            bool adjusted = false;

            while (true)
            {
                bool retry = false;

                // Then check against already spawned terrain
                foreach (var alreadySpawned in spawned)
                {
                    var distanceSquared = position.DistanceSquaredTo(alreadySpawned.CenterPosition);
                    if (distanceSquared >= MathUtils.Square(alreadySpawned.OverlapRadius + currentOverlap))
                        continue;

                    // Try sliding to make both clusters fit
                    if (cluster.SlideToFit && !adjusted)
                    {
                        // TODO: this could alternatively not take a random angle but instead just slide outwards
                        // to the closest edge with a normalized vector from CenterPosition to position

                        // var overlap = (alreadySpawned.OverlapRadius + currentOverlap) - MathF.Sqrt(distanceSquared);
                        // var slideVector = new Vector3(MathF.Cos(slideAngleIfNeeded), 0,
                        //                       MathF.Sin(slideAngleIfNeeded)) * overlap * 1.02f;
                        // position += (alreadySpawned.CenterPosition - position).Normalized() * slideVector;

                        var neededDistance = alreadySpawned.OverlapRadius + currentOverlap;
                        var offset = new Vector3(MathF.Cos(slideAngleIfNeeded), 0,
                            MathF.Sin(slideAngleIfNeeded)) * neededDistance * 1.02f;

                        position = alreadySpawned.CenterPosition + offset;

                        // Make sure the position is not outside the target grid
                        if (position.X < minX || position.X > maxX || position.Z < minZ || position.Z > maxZ)
                        {
                            overlaps = true;
                            break;
                        }

#if DEBUG
                        var newDistance = position.DistanceSquaredTo(alreadySpawned.CenterPosition);
                        if (newDistance < MathUtils.Square(alreadySpawned.OverlapRadius + currentOverlap))
                        {
                            GD.Print($"Still overlaps after sliding with the original, " +
                                $"old dist: {MathF.Sqrt(distanceSquared)} new: {MathF.Sqrt(newDistance)}, " +
                                $"needed distance: {alreadySpawned.OverlapRadius + currentOverlap}");
                        }
#endif

                        retry = true;
                        adjusted = true;
                        break;
                    }

                    // Will overlap existing
                    overlaps = true;
                    break;
                }

                if (retry)
                    continue;

                break;
            }

            if (overlaps)
                continue;

            // No problems can try to spawn the cluster
            var hasSpawned =
                SpawnNewClusterWithStrategy(cluster, position, recorder, spawned, (minX, maxX, minZ, maxZ), random);

            if (!hasSpawned)
                continue;

            return true;
        }

        return false;
    }

    private bool SpawnNewClusterWithStrategy(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        (float MinX, float MaxX, float MinZ, float MaxZ) borders, XoShiRo128starstar random)
    {
        switch (cluster.SpawnStrategy)
        {
            case TerrainSpawnStrategy.Single:
                spawned.Add(SpawnTerrainCluster(cluster, position, recorder, random));
                return true;
            case TerrainSpawnStrategy.Row:
                return SpawnRowCluster(cluster, position, recorder, spawned, borders, random);
            case TerrainSpawnStrategy.Vent:
                return SpawnVentCluster(cluster, position, recorder, spawned, borders, random);
        }

        return false;
    }

    private bool SpawnRowCluster(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 position, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        (float MinX, float MaxX, float MinZ, float MaxZ) borders, XoShiRo128starstar random)
    {
        var (minX, maxX, minZ, maxZ) = borders;
        var offsetDirection = random.Next(-cluster.MaxPossibleChunkRadius, cluster.MaxPossibleChunkRadius);
        var stopAddingNewChunks = false;
        var chunksPositions = new List<Vector3>();

        var numberOfChunks = random.Next(cluster.MinChunks, cluster.MaxChunks + 1);
        for (var i = 1; i < numberOfChunks; ++i)
        {
            position = new Vector3(position.X, position.Y, position.Z);

            if (position.X < minX || position.X > maxX || position.Z < minZ || position.Z > maxZ)
            {
                break;
            }

            foreach (var alreadySpawned in spawned)
            {
                var distanceSquared = position.DistanceSquaredTo(alreadySpawned.CenterPosition);
                if (distanceSquared < MathUtils.Square(alreadySpawned.OverlapRadius + cluster.OverlapRadius))
                {
                    stopAddingNewChunks = true;
                    break;
                }
            }

            if (stopAddingNewChunks)
                break;

            chunksPositions.Add(position);

            var xOffsetVariation = random.Next(-5, 5);
            var shift = Math.Clamp(offsetDirection + xOffsetVariation, -cluster.MaxPossibleChunkRadius,
                cluster.MaxPossibleChunkRadius);

            position.X += shift;
            position.Z += (float)Math.Sqrt(Math.Pow(cluster.MaxPossibleChunkRadius, 2) - Math.Pow(shift, 2));
        }

        if (chunksPositions.Count < cluster.MinChunks)
            return false;
        
        var groupSpawnData = new GroupSpawnData(chunkGroupPosition, chunksPositions, radius * (layers + 2),
            nextGroupId++,
            cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(chunkGroupPosition,
            [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));

        // foreach (var chunkPosition in chunksPositions)
        // {
        //     spawned.Add(SpawnTerrainCluster(cluster, chunkPosition, recorder, random));
        // }

        return true;
    }

    private bool SpawnVentCluster(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 chunkGroupPosition, CommandBuffer recorder, List<SpawnedTerrainCluster> spawned,
        (float MinX, float MaxX, float MinZ, float MaxZ) borders, XoShiRo128starstar random)
    {
        // var (minX, maxX, minZ, maxZ) = borders;
        var radius = cluster.MaxPossibleChunkRadius;
        var layers = random.Next(2, 5);

        cluster.OverlapRadius = radius * (layers + 2);

        foreach (var alreadySpawned in spawned)
        {
            var distanceSquared = chunkGroupPosition.DistanceSquaredTo(alreadySpawned.CenterPosition);
            if (distanceSquared < MathUtils.Square(alreadySpawned.OverlapRadius + cluster.OverlapRadius))
            {
                return false;
            }
        }

        var chunksPositions = GetVentTerrainPositions(cluster, chunkGroupPosition, random, layers);
        var groupSpawnData = new GroupSpawnData(chunkGroupPosition, chunksPositions, radius * (layers + 2),
            nextGroupId++,
            cluster.TerrainGroups.GetItemByIndex(0).Chunks);
        var clusterSpawnData = new ClusterSpawnData(chunkGroupPosition,
            [groupSpawnData]);

        spawned.Add(SpawnTerrainCluster(clusterSpawnData, recorder, random));

        return true;
    }

    private List<Vector3> GetVentTerrainPositions(TerrainConfiguration.TerrainClusterConfiguration cluster,
        Vector3 chunkGroupPosition,
        XoShiRo128starstar random, int layers)
    {
        var segments = 6;
        var radius = cluster.MaxPossibleChunkRadius;
        var yLevel = 10 * layers - 25;

        var chunksPositions = new List<Vector3>();

        for (var j = 0; j < layers; ++j)
        {
            for (var i = 0; i < segments; ++i)
            {
                var angle = i * MathF.Tau / segments;
                var x = chunkGroupPosition.X + MathF.Cos(angle) * radius + random.Next(-2, 2);
                var z = chunkGroupPosition.Z + MathF.Sin(angle) * radius + random.Next(-2, 2);
                var position = new Vector3(x, yLevel + random.Next(-1, 1), z);
                chunksPositions.Add(position);
            }

            yLevel -= 10;
            segments += 6;
            radius += cluster.MaxPossibleChunkRadius;
        }

        return chunksPositions;
    }

    // private SpawnedTerrainCluster SpawnTerrainCluster(
    //     TerrainConfiguration.TerrainClusterConfiguration clusterConfiguration, Vector3 chunkPosition,
    //     CommandBuffer recorder, XoShiRo128starstar random)
    // {
    //     return SpawnTerrainCluster(clusterConfiguration, chunkPosition, [chunkPosition],
    //         [[chunkPosition]], recorder, random);
    // }
    //
    // private SpawnedTerrainCluster SpawnTerrainCluster(
    //     TerrainConfiguration.TerrainClusterConfiguration clusterConfiguration, Vector3 groupPosition,
    //     Vector3[] chunkPositions,
    //     CommandBuffer recorder, XoShiRo128starstar random)
    // {
    //     return SpawnTerrainCluster(clusterConfiguration, groupPosition, [groupPosition],
    //         [chunkPositions], recorder, random);
    // }

    private SpawnedTerrainCluster SpawnTerrainCluster(ClusterSpawnData clusterSpawnData, CommandBuffer recorder, XoShiRo128starstar random)
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

        return new SpawnedTerrainCluster(chunkPosition,
            clusterConfiguration.MaxPossibleChunkRadius, groupData,
            clusterConfiguration.OverlapRadius);
    }

    private SpawnedTerrainGroup SpawnTerrainGroup(GroupSpawnData groupSpawnData, CommandBuffer recorder, XoShiRo128starstar random)
    {
        var groupId = nextGroupId++;
        var data = new SpawnedTerrainGroup(groupSpawnData.Position,
            groupSpawnData.Radius,
            groupId);

        foreach (var position in groupSpawnData.ChunksPositions)
        {
            var rotation = new Quaternion(Vector3.Up, random.NextSingle() * MathF.Tau);
            var chunk = groupSpawnData.Chunks.GetItemByIndex(random.Next(groupSpawnData.Chunks.Count));
            var yOffset = new Vector3(0, random.NextSingle() * Constants.TERRAIN_HEIGHT_RANDOMNESS, 0);

            SpawnHelpers.SpawnMicrobeTerrainWithoutFinalizing(recorder, worldSimulation,
                position + rotation * chunk.RelativePosition + yOffset,
                rotation, chunk, groupId, random);

            data.ExpectedMemberCount += 1;
        }

        return data;
    }

    private class GroupSpawnData(Vector3 position, List<Vector3> chunksPositions, float radius, uint groupId,
        List<TerrainConfiguration.TerrainChunkConfiguration> chunks)
    {
        public Vector3 Position = position;
        public List<Vector3> ChunksPositions = chunksPositions;
        public float Radius = radius;
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
