using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Limits resources allowed from <see cref="IBiomeConditions"/> based on <see cref="ResourceLimitingMode"/>.
///   Note that this allocates a bit of memory so this is mostly good to use for editor usage and not auto-evo or
///   realtime use.
/// </summary>
public class BiomeResourceLimiterAdapter : IBiomeConditions
{
    private readonly ResourceLimitingMode limitingMode;
    private readonly IBiomeConditions baseConditions;
    private readonly Lazy<Dictionary<string, ChunkConfiguration>> filteredChunks;

    public BiomeResourceLimiterAdapter(ResourceLimitingMode limitingMode, IBiomeConditions baseConditions)
    {
        this.limitingMode = limitingMode;
        this.baseConditions = baseConditions;

        filteredChunks = new Lazy<Dictionary<string, ChunkConfiguration>>(FilterChunks);
    }

    [JsonIgnore]
    public Dictionary<string, ChunkConfiguration> Chunks => filteredChunks.Value;

    public BiomeCompoundProperties GetCompound(Compound compound, CompoundAmountType amountType)
    {
        if (!AllowCompound(compound))
        {
            // TODO: should this throw an exception? That might complicate some code that assumes some compounds are
            // always present
            return default(BiomeCompoundProperties);
        }

        return baseConditions.GetCompound(compound, amountType);
    }

    public bool TryGetCompound(Compound compound, CompoundAmountType amountType, out BiomeCompoundProperties result)
    {
        if (!AllowCompound(compound))
        {
            result = default(BiomeCompoundProperties);
            return false;
        }

        return baseConditions.TryGetCompound(compound, amountType, out result);
    }

    public IEnumerable<Compound> GetAmbientCompoundsThatVary()
    {
        foreach (var compound in baseConditions.GetAmbientCompoundsThatVary())
        {
            if (AllowCompound(compound))
                yield return compound;
        }
    }

    public bool HasCompoundsThatVary()
    {
        // TODO: does this need to be filtered?
        return baseConditions.HasCompoundsThatVary();
    }

    public bool IsVaryingCompound(Compound compound)
    {
        return AllowCompound(compound) && baseConditions.IsVaryingCompound(compound);
    }

    public bool AllowCompound(Compound compound)
    {
        switch (limitingMode)
        {
            case ResourceLimitingMode.AllResources:
                return true;
            case ResourceLimitingMode.WithoutGlucose:
                return compound != Compound.Glucose;
            case ResourceLimitingMode.WithoutIron:
                return compound != Compound.Iron;
            case ResourceLimitingMode.WithoutHydrogenSulfide:
                return compound != Compound.Hydrogensulfide;
            case ResourceLimitingMode.NoExternalResources:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(limitingMode), "unimplemented limiting mode");
        }
    }

    private Dictionary<string, ChunkConfiguration> FilterChunks()
    {
        Dictionary<string, ChunkConfiguration> chunks = new();

        foreach (var chunk in baseConditions.Chunks)
        {
            // It's probably not important to copy chunks that don't have compounds
            if (chunk.Value.Compounds == null)
                continue;

            // Chunk is let through
            chunks[chunk.Key] = chunk.Value;
        }

        return chunks;
    }
}
