using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Reduces amount of iron (down to a minimum) when oxygen goes up to simulate oxidation making iron less available
/// </summary>
public class IronOxidationEffect : IWorldEffect
{
    [JsonProperty]
    private GameWorld targetWorld;

    public IronOxidationEffect(GameWorld targetWorld)
    {
        this.targetWorld = targetWorld;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        var chunkTypes = new List<string>();

        // Iron is only in chunks so we don't need to handle clouds
        foreach (var key in targetWorld.Map.Patches.Keys)
        {
            var patch = targetWorld.Map.Patches[key];

            // Skip patches that don't need handling
            if (!patch.Biome.ChangeableCompounds.TryGetValue(Compound.Oxygen,
                    out BiomeCompoundProperties oxygenAmount) ||
                oxygenAmount.Ambient <= Constants.MIN_OXYGEN_BEFORE_OXIDATION)
            {
                continue;
            }

            // Potentially a patch with oxygen and iron
            // This effect is about oxygen eating the chunks so no species are involved in the calculation

            // The configurations are structs so modifying them is slightly complex process
            chunkTypes.Clear();
            chunkTypes.AddRange(patch.Biome.Chunks.Keys);

            foreach (var chunk in chunkTypes)
            {
                var chunkConfiguration = patch.Biome.Chunks[chunk];

                // Skip chunks that don't spawn
                if (chunkConfiguration.Density <= 0)
                    continue;

                bool isIronChunk = false;

                // Detect if the chunk has an above-zero amount of iron
                if (chunkConfiguration.Compounds != null)
                {
                    foreach (var chunkCompound in chunkConfiguration.Compounds)
                    {
                        if (chunkCompound.Key == Compound.Iron && chunkCompound.Value.Amount > 0)
                        {
                            isIronChunk = true;
                            break;
                        }
                    }
                }

                if (!isIronChunk)
                    continue;

                // Found an iron chunk! Reduce its spawn density
                // TODO: should we have an effect that brings back iron in some situations?
                var newValue = chunkConfiguration.Density *
                    (1 - oxygenAmount.Ambient * Constants.CHUNK_OXIDATION_SPEED);

                // Apply a minimum floor
                float minimum = 0;
                if (patch.BiomeTemplate.Conditions.Chunks.TryGetValue(chunk, out var templateChunk))
                {
                    minimum = templateChunk.Density * Constants.MIN_IRON_DENSITY_OXIDATION;
                }
                else
                {
                    GD.PrintErr("Couldn't find original spawn density of chunk for oxidation: " + chunk);
                }

                chunkConfiguration.Density = Math.Max(newValue, minimum);

                patch.Biome.Chunks[chunk] = chunkConfiguration;
            }
        }
    }
}
