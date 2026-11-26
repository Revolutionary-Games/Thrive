using System.Collections.Generic;
using SharedBase.Archive;
using Xoshiro.PRNG64;

/// <summary>
///   Underwater vents only event producing hydrogen sulfide and carbon dioxide
/// </summary>
public class UnderwaterVentEruptionEvent : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

    private readonly XoShiRo256starstar random;

    private readonly GameWorld targetWorld;

    public UnderwaterVentEruptionEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    private UnderwaterVentEruptionEvent(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.UnderwaterVentEruptionEvent;

    public bool CanBeReferencedInArchive => false;

    public static UnderwaterVentEruptionEvent ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new UnderwaterVentEruptionEvent(reader.ReadObject<GameWorld>(),
            reader.ReadObject<XoShiRo256starstar>());
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
        writer.WriteAnyRegisteredValueAsObject(random);
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        FinishOldEvents();
        StartNewEvents();
    }

    private void FinishOldEvents()
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.UnderwaterVentEruption);
        }
    }

    private void StartNewEvents()
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.BiomeType != BiomeType.Vents)
                continue;

            if (random.NextFloat() > GetVentEruptionChance())
                continue;

            var propertiesChanged = ChangePatchProperties(patch);

            if (!propertiesChanged)
                continue;

            patch.CurrentSnapshot.ActivePatchEvents.Add(PatchEventTypes.UnderwaterVentEruption,
                new PatchEventProperties());

            LogPatchEvent(patch);
        }
    }

    private bool ChangePatchProperties(Patch patch)
    {
        var changes = new Dictionary<Compound, float>();
        var cloudSizes = new Dictionary<Compound, float>();

        patch.Biome.ChangeableCompounds.TryGetValue(Compound.Hydrogensulfide, out var currentHydrogenSulfide);

        currentHydrogenSulfide.Amount = currentHydrogenSulfide.Amount == 0 ? 250000 : currentHydrogenSulfide.Amount;

        // Intelligently apply the changes taking total gas percentages into account
        changes[Compound.Hydrogensulfide] = Constants.VENT_ERUPTION_HYDROGEN_SULFIDE_INCREASE;
        changes[Compound.Carbondioxide] = Constants.VENT_ERUPTION_CARBON_DIOXIDE_INCREASE;
        cloudSizes[Compound.Hydrogensulfide] = currentHydrogenSulfide.Amount;

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, cloudSizes);

        return true;
    }

    private void LogPatchEvent(Patch patch)
    {
        // Patch specific log
        // TODO: should these events be highlighted always? It'll get busy when there are a lot of events.
        patch.LogEvent(new LocalizedString("UNDERWATER_VENT_ERUPTION"),
            true, true, "EruptionEvent.svg");

        if (patch.Visibility == MapElementVisibility.Shown)
        {
            // Global log, but only if patch is known to the player
            targetWorld.LogEvent(new LocalizedString("UNDERWATER_VENT_ERUPTION_IN", patch.Name),
                true, true, "EruptionEvent.svg");
        }
    }

    private float GetVentEruptionChance()
    {
        switch (targetWorld.WorldSettings.GeologicalActivity)
        {
            case WorldGenerationSettings.GeologicalActivityEnum.Dormant:
                return Constants.VENT_ERUPTION_CHANCE * 0.5f;
            case WorldGenerationSettings.GeologicalActivityEnum.Active:
                return Constants.VENT_ERUPTION_CHANCE * 2;
            case WorldGenerationSettings.GeologicalActivityEnum.Average:
            default:
                return Constants.VENT_ERUPTION_CHANCE;
        }
    }
}
