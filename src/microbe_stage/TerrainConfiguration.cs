using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Configures how microbe terrain is spawned for a patch
/// </summary>
public class TerrainConfiguration : IRegistryType
{
    public List<TerrainClusterConfiguration> PotentialClusters = new();

    public int MinClusters;
    public int MaxClusters;

    private int totalChance;

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (PotentialClusters == null! || PotentialClusters.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Terrain clusters is empty");
        }

        if (MinClusters < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Min clusters must be above 0");

        if (MaxClusters <= MinClusters)
            throw new InvalidRegistryDataException(name, GetType().Name, "Max clusters must be more than min clusters");

        totalChance = 0;

        foreach (var cluster in PotentialClusters)
        {
            cluster.Check(name);

            totalChance += cluster.RelativeChance;
        }
    }

    public TerrainClusterConfiguration GetRandomCluster(Random random)
    {
        var selected = random.Next(0, totalChance + 1);

        foreach (var cluster in PotentialClusters)
        {
            selected -= cluster.RelativeChance;

            if (selected <= 0)
                return cluster;
        }

        return PotentialClusters[^1];
    }

    public void ApplyTranslations()
    {
    }

    public class TerrainChunkConfiguration
    {
        [JsonProperty]
        public readonly VisualResourceIdentifier Visuals;

        [JsonProperty]
        public readonly string CollisionShapePath = null!;

        /// <summary>
        ///   How much 2D space this chunk takes when spawned. Used to avoid spawn overlaps.
        /// </summary>
        [JsonProperty]
        public readonly float Radius;

        [JsonProperty]
        public readonly Vector3 RelativePosition;

        [JsonProperty]
        public readonly Quaternion DefaultRotation = Quaternion.Identity;

        [JsonProperty]
        public readonly bool RandomizeRotation;

        public void Check(string name)
        {
            if (Radius <= 0.5f)
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain chunk radius is unset");

            if (string.IsNullOrEmpty(CollisionShapePath))
                throw new InvalidRegistryDataException(name, GetType().Name, "Collision shape path is empty");

            if (!DefaultRotation.IsNormalized())
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Terrain chunk rotation is not normalized");
            }

            if (Visuals <= VisualResourceIdentifier.Error)
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain chunk visuals is not valid");
        }
    }

    public class TerrainGroupConfiguration
    {
        [JsonProperty]
        public readonly List<TerrainChunkConfiguration> Chunks = new();

        [JsonProperty]
        public readonly Vector3 RelativePosition;

        public float Radius;

        public float OtherTerrainPreventionRadius;

        public void Check(string name)
        {
            if (Chunks.Count < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain chunks are empty");
            }

            Radius = 0;

            foreach (var chunk in Chunks)
            {
                chunk.Check(name);

                // Calculate overall radius
                var currentRadius = chunk.Radius + chunk.RelativePosition.Length();
                if (currentRadius > Radius)
                    Radius = currentRadius;
            }

            if (Radius < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Terrain calculated radius is less than 1");
            }

            // If other prevention radius is not set, set it automatically
            if (OtherTerrainPreventionRadius < 1)
                OtherTerrainPreventionRadius = Radius;
        }
    }

    public class TerrainClusterConfiguration
    {
        [JsonProperty]
        public readonly List<TerrainGroupConfiguration> TerrainGroups = new();

        [JsonProperty]
        public readonly int RelativeChance;

        public float OverallRadius;
        public float OverallOverlapRadius;

        public void Check(string name)
        {
            if (TerrainGroups.Count < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain groups is empty");
            }

            if (RelativeChance < 1)
                throw new InvalidRegistryDataException(name, GetType().Name, "RelativeChance must be above 0");

            OverallRadius = 0;
            OverallOverlapRadius = 0;

            foreach (var group in TerrainGroups)
            {
                group.Check(name);

                var groupPositionFactor = group.RelativePosition.Length();
                var currentRadius = groupPositionFactor + group.Radius;

                if (currentRadius > OverallRadius)
                    OverallRadius = currentRadius;

                var overlapRadius = groupPositionFactor + group.OtherTerrainPreventionRadius;
                if (overlapRadius > OverallOverlapRadius)
                    OverallOverlapRadius = overlapRadius;
            }
        }
    }
}
