using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   The conditions of a biome that can change. This is a separate class to make serialization work regarding the biome
/// </summary>
[UseThriveSerializer]
public class BiomeConditions : ICloneable, ISaveLoadable
{
    // TODO: make this also a property / private
    public Dictionary<string, ChunkConfiguration> Chunks = null!;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> compounds;

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> currentCompoundAmounts = new();

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> averageCompoundAmounts = new();

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> maximumCompoundAmounts = new();

    [JsonProperty]
    private Dictionary<Compound, BiomeCompoundProperties> minimumCompoundAmounts = new();

    [JsonConstructor]
    public BiomeConditions(Dictionary<Compound, BiomeCompoundProperties> compounds)
    {
        this.compounds = compounds;
    }

    public BiomeConditions(Dictionary<Compound, BiomeCompoundProperties> compounds,
        Dictionary<Compound, BiomeCompoundProperties> currentCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties> averageCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties> maximumCompoundAmounts,
        Dictionary<Compound, BiomeCompoundProperties> minimumCompoundAmounts) : this(compounds)
    {
        this.currentCompoundAmounts = currentCompoundAmounts;
        this.averageCompoundAmounts = averageCompoundAmounts;
        this.maximumCompoundAmounts = maximumCompoundAmounts;
        this.minimumCompoundAmounts = minimumCompoundAmounts;
    }

    /// <summary>
    ///   The compound amounts that change in realtime during gameplay
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> CurrentCompoundAmounts =>
        new DictionaryWithFallback<Compound, BiomeCompoundProperties>(currentCompoundAmounts, compounds);

    /// <summary>
    ///   Average compounds over an in-game day
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> AverageCompounds =>
        new DictionaryWithFallback<Compound, BiomeCompoundProperties>(averageCompoundAmounts, compounds);

    /// <summary>
    ///   Maximum compounds during an in-game day
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> MaximumCompounds =>
        new DictionaryWithFallback<Compound, BiomeCompoundProperties>(maximumCompoundAmounts, compounds);

    /// <summary>
    ///   Minimum compounds during an in-game day
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> MinimumCompounds =>
        new DictionaryWithFallback<Compound, BiomeCompoundProperties>(minimumCompoundAmounts, compounds);

    /// <summary>
    ///   The normal, large timescale compound amounts
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     I couldn't come up with a better name so this is named unimaginatively like this - hhyyrylainen
    ///   </para>
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<Compound, BiomeCompoundProperties> Compounds => compounds;

    /// <summary>
    ///   Allows access to changing the compound values in the biome permanently. Should only be used by auto-evo or
    ///   map generator.
    /// </summary>
    [JsonIgnore]
    public IDictionary<Compound, BiomeCompoundProperties> ChangeableCompounds => compounds;

    /// <summary>
    ///   Returns a new dictionary where <see cref="Compounds"/> is combined with compounds contained in
    ///   <see cref="Chunks"/>.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<Compound, BiomeCompoundProperties> CombinedCompounds
    {
        get
        {
            var result = new Dictionary<Compound, BiomeCompoundProperties>(compounds);

            foreach (var chunk in Chunks.Values)
            {
                if (chunk.Compounds == null)
                    continue;

                foreach (var compound in chunk.Compounds)
                {
                    compounds.TryGetValue(compound.Key, out BiomeCompoundProperties properties);
                    properties.Amount += compound.Value.Amount;
                    result[compound.Key] = properties;
                }
            }

            return result;
        }
    }

    public BiomeCompoundProperties GetCompound(Compound compound, CompoundAmountType amountType)
    {
        if (TryGetCompound(compound, amountType, out var result))
        {
            return result;
        }

        throw new KeyNotFoundException("Compound type not found in BiomeConditions");
    }

    public bool TryGetCompound(Compound compound, CompoundAmountType amountType, out BiomeCompoundProperties result)
    {
        switch (amountType)
        {
            case CompoundAmountType.Current:
                return CurrentCompoundAmounts.TryGetValue(compound, out result);
            case CompoundAmountType.Maximum:
                return MaximumCompounds.TryGetValue(compound, out result);
            case CompoundAmountType.Average:
                return AverageCompounds.TryGetValue(compound, out result);
            case CompoundAmountType.Biome:
                return Compounds.TryGetValue(compound, out result);
            case CompoundAmountType.Template:
                throw new NotSupportedException("BiomeConditions doesn't have access to template");
            default:
                throw new ArgumentOutOfRangeException(nameof(amountType), amountType, null);
        }
    }

    public void Check(string name)
    {
        if (compounds == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Compounds missing");
        }

        if (Chunks == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Chunks missing");
        }

        foreach (var compound in compounds)
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

            if (string.IsNullOrEmpty(chunk.Value.Name))
            {
                throw new InvalidRegistryDataException(name, GetType().Name, "Missing name for chunk type");
            }
        }
    }

    public void Resolve(SimulationParameters parameters)
    {
        LoadChunkScenes();
    }

    public void FinishLoading(ISaveContext? context)
    {
        LoadChunkScenes();
    }

    public object Clone()
    {
        // Shallow cloning is enough here thanks to us using value types (structs) as the dictionary values
        var result = new BiomeConditions(compounds = compounds.CloneShallow(), currentCompoundAmounts.CloneShallow(),
            averageCompoundAmounts.CloneShallow(), maximumCompoundAmounts.CloneShallow(),
            minimumCompoundAmounts.CloneShallow())
        {
            Chunks = Chunks.CloneShallow(),
        };

        return result;
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
