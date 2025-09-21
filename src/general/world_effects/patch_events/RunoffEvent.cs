using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class RunoffEvent : IWorldEffect
{
    private const string TemplateBiomeForChunks = "patchEventTemplateBiome";

    private static readonly Dictionary<Compound, string[]> SmallChunks = new()
    {
        { Compound.Iron, ["ironSmallChunk"] },
        { Compound.Hydrogensulfide, ["sulfurSmallChunk"] },
        { Compound.Phosphates, ["phosphateSmallChunk"] },
    };

    private static readonly Dictionary<Compound, string[]> BigChunks = new()
    {
        { Compound.Iron, ["ironBigChunk"] },
        { Compound.Hydrogensulfide, ["sulfurMediumChunk", "sulfurLargeChunk"] },
        { Compound.Phosphates, ["phosphateBigChunk"] },
    };

    private static readonly Compound[] CompoundsToAffect =
    [
        Compound.Ammonia,
        Compound.Phosphates,
        Compound.Nitrogen,
        Compound.Hydrogensulfide,
        Compound.Iron,
    ];

    private readonly Dictionary<Compound, float> compoundChanges = new();
    private readonly Dictionary<Compound, float> cloudSizes = new();

    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private readonly Dictionary<int, int> eventDurationsInPatches = new();

    [JsonProperty]
    private readonly Dictionary<int, List<Compound>> affectedCompoundsInPatches = new();

    [JsonProperty]
    private GameWorld targetWorld;

    public RunoffEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
    public RunoffEvent(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        TriggerEvents(elapsed);
        ChangePatchProperties();
    }

    // Returns bigger chance for earlier generations, then linearly diminishes to a final chance
    private float GetChanceOfTriggering(double currentGeneration)
    {
        if (currentGeneration >= Constants.RUNOFF_CHANCE_DIMINISH_DURATION)
            return Constants.RUNOFF_FINAL_CHANCE;

        // Linear interpolation
        var chance = (float)(((Constants.RUNOFF_CHANCE_DIMINISH_DURATION - currentGeneration) *
                Constants.RUNOFF_INITIAL_CHANCE
                + currentGeneration * Constants.RUNOFF_FINAL_CHANCE) /
            Constants.RUNOFF_CHANCE_DIMINISH_DURATION);

        float modifier = targetWorld.WorldSettings.GeologicalActivity switch
        {
            WorldGenerationSettings.GeologicalActivityEnum.Dormant => 0.8f,
            WorldGenerationSettings.GeologicalActivityEnum.Active => 1.2f,
            _ => 1.0f,
        };

        return Math.Min(chance * modifier, 0.9f);
    }

    // Each additional compound has a diminishing chance of being affected
    private float GetChanceOfAffectAnotherCompound(int numberOfAffectedCompounds)
    {
        return (float)Math.Pow(Constants.RUNOFF_CHANCE_OF_AFFECTING_ANOTHER_COMPOUND, numberOfAffectedCompounds);
    }

    private void TriggerEvents(double elapsed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (!CanTriggerEvent(patch, elapsed))
                continue;

            var duration = GetEventDuration();
            var affectedCompounds = GetAffectedCompounds();

            if (affectedCompounds.Count == 0)
                continue;

            eventDurationsInPatches[patch.ID] = duration;
            affectedCompoundsInPatches[patch.ID] = affectedCompounds;
        }
    }

    private bool CanTriggerEvent(Patch patch, double elapsed)
    {
        return patch.IsContinentalPatch() && !eventDurationsInPatches.ContainsKey(patch.ID) &&
            random.NextFloat() < GetChanceOfTriggering(elapsed) &&
            !patch.ActivePatchEvents.ContainsKey(PatchEventTypes.CurrentDilution);
    }

    private void ChangePatchProperties()
    {
        foreach (int patchId in eventDurationsInPatches.Keys.ToList())
        {
            var patch = targetWorld.Map.Patches[patchId];
            eventDurationsInPatches[patchId] -= 1;

            if (eventDurationsInPatches[patchId] <= 0)
            {
                eventDurationsInPatches.Remove(patchId);
                affectedCompoundsInPatches.Remove(patchId);
                patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.Runoff);
                continue;
            }

            var affectedCompounds = affectedCompoundsInPatches[patchId];
            var tooltipBuilder = new LocalizedStringBuilder(200);
            tooltipBuilder.Append(new LocalizedString("EVENT_RUNOFF_TOOLTIP"));
            tooltipBuilder.Append(' ');

            GD.Print("BEFORE RUNOFF EVENT\nAffected compounds:");
            foreach (var compound in affectedCompounds)
            {
                GD.Print(SimulationParameters.Instance.GetCompoundDefinition(compound).Name);
            }

            GD.Print();
            DisplayCompounds(patch, affectedCompounds);

            for (var i = 0; i < affectedCompounds.Count; ++i)
            {
                var compound = affectedCompounds[i];
                var compoundDefinition = SimulationParameters.Instance.GetCompoundDefinition(compound);
                if (patch.Biome.ChangeableCompounds.TryGetValue(compound, out var compoundLevel))
                {
                    if (!compoundDefinition.IsEnvironmental)
                    {
                        // ammonia, hydrogensulfide, phosphates
                        compoundLevel.Amount = compoundLevel.Amount == 0 ? 90000 : compoundLevel.Amount;
                        compoundChanges[compound] = Constants.RUNOFF_COMPOUND_CHANGE * random.Next(0.8f, 1.2f);
                        cloudSizes[compound] = compoundLevel.Amount;
                    }
                    else
                    {
                        // nitrogen
                        compoundChanges[compound] = 0.1f;
                    }
                }

                // iron, hydrogensulfide, phosphates
                AddChunks(patch, compound);

                tooltipBuilder.Append(compoundDefinition.Name);

                if (i < affectedCompounds.Count - 1)
                    tooltipBuilder.Append(", ");
            }

            if (compoundChanges.Count > 0)
            {
                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, compoundChanges, cloudSizes);

                var patchProperties = new PatchEventProperties
                {
                    CustomTooltip = tooltipBuilder.ToString(),
                };

                patch.CurrentSnapshot.ActivePatchEvents[PatchEventTypes.Runoff] = patchProperties;
            }

            GD.Print("AFTER RUNOFF EVENT\n");
            DisplayCompounds(patch, affectedCompounds);

            compoundChanges.Clear();
            cloudSizes.Clear();
        }
    }

    private void DisplayCompounds(Patch patch, List<Compound> compounds)
    {
        return;

        foreach (var compound in compounds)
        {
            var compoundDefinition = SimulationParameters.Instance.GetCompoundDefinition(compound);
            GD.Print(compoundDefinition.Name + ":");
            if (patch.Biome.ChangeableCompounds.TryGetValue(compound, out var compoundLevel))
            {
                GD.Print(" - " + compoundLevel);
            }
            else
            {
                GD.Print(" - not present");
            }

            var chunks =
                patch.Biome.Chunks.Where(configuration =>
                    configuration.Value.Compounds != null && configuration.Value.Compounds.ContainsKey(compound));

            GD.Print(" - chunks:");
            foreach (var chunk in chunks)
            {
                GD.Print($"     - Chunk '{chunk.Key}' density: {chunk.Value.Density}");
            }

            GD.Print();
        }

        GD.Print("-------------------");
        GD.Print();
    }

    /// <summary>
    ///   Gets chunks from event template patch and reduced their density in the patches.
    /// </summary>
    private void AddChunks(Patch patch, Compound compound)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForChunks);

        ApplyChunksConfiguration(patch, templateBiome, SmallChunks, compound);
        ApplyChunksConfiguration(patch, templateBiome, BigChunks, compound);
    }

    /// <summary>
    ///   Helper to reduce group of chunks in a patch using the provided configuration and multipliers.
    /// </summary>
    private void ApplyChunksConfiguration(Patch patch, Biome templateBiome, Dictionary<Compound, string[]> chunkGroup,
        Compound compound)
    {
        if (!chunkGroup.TryGetValue(compound, out var chunkConfigurations))
            return;

        foreach (var configuration in chunkConfigurations)
        {
            var chunkConfiguration = templateBiome.Conditions.Chunks[configuration];
            chunkConfiguration.Density *= random.Next(8.0f, 22.0f);

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
    }

    // /// <summary>
    // ///   Helper to reduce group of chunks in a patch using the provided configuration and multipliers.
    // /// </summary>
    // private void ApplyChunksConfiguration(Patch patch, Biome templateBiome, Dictionary<Compound, string[]> chunkGroup,
    //     Compound compound)
    // {
    //     if (chunkGroup.TryGetValue(compound, out var chunkConfigurations))
    //     {
    //         foreach (var configuration in chunkConfigurations)
    //         {
    //             GD.Print(configuration);
    //             var chunkConfiguration = templateBiome.Conditions.Chunks[configuration];
    //             chunkConfiguration.Density *= random.Next(8.0f, 22.0f);
    //
    //             if (patch.Biome.Chunks.TryGetValue(configuration, out var existingChunkConfiguration))
    //             {
    //                 GD.Print("Updating: " + configuration);
    //                 GD.Print(existingChunkConfiguration.Density, ", ", chunkConfiguration.Density);
    //                 existingChunkConfiguration.Density += chunkConfiguration.Density;
    //                 patch.Biome.Chunks[configuration] = existingChunkConfiguration;
    //                 GD.Print(existingChunkConfiguration.Density, ", ", patch.Biome.Chunks[configuration].Density);
    //             }
    //             else
    //             {
    //                 patch.Biome.Chunks[configuration] = chunkConfiguration;
    //             }
    //         }
    //     }
    // }

    private int GetEventDuration()
    {
        return random.Next(Constants.RUNOFF_MIN_DURATION, Constants.RUNOFF_MAX_DURATION + 1);
    }

    private List<Compound> GetAffectedCompounds()
    {
        var pool = (Compound[])CompoundsToAffect.Clone();
        int poolCount = CompoundsToAffect.Length;

        var selectedCompounds = new List<Compound>();

        while (poolCount > 0)
        {
            // Select a random index from the pool
            int randomIndex = random.Next(0, poolCount);
            var compound = pool[randomIndex];

            // Remove selected compound from pool by overwriting with last and reducing count
            pool[randomIndex] = pool[poolCount - 1];
            poolCount--;

            selectedCompounds.Add(compound);

            if (random.NextFloat() > GetChanceOfAffectAnotherCompound(selectedCompounds.Count))
                break;
        }

        return selectedCompounds;
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");
    }
}
