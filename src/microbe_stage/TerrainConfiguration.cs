using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using SharedBase.Archive;
using ThriveScriptsShared;

public enum TerrainSpawnStrategy
{
    Single,
    Vent,
}

/// <summary>
///   Configures how microbe terrain is spawned for a patch
/// </summary>
public class TerrainConfiguration : RegistryType
{
    public List<TerrainClusterConfiguration> PotentialClusters = new();

    public int MinClusters;
    public int MaxClusters;

    private int totalChance;

    [JsonIgnore]
    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.TerrainConfiguration;

    public static object ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        return SimulationParameters.Instance.GetTerrainConfigurationForBiome(ReadInternalName(reader, version));
    }

    public override void Check(string name)
    {
        if (PotentialClusters == null! || PotentialClusters.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Terrain clusters is empty");
        }

        if (MinClusters < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Min clusters must be at least 0");

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

    public override void ApplyTranslations()
    {
    }

    public override string ToString()
    {
        return $"{InternalName} Terrain";
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
        public readonly Quaternion DefaultRotation = Quaternion.Identity;

        [JsonProperty]
        public readonly bool RandomizeRotation;

        public void Check(string name)
        {
            if (Radius <= 0.5f)
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain chunk radius is unset");

            if (Radius is > 0.5f * Constants.TERRAIN_GRID_SIZE_X or > 0.5f * Constants.TERRAIN_GRID_SIZE_Z)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Terrain chunk is so big it's not going to fit");
            }

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
        public readonly bool RandomizeRotation;

        public float MaxPossibleChunkRadius;

        public void Check(string name)
        {
            if (Chunks.Count < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain chunks are empty");
            }

            foreach (var chunk in Chunks)
            {
                chunk.Check(name);

                if (chunk.Radius > MaxPossibleChunkRadius)
                {
                    MaxPossibleChunkRadius = chunk.Radius;
                }
            }
        }
    }

    public class TerrainClusterConfiguration
    {
        [JsonProperty]
        public readonly List<TerrainGroupConfiguration> TerrainGroups = new();

        [JsonProperty]
        public readonly int RelativeChance;

        [JsonProperty]
        public readonly bool RandomizeRotation;

        /// <summary>
        ///   When true, the terrain will slide around other terrain to fit and spawn.
        ///   If false, only exact positions are tested, and even if it doesn't fit, no adjustment will be done.
        /// </summary>
        [JsonProperty]
        public readonly bool SlideToFit = true;

        [JsonProperty]
        public readonly TerrainSpawnStrategy SpawnStrategy = TerrainSpawnStrategy.Single;

        [JsonProperty]
        public readonly int MinChunks = 5;

        [JsonProperty]
        public readonly int MaxChunks = 10;

        public void Check(string name)
        {
            if (TerrainGroups.Count < 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Terrain groups is empty");
            }

            if (RelativeChance < 1)
                throw new InvalidRegistryDataException(name, GetType().Name, "RelativeChance must be above 0");

            foreach (var group in TerrainGroups)
            {
                group.Check(name);
            }
        }
    }
}
