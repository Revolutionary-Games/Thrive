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
        Compound compound, XoShiRo256starstar random, bool addChunks, float minMultiplier = 1.0f,
        float maxMultiplier = 1.0f)
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
            chunkConfiguration.Density *= chunkConfiguration.Compounds?.ContainsKey(Compound.Iron) == true ? 2.5f : 1;

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
    public static string BuildCustomTooltip(LocalizedString baseTooltip, List<Compound> affectedCompounds)
    {
        var builder = new LocalizedStringBuilder(256);

        builder.Append(baseTooltip);
        builder.Append(' ');

        for (var i = 0; i < affectedCompounds.Count; ++i)
        {
            builder.Append(SimulationParameters.Instance.GetCompoundDefinition(affectedCompounds[i]).Name);
            if (i < affectedCompounds.Count - 1)
                builder.Append(", ");
        }

        return builder.ToString();
    }

    public static float GetChanceOfTriggering(double currentGeneration, float diminishDuration, float initialChance,
        float finalChance, WorldGenerationSettings.GeologicalActivityEnum geologicalActivity)
    {
        if (currentGeneration >= diminishDuration)
            return finalChance;

        // Returns bigger chance for earlier generations, then linearly diminishes to a final chance
        var chance = ((diminishDuration - currentGeneration) * initialChance
            + currentGeneration * finalChance) / diminishDuration;

        var modifier = geologicalActivity switch
        {
            WorldGenerationSettings.GeologicalActivityEnum.Dormant => 0.8f,
            WorldGenerationSettings.GeologicalActivityEnum.Active => 1.2f,
            _ => 1.0f,
        };

        return Math.Min((float)chance * modifier, 0.9f);
    }

    private static float GetChanceOfAffectAnotherCompound(float chance, int numberOfAffectedCompounds)
    {
        if (numberOfAffectedCompounds <= 1)
            return 1.0f;

        // Each additional compound has a diminishing chance of being affected
        return (float)Math.Pow(chance, numberOfAffectedCompounds);
    }
}
