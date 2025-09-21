﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class UpwellingEvent : IWorldEffect
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

    private static readonly Dictionary<BiomeType, float> SmallChunksDensityMultipliers = new()
    {
        { BiomeType.IceShelf, 30.0f },
        { BiomeType.Epipelagic, 30.0f },
        { BiomeType.Mesopelagic, 15.0f },
        { BiomeType.Bathypelagic, 8.0f },
        { BiomeType.Abyssopelagic, 2.0f },
        { BiomeType.Seafloor, 1.0f },
    };

    private static readonly Dictionary<BiomeType, float> BigChunksDensityMultipliers = new()
    {
        { BiomeType.IceShelf, 2.0f },
        { BiomeType.Epipelagic, 2.0f },
        { BiomeType.Mesopelagic, 3.0f },
        { BiomeType.Bathypelagic, 7.0f },
        { BiomeType.Abyssopelagic, 11.0f },
        { BiomeType.Seafloor, 17.0f },
    };

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

    public UpwellingEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
    public UpwellingEvent(GameWorld targetWorld, XoShiRo256starstar random)
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

    private float GetChanceOfTriggering(double currentGeneration)
    {
        return PatchEventUtils.GetChanceOfTriggering(currentGeneration, Constants.UPWELLING_CHANCE_DIMINISH_DURATION,
            Constants.UPWELLING_INITIAL_CHANCE, Constants.UPWELLING_FINAL_CHANCE,
            targetWorld.WorldSettings.GeologicalActivity);
    }

    private void TriggerEvents(double elapsed)
    {
        foreach (var region in targetWorld.Map.Regions.Values)
        {
            if (!CanTriggerEvent(region, elapsed))
                continue;

            var duration = GetEventDuration();
            var affectedCompounds =
                PatchEventUtils.GetAffectedCompounds(random, Constants.UPWELLING_CHANCE_OF_AFFECTING_ANOTHER_COMPOUND);

            if (affectedCompounds.Count == 0)
                continue;

            foreach (var patch in region.Patches)
            {
                if (!patch.IsOceanicPatch())
                    continue;

                eventDurationsInPatches[patch.ID] = duration;
                affectedCompoundsInPatches[patch.ID] = affectedCompounds;
            }
        }
    }

    private int GetEventDuration()
    {
        return random.Next(Constants.UPWELLING_MIN_DURATION, Constants.UPWELLING_MAX_DURATION + 1);
    }

    private bool CanTriggerEvent(PatchRegion region, double elapsed)
    {
        var canTrigger = random.NextFloat() < GetChanceOfTriggering(elapsed);

        if (!canTrigger)
            return false;

        bool hasOceanicPatch = false;
        foreach (var patch in region.Patches)
        {
            if (patch.IsOceanicPatch())
            {
                hasOceanicPatch = true;
                if (patch.ActivePatchEvents.ContainsKey(PatchEventTypes.CurrentDilution) ||
                    patch.ActivePatchEvents.ContainsKey(PatchEventTypes.Upwelling))
                {
                    return false;
                }
            }
        }

        return hasOceanicPatch;
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
                patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.Upwelling);
                continue;
            }

            var affectedCompounds = affectedCompoundsInPatches[patchId];
            var tooltip = PatchEventUtils.BuildCustomTooltip("EVENT_UPWELLING_TOOLTIP",
                affectedCompounds);

            foreach (var compound in affectedCompounds)
            {
                var compoundDefinition = SimulationParameters.Instance.GetCompoundDefinition(compound);
                if (patch.Biome.ChangeableCompounds.TryGetValue(compound, out var compoundLevel))
                {
                    if (!compoundDefinition.IsEnvironmental)
                    {
                        // ammonia, hydrogensulfide, phosphates
                        compoundLevel.Amount = compoundLevel.Amount == 0 ? 90000 : compoundLevel.Amount;
                        compoundChanges[compound] =
                            Constants.UPWELLING_DILUTION_COMPOUND_CHANGE * random.Next(0.8f, 1.2f);
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
            }

            if (compoundChanges.Count > 0)
            {
                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, compoundChanges, cloudSizes);

                var patchProperties = new PatchEventProperties
                {
                    CustomTooltip = tooltip,
                };

                patch.CurrentSnapshot.ActivePatchEvents[PatchEventTypes.Upwelling] = patchProperties;
            }

            compoundChanges.Clear();
            cloudSizes.Clear();
        }
    }

    private void AddChunks(Patch patch, Compound compound)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForChunks);

        ApplyChunksConfiguration(patch, templateBiome, SmallChunks, SmallChunksDensityMultipliers, compound);
        ApplyChunksConfiguration(patch, templateBiome, BigChunks, BigChunksDensityMultipliers, compound);
    }

    private void ApplyChunksConfiguration(Patch patch, Biome templateBiome, Dictionary<Compound, string[]> chunkGroup,
        Dictionary<BiomeType, float> chunkDensityMultipliers,
        Compound compound)
    {
        PatchEventUtils.ApplyChunksConfiguration(patch, templateBiome, chunkGroup, chunkDensityMultipliers, compound,
            random, addChunks: true);
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");
    }
}
