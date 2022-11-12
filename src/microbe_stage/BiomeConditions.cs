using System;
using System.Collections.Generic;

/// <summary>
///   The conditions of a biome that can change. This is a separate class to make serialization work regarding the biome
/// </summary>
[UseThriveSerializer]
public class BiomeConditions : ICloneable, ISaveLoadable
{
    public Dictionary<Compound, EnvironmentalCompoundProperties> Compounds = null!;
    public Dictionary<string, ChunkConfiguration> Chunks = null!;

    public DaylightProperties? Sunlight;

    public void Check(string name)
    {
        if (Compounds == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compounds missing");
        }

        if (Chunks == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Chunks missing");
        }

        foreach (var compound in Compounds)
        {
            if (compound.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR is < 0 or > 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Density {compound.Value.Density} invalid for {compound.Key} " +
                    $"(scale factor is {Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR})");
            }
        }

        foreach (var chunk in Chunks)
        {
            if (chunk.Value.Density * Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR is < 0 or > 1)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    $"Density {chunk.Value.Density} invalid for {chunk.Key} " +
                    $"(scale factor is {Constants.CLOUD_SPAWN_DENSITY_SCALE_FACTOR})");
            }
        }
    }

    public void Resolve(SimulationParameters parameters)
    {
        LoadChunkScenes();
    }

    public object Clone()
    {
        var result = new BiomeConditions
        {
            Compounds = new Dictionary<Compound, EnvironmentalCompoundProperties>(Compounds.Count),
            Chunks = new Dictionary<string, ChunkConfiguration>(Chunks.Count),
            Sunlight = new DaylightProperties(
                Sunlight?.Maximum ?? 0.0f, Sunlight?.Current ?? 0.0f, Sunlight?.Average ?? 0.0f),
        };

        foreach (var entry in Compounds)
        {
            result.Compounds.Add(entry.Key, entry.Value);
        }

        foreach (var entry in Chunks)
        {
            result.Chunks.Add(entry.Key, entry.Value);
        }

        return result;
    }

    public void FinishLoading(ISaveContext? context)
    {
        LoadChunkScenes();
    }

    public void CreateSunlight(float ambient)
    {
        Sunlight = new DaylightProperties(ambient, ambient, ambient);
    }

    private void LoadChunkScenes()
    {
        foreach (var entry in Chunks)
        {
            foreach (var meshEntry in entry.Value.Meshes)
            {
                meshEntry.LoadScene();
            }
        }
    }
}
