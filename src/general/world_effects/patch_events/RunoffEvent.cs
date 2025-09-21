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
        return PatchEventUtils.GetChanceOfTriggering(currentGeneration,
            Constants.RUNOFF_CHANCE_DIMINISH_DURATION,
            Constants.RUNOFF_INITIAL_CHANCE,
            Constants.RUNOFF_FINAL_CHANCE,
            targetWorld.WorldSettings.GeologicalActivity);
    }

    private void TriggerEvents(double elapsed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (!CanTriggerEvent(patch, elapsed))
                continue;

            var duration = GetEventDuration();
            var affectedCompounds =
                PatchEventUtils.GetAffectedCompounds(random, Constants.RUNOFF_CHANCE_OF_AFFECTING_ANOTHER_COMPOUND);

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
            var tooltip = PatchEventUtils.BuildCustomTooltip("EVENT_RUNOFF_TOOLTIP",
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
            }

            if (compoundChanges.Count > 0)
            {
                patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, compoundChanges, cloudSizes);

                var patchProperties = new PatchEventProperties
                {
                    CustomTooltip = tooltip,
                };

                patch.CurrentSnapshot.ActivePatchEvents[PatchEventTypes.Runoff] = patchProperties;
            }

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

    private void ApplyChunksConfiguration(Patch patch, Biome templateBiome, Dictionary<Compound, string[]> chunkGroup,
        Compound compound)
    {
        PatchEventUtils.ApplyChunksConfiguration(patch, templateBiome, chunkGroup, null, compound, random,
            addChunks: true, Constants.RUNOFF_MIN_CHUNK_DENSITY_MULTIPLIER,
            Constants.RUNOFF_MAX_CHUNK_DENSITY_MULTIPLIER);
    }

    private int GetEventDuration()
    {
        return random.Next(Constants.RUNOFF_MIN_DURATION, Constants.RUNOFF_MAX_DURATION + 1);
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");
    }
}
