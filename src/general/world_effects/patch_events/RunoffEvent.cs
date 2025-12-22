using System.Collections.Generic;
using SharedBase.Archive;
using Xoshiro.PRNG64;

public class RunoffEvent : IWorldEffect
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

    private readonly Dictionary<Compound, float> compoundChanges = new();
    private readonly Dictionary<Compound, float> cloudSizes = new();

    private readonly XoShiRo256starstar random;

    private readonly Dictionary<int, int> eventDurationsInPatches = new();

    private readonly Dictionary<int, List<Compound>> affectedCompoundsInPatches = new();

    private readonly List<int> patchesToRemove = new();

    private GameWorld targetWorld;

    public RunoffEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    public RunoffEvent(GameWorld targetWorld, XoShiRo256starstar random,
        Dictionary<int, int> eventDurationsInPatches, Dictionary<int, List<Compound>> affectedCompoundsInPatches)
    {
        this.targetWorld = targetWorld;
        this.random = random;
        this.eventDurationsInPatches = eventDurationsInPatches;
        this.affectedCompoundsInPatches = affectedCompoundsInPatches;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RunoffEvent;
    public bool CanBeReferencedInArchive => false;

    public static RunoffEvent ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new RunoffEvent(reader.ReadObject<GameWorld>(),
            reader.ReadObject<XoShiRo256starstar>(), reader.ReadObject<Dictionary<int, int>>(),
            reader.ReadObject<Dictionary<int, List<Compound>>>());

        return instance;
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
            LogEvent(patch);
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
        foreach (int patchId in eventDurationsInPatches.Keys)
        {
            var patch = targetWorld.Map.Patches[patchId];
            eventDurationsInPatches[patchId] -= 1;

            if (eventDurationsInPatches[patchId] <= 0)
            {
                patchesToRemove.Add(patchId);
                affectedCompoundsInPatches.Remove(patchId);
                patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.Runoff);
                continue;
            }

            var affectedCompounds = affectedCompoundsInPatches[patchId];
            var tooltip = PatchEventUtils.BuildCustomTooltip(new LocalizedString("EVENT_RUNOFF_TOOLTIP"),
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

        foreach (var patchId in patchesToRemove)
        {
            eventDurationsInPatches.Remove(patchId);
        }

        patchesToRemove.Clear();
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
            true, Constants.RUNOFF_MIN_CHUNK_DENSITY_MULTIPLIER,
            Constants.RUNOFF_MAX_CHUNK_DENSITY_MULTIPLIER);
    }

    private int GetEventDuration()
    {
        return random.Next(Constants.RUNOFF_MIN_DURATION, Constants.RUNOFF_MAX_DURATION + 1);
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("RUNOFF_EVENT"),
            true, true, "RunoffEvent.svg");
    }
}
