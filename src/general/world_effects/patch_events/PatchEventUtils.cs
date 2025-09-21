using System;
using System.Collections.Generic;
using Xoshiro.PRNG64;

public static class PatchEventUtils
{
    private static readonly Compound[] CompoundsToAffect =
    [
        Compound.Ammonia,
        Compound.Phosphates,
        Compound.Nitrogen,
        Compound.Hydrogensulfide,
        Compound.Iron,
    ];

    public static void ApplyChunksConfiguration(Patch patch, Biome templateBiome,
        Dictionary<Compound, string[]> chunkGroup, Dictionary<BiomeType, float>? densityMultipliers,
        Compound compound, XoShiRo256starstar random, bool addChunks, float minMultiplier = 1f,
        float maxMultiplier = 1f)
    {
        if (!chunkGroup.TryGetValue(compound, out var chunkConfigurations))
            return;

        foreach (var configuration in chunkConfigurations)
        {
            var chunkConfiguration = templateBiome.Conditions.Chunks[configuration];
            var multiplier = densityMultipliers?.TryGetValue(patch.BiomeType, out var baseMultiplier) ?? false ?
                baseMultiplier * random.Next(0.8f, 1.2f) :
                random.Next(minMultiplier, maxMultiplier);

            chunkConfiguration.Density *= multiplier;

            if (addChunks)
            {
                if (patch.Biome.Chunks.TryGetValue(configuration, out var existingChunkConfiguration))
                {
                    existingChunkConfiguration.Density += chunkConfiguration.Density;
                    patch.Biome.Chunks[configuration] = existingChunkConfiguration;
                }
                else
                {
                    patch.Biome.Chunks[configuration] = chunkConfiguration;
                }
            }
            else
            {
                if (!patch.Biome.Chunks.TryGetValue(configuration, out var existingChunkConfiguration))
                    continue;

                existingChunkConfiguration.Density = Math.Max(0.0f,
                    existingChunkConfiguration.Density - chunkConfiguration.Density);
                patch.Biome.Chunks[configuration] = existingChunkConfiguration;
            }
        }
    }

    public static List<Compound> GetAffectedCompounds(XoShiRo256starstar random,
        float chanceOfAffectingCompound)
    {
        var pool = (Compound[])CompoundsToAffect.Clone();
        var poolCount = CompoundsToAffect.Length;
        var selectedCompounds = new List<Compound>(poolCount);
        while (poolCount > 0)
        {
            // Select a random index from the pool
            var randomIndex = random.Next(0, poolCount);
            var compound = pool[randomIndex];

            // Remove selected compound from pool by overwriting with last and reducing count
            pool[randomIndex] = pool[poolCount - 1];
            poolCount--;

            selectedCompounds.Add(compound);

            if (random.NextFloat() >
                GetChanceOfAffectAnotherCompound(chanceOfAffectingCompound, selectedCompounds.Count))
                break;
        }

        return selectedCompounds;
    }

    // Builds a tooltip string for affected compounds
    public static string BuildCustomTooltip(string baseTooltip, List<Compound> affectedCompounds)
    {
        var builder = new LocalizedStringBuilder(256);
        builder.Append(new LocalizedString(baseTooltip));
        builder.Append(' ');
        for (var i = 0; i < affectedCompounds.Count; ++i)
        {
            builder.Append(SimulationParameters.Instance.GetCompoundDefinition(affectedCompounds[i]).Name);
            if (i < affectedCompounds.Count - 1)
                builder.Append(", ");
        }

        return builder.ToString();
    }

    private static float GetChanceOfAffectAnotherCompound(float chance, int numberOfAffectedCompounds)
    {
        return (float)Math.Pow(chance, numberOfAffectedCompounds);
    }
}
