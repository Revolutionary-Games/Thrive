using System.Collections.Generic;
using System.Linq;
using Godot;
using SharedBase.Archive;
using Xoshiro.PRNG64;

public class MeteorImpactEvent : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

    private const string TemplateBiomeForChunks = "patchEventTemplateBiome";

    private readonly Dictionary<Compound, float> tempCompoundChanges = new();
    private readonly Dictionary<Compound, float> tempCloudSizes = new();

    private readonly HashSet<int> affectedPatchesIds = new();

    private readonly XoShiRo256starstar random;

    private readonly GameWorld targetWorld;

    private Meteor? selectedMeteor;

    public MeteorImpactEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    private MeteorImpactEvent(GameWorld targetWorld, XoShiRo256starstar random, HashSet<int> affectedPatchesIds)
    {
        this.targetWorld = targetWorld;
        this.random = random;
        this.affectedPatchesIds = affectedPatchesIds;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.MeteorImpactEvent;
    public bool CanBeReferencedInArchive => false;

    public static MeteorImpactEvent ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MeteorImpactEvent(reader.ReadObject<GameWorld>(), reader.ReadObject<XoShiRo256starstar>(),
            reader.ReadObject<HashSet<int>>())
        {
            selectedMeteor = reader.ReadObjectOrNull<Meteor>(),
        };

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
        writer.WriteAnyRegisteredValueAsObject(random);
        writer.WriteObject(affectedPatchesIds);
        writer.WriteObjectOrNull(selectedMeteor);
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        FinishEvent();
        TryToTriggerEvent();
    }

    private void TryToTriggerEvent()
    {
        if (!AreConditionsMet())
            return;

        ChooseAffectedPatches();
        ChooseMeteorType();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (affectedPatchesIds.Contains(patch.ID))
            {
                ChangePatchProperties(patch);
            }
        }

        LogBeginningOfMeteorStrike();
    }

    private void ChooseMeteorType()
    {
        var meteors = SimulationParameters.Instance.GetAllMeteors();
        var index = SimulationParameters.Instance.GetMeteorChances()
            .RandomElementIndexByProbability(random.NextDouble());
        selectedMeteor = meteors.ElementAt(index);
    }

    private void ChooseAffectedPatches()
    {
        var impactSize = random.NextFloat();

        var surfacePatches = new List<Patch>();
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.IsSurfacePatch())
                surfacePatches.Add(patch);
        }

        var selectedPatch = surfacePatches.Random(random);
        var adjacentList = selectedPatch.Region.Adjacent;
        var adjacentRegion = adjacentList.Random(random);

        // 1 patch
        if (impactSize <= 0.33f)
        {
            affectedPatchesIds.Add(selectedPatch.ID);
        }

        // all surface patches in region
        if (impactSize > 0.33f && impactSize <= 0.66f)
        {
            foreach (var adjacent in selectedPatch.Adjacent)
            {
                if (adjacent.Region.ID == selectedPatch.Region.ID && adjacent.IsSurfacePatch())
                {
                    affectedPatchesIds.Add(adjacent.ID);
                }
            }
        }

        // all surface patches in 2 neighbouring regions
        if (impactSize > 0.66f && impactSize <= 0.9f)
        {
            foreach (var patch in adjacentRegion.Patches)
            {
                if (patch.IsSurfacePatch())
                {
                    affectedPatchesIds.Add(patch.ID);
                }
            }
        }

        // around half of all surface patches, canon explanation being meteor splitting into multiple pieces
        if (impactSize > 0.9f)
        {
            foreach (var patch in surfacePatches)
            {
                if (random.Next(0, 2) == 1)
                {
                    affectedPatchesIds.Add(patch.ID);
                }
            }
        }
    }

    private bool AreConditionsMet()
    {
        return random.NextFloat() <= GetImpactChance();
    }

    private float GetImpactChance()
    {
        switch (targetWorld.WorldSettings.GeologicalActivity)
        {
            case WorldGenerationSettings.GeologicalActivityEnum.Dormant:
                return Constants.METEOR_IMPACT_CHANCE * 0.5f;
            case WorldGenerationSettings.GeologicalActivityEnum.Active:
                return Constants.METEOR_IMPACT_CHANCE * 2;
            case WorldGenerationSettings.GeologicalActivityEnum.Average:
            default:
                return Constants.METEOR_IMPACT_CHANCE;
        }
    }

    private void ChangePatchProperties(Patch patch)
    {
        AdjustEnvironmentalConditions(patch);
        AdjustCompounds(patch);
        AddChunks(patch);
        LogEvent(patch);
    }

    /// <summary>
    ///   Gets chunks from the event template patch and applies them to the patches.
    /// </summary>
    private void AddChunks(Patch patch)
    {
        if (selectedMeteor == null)
        {
            GD.PrintErr("Internal error in meteor impact event, no selected meteor type for patch chunks");
            return;
        }

        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForChunks);
        foreach (var configuration in selectedMeteor.Chunks)
        {
            var configurationExists =
                templateBiome.Conditions.Chunks.TryGetValue(configuration, out var chunkConfiguration);
            if (!configurationExists)
            {
                GD.PrintErr("Event chunk configuration does not exist: " + configuration);
                continue;
            }

            chunkConfiguration.Density *= random.NextFloat() * 0.5f + 0.5f;
            patch.Biome.Chunks.Add(configuration, chunkConfiguration);
        }
    }

    private void AdjustEnvironmentalConditions(Patch patch)
    {
        PatchEventProperties eventProperties = new()
        {
            SunlightAmbientMultiplier = Constants.METEOR_IMPACT_SUNLIGHT_MULTIPLICATION,
        };

        if (selectedMeteor == null)
        {
            GD.PrintErr("Meteor type has not been chosen!");
            return;
        }

        patch.CurrentSnapshot.ActivePatchEvents.Add(selectedMeteor.VisualEffect, eventProperties);
    }

    private void AdjustCompounds(Patch patch)
    {
        if (selectedMeteor == null)
        {
            GD.PrintErr("Internal error in meteor impact event, no selected meteor type for patch compounds");
            return;
        }

        tempCompoundChanges.Clear();
        tempCloudSizes.Clear();

        foreach (var (compoundName, levelChange) in selectedMeteor.Compounds)
        {
            bool hasCompound =
                patch.Biome.ChangeableCompounds.TryGetValue(compoundName, out var currentCompoundLevel);

            if (!hasCompound)
            {
                // This is adding a new compound
                GD.Print($"Impact event is adding a new compound {compoundName} that was not present before " +
                    $"in {patch.Name}");
            }

            var definition = SimulationParameters.Instance.GetCompoundDefinition(compoundName);

            if (!definition.IsEnvironmental)
            {
                // glucose, phosphates, iron, sulfur
                currentCompoundLevel.Density = levelChange;

                // TODO: instead of hardcoding the fallback value, maybe this could look in the event template biome?
                currentCompoundLevel.Amount = currentCompoundLevel.Amount == 0 ? 125000 : currentCompoundLevel.Amount;
                tempCompoundChanges[compoundName] = currentCompoundLevel.Density;
                tempCloudSizes[compoundName] = currentCompoundLevel.Amount;
            }
            else
            {
                // CO2
                currentCompoundLevel.Ambient = levelChange;
                tempCompoundChanges[compoundName] = currentCompoundLevel.Ambient;
            }
        }

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, tempCompoundChanges, tempCloudSizes);
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("METEOR_IMPACT_EVENT"),
            true, true, "MeteorImpactEvent.svg");
    }

    private void FinishEvent()
    {
        foreach (var index in affectedPatchesIds)
        {
            if (!targetWorld.Map.Patches.TryGetValue(index, out var patch))
            {
                GD.PrintErr("Patch exited the world in meteor impact event");
                continue;
            }

            ReduceCompounds(patch);
            ResetEnvironment(patch);
            RemoveChunks(patch);
        }

        affectedPatchesIds.Clear();
    }

    private void ReduceCompounds(Patch patch)
    {
        if (selectedMeteor == null)
        {
            GD.PrintErr("Internal error in meteor impact event, no selected meteor type for patch compounds reset");
            return;
        }

        tempCompoundChanges.Clear();
        tempCloudSizes.Clear();

        foreach (var (compoundName, levelChange) in selectedMeteor.Compounds)
        {
            bool hasCompound =
                patch.Biome.ChangeableCompounds.TryGetValue(compoundName, out var currentCompoundLevel);

            if (!hasCompound)
            {
                GD.PrintErr("Did not find compound to reduce after impact event ended");
                continue;
            }

            var definition = SimulationParameters.Instance.GetCompoundDefinition(compoundName);

            if (!definition.IsEnvironmental)
            {
                // glucose, phosphates, iron, sulfur
                currentCompoundLevel.Density = -levelChange * 0.2f;
                tempCompoundChanges[compoundName] = currentCompoundLevel.Density;
                tempCloudSizes[compoundName] = currentCompoundLevel.Amount;
            }

            // CO2 (and other gases) are not reduced back to normal values
        }

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, tempCompoundChanges, tempCloudSizes);
    }

    private void ResetEnvironment(Patch patch)
    {
        if (selectedMeteor == null)
        {
            GD.PrintErr("Meteor type has not been chosen!");
            return;
        }

        patch.CurrentSnapshot.ActivePatchEvents.Remove(selectedMeteor.VisualEffect);
    }

    private void RemoveChunks(Patch patch)
    {
        if (selectedMeteor == null)
        {
            GD.PrintErr("Internal error in meteor impact event, no meteor type to remove added chunks");
            return;
        }

        foreach (var configuration in selectedMeteor.Chunks)
        {
            patch.Biome.Chunks.Remove(configuration);
        }
    }

    private void LogBeginningOfMeteorStrike()
    {
        var translatedText = affectedPatchesIds.Count == 1 ?
            new LocalizedString("METEOR_STRIKE_START_EVENT_LOG_SINGULAR", affectedPatchesIds.Count) :
            new LocalizedString("METEOR_STRIKE_START_EVENT_LOG_PLURAL", affectedPatchesIds.Count);
        targetWorld.LogEvent(translatedText, true, true, "GlobalGlaciationEvent.svg");
    }
}
