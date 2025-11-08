using System.Collections.Generic;
using System.Linq;
using SharedBase.Archive;
using Xoshiro.PRNG64;

public class CurrentDilutionEvent : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

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
        { BiomeType.Mesopelagic, 18.0f },
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

    private readonly XoShiRo256starstar random;

    private readonly Dictionary<int, int> eventDurationsInPatches = new();

    private readonly Dictionary<int, List<Compound>> affectedCompoundsInPatches = new();

    private GameWorld targetWorld;

    public CurrentDilutionEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    public CurrentDilutionEvent(GameWorld targetWorld, XoShiRo256starstar random,
        Dictionary<int, int> eventDurationsInPatches, Dictionary<int, List<Compound>> affectedCompoundsInPatches)
    {
        this.targetWorld = targetWorld;
        this.random = random;
        this.eventDurationsInPatches = eventDurationsInPatches;
        this.affectedCompoundsInPatches = affectedCompoundsInPatches;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.CurrentDilutionEvent;
    public bool CanBeReferencedInArchive => false;

    public static CurrentDilutionEvent ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var targetWorld = reader.ReadObject<GameWorld>();
        var random = reader.ReadObject<XoShiRo256starstar>();
        var eventDurations = reader.ReadObject<Dictionary<int, int>>();
        var affectedCompounds = reader.ReadObject<Dictionary<int, List<Compound>>>();

        return new CurrentDilutionEvent(targetWorld, random, eventDurations, affectedCompounds);
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
        writer.WriteAnyRegisteredValueAsObject(random);
        writer.WriteObject(eventDurationsInPatches);
        writer.WriteObject(affectedCompoundsInPatches);
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        TriggerEventOnContinent(elapsed);
        TriggerEventInOcean(elapsed);
        ChangePatchProperties();
    }

    private float GetChanceOfTriggering(double currentGeneration)
    {
        return PatchEventUtils.GetChanceOfTriggering(currentGeneration,
            Constants.CURRENT_DILUTION_CHANCE_DIMINISH_DURATION, Constants.CURRENT_DILUTION_INITIAL_CHANCE,
            Constants.CURRENT_DILUTION_FINAL_CHANCE, targetWorld.WorldSettings.GeologicalActivity);
    }

    private void TriggerEventOnContinent(double elapsed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (!CanTriggerEventOnContinent(patch, elapsed))
                continue;

            var duration = GetEventDuration();
            var affectedCompounds = GetAffectedCompounds();

            if (affectedCompounds.Count <= 0)
                continue;

            eventDurationsInPatches[patch.ID] = duration;
            affectedCompoundsInPatches[patch.ID] = affectedCompounds;
            LogEvent(patch);
        }
    }

    private void TriggerEventInOcean(double elapsed)
    {
        foreach (var region in targetWorld.Map.Regions.Values)
        {
            if (!CanTriggerEventInOcean(region, elapsed))
                continue;

            var duration = GetEventDuration();
            var affectedCompounds = GetAffectedCompounds();
            foreach (var patch in region.Patches)
            {
                if (affectedCompounds.Count <= 0 || !patch.IsOceanicPatch())
                    continue;

                eventDurationsInPatches[patch.ID] = duration;
                affectedCompoundsInPatches[patch.ID] = affectedCompounds;
                LogEvent(patch);
            }
        }
    }

    private bool CanTriggerEventOnContinent(Patch patch, double elapsed)
    {
        return patch.IsContinentalPatch() && !eventDurationsInPatches.ContainsKey(patch.ID) &&
            !patch.ActivePatchEvents.ContainsKey(PatchEventTypes.Runoff) &&
            random.NextFloat() < GetChanceOfTriggering(elapsed);
    }

    private bool CanTriggerEventInOcean(PatchRegion region, double elapsed)
    {
        foreach (var patch in region.Patches)
        {
            if (patch.IsOceanicPatch())
            {
                if (!patch.ActivePatchEvents.ContainsKey(PatchEventTypes.Upwelling) &&
                    !eventDurationsInPatches.ContainsKey(patch.ID))
                {
                    return random.NextFloat() < GetChanceOfTriggering(elapsed);
                }

                return false;
            }
        }

        return false;
    }

    private void ChangePatchProperties()
    {
        foreach (var patchId in eventDurationsInPatches.Keys.ToList())
        {
            var patch = targetWorld.Map.Patches[patchId];
            eventDurationsInPatches[patchId] -= 1;

            if (eventDurationsInPatches[patchId] <= 0)
            {
                eventDurationsInPatches.Remove(patchId);
                affectedCompoundsInPatches.Remove(patchId);
                patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.CurrentDilution);
                continue;
            }

            var affectedCompounds = affectedCompoundsInPatches[patchId];
            var tooltip = PatchEventUtils.BuildCustomTooltip(new LocalizedString("CURRENT_DILUTION_TOOLTIP"),
                affectedCompounds);

            foreach (var compound in affectedCompounds)
            {
                var compoundDefinition = SimulationParameters.Instance.GetCompoundDefinition(compound);
                if (patch.Biome.ChangeableCompounds.TryGetValue(compound, out var compoundLevel))
                {
                    if (!compoundDefinition.IsEnvironmental)
                    {
                        // ammonia, hydrogensulfide, phosphates
                        compoundLevel.Amount = compoundLevel.Amount == 0 ? 0 : compoundLevel.Amount;
                        compoundChanges[compound] = Constants.CURRENT_DILUTION_COMPOUND_CHANGE;
                        cloudSizes[compound] = compoundLevel.Amount;
                    }
                    else
                    {
                        // nitrogen
                        compoundChanges[compound] = -0.1f;
                    }
                }

                // iron, hydrogensulfide, phosphates
                ReduceChunks(patch, compound);
            }

            if (compoundChanges.Count > 0)
            {
                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, compoundChanges, cloudSizes);

                var patchProperties = new PatchEventProperties
                {
                    CustomTooltip = tooltip,
                };

                patch.CurrentSnapshot.ActivePatchEvents[PatchEventTypes.CurrentDilution] = patchProperties;
            }

            compoundChanges.Clear();
            cloudSizes.Clear();
        }
    }

    /// <summary>
    ///   Gets chunks from event template patch and applies them to the patches.
    /// </summary>
    private void ReduceChunks(Patch patch, Compound compound)
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
            random, false, Constants.CURRENT_DILUTION_MIN_CHUNK_DENSITY_MULTIPLIER,
            Constants.CURRENT_DILUTION_MAX_CHUNK_DENSITY_MULTIPLIER);
    }

    private int GetEventDuration()
    {
        return random.Next(Constants.CURRENT_DILUTION_MIN_DURATION, Constants.CURRENT_DILUTION_MAX_DURATION + 1);
    }

    private List<Compound> GetAffectedCompounds()
    {
        return PatchEventUtils.GetAffectedCompounds(random,
            Constants.CURRENT_DILUTION_CHANCE_OF_AFFECTING_ANOTHER_COMPOUND);
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("CURRENT_DILUTION"),
            true, true, "CurrentDilution.svg");
    }
}
