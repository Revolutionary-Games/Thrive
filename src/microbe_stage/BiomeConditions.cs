using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   The conditions of a biome that can change. This is a separate class to make serialization work regarding the biome
/// </summary>
public class BiomeConditions : ICloneable, ISaveLoadable
{
    public float AverageTemperature;
    public Dictionary<string, EnvironmentalCompoundProperties> Compounds;
    public Dictionary<string, ChunkConfiguration> Chunks;

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
    }

    public void Resolve(SimulationParameters parameters)
    {
        _ = parameters;

        LoadChunkScenes();
    }

    public object Clone()
    {
        var result = new BiomeConditions()
        {
            AverageTemperature = AverageTemperature,
            Compounds = new Dictionary<string, EnvironmentalCompoundProperties>(Compounds.Count),
            Chunks = new Dictionary<string, ChunkConfiguration>(Chunks.Count),
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

    public void FinishLoading(ISaveContext context)
    {
        LoadChunkScenes();
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
