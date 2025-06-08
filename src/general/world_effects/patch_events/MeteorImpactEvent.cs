using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class MeteorImpactEvent : IWorldEffect
{
    private const string TemplateBiomeForChunks = "patchEventTemplateBiome";

    private readonly Dictionary<Compound, float> tempCompoundChanges = new();
    private readonly Dictionary<Compound, float> tempCloudSizes = new();

    [JsonProperty]
    private readonly HashSet<int> modifiedPatchesIds = new();

    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private GameWorld targetWorld;

    [JsonProperty]
    private Meteor? selectedMeteor;

    public MeteorImpactEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
    public MeteorImpactEvent(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        FinishEvent();
        TryToTriggerEvent(totalTimePassed);
    }

    private bool IsSurfacePatch(Patch patch)
    {
        return patch.Depth[0] == 0 && patch.BiomeType != BiomeType.Cave;
    }

    private void TryToTriggerEvent(double totalTimePassed)
    {
        if (!AreConditionsMet())
            return;

        ChooseAffectedPatches();
        ChooseMeteorType();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (modifiedPatchesIds.Contains(patch.ID))
            {
                ChangePatchProperties(patch, totalTimePassed);
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
        var impactSize = random.Next(0, 3);

        var surfacePatches = new List<Patch>();
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (IsSurfacePatch(patch))
                surfacePatches.Add(patch);
        }

        var selectedPatch = surfacePatches.Random(random);
        var adjacentList = selectedPatch.Region.Adjacent;
        var adjacentRegion = adjacentList.Random(random);

        // 1 patch
        if (impactSize >= 0)
        {
            modifiedPatchesIds.Add(selectedPatch.ID);
        }

        // all surface patches in region
        if (impactSize >= 1)
        {
            foreach (var adjacent in selectedPatch.Adjacent)
            {
                if (adjacent.Region.ID == selectedPatch.Region.ID && IsSurfacePatch(adjacent))
                {
                    modifiedPatchesIds.Add(adjacent.ID);
                }
            }
        }

        // all surface patches in 2 neighbouring regions
        if (impactSize >= 2)
        {
            foreach (var patch in adjacentRegion.Patches)
            {
                if (IsSurfacePatch(patch))
                {
                    modifiedPatchesIds.Add(patch.ID);
                }
            }
        }
    }

    private bool AreConditionsMet()
    {
        return random.NextFloat() <= Constants.METEOR_IMPACT_CHANCE;
    }

    private void ChangePatchProperties(Patch patch, double totalTimePassed)
    {
        AdjustEnvironmentalConditions(patch);
        AdjustCompounds(patch);
        AddChunks(patch);
        LogEvent(patch, totalTimePassed);
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
        bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

        if (!hasSunlight)
        {
            GD.PrintErr("Meteor impact event encountered patch with unexpectedly no sunlight");
            return;
        }

        currentSunlight.Ambient *= Constants.METEOR_IMPACT_SUNLIGHT_MULTIPLICATION;
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
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
            // Add 0 value for compound if it doesn't exist (e.g. for iron)
            patch.Biome.ChangeableCompounds.TryAdd(compoundName, default);

            var currentCompoundLevel = patch.Biome.ChangeableCompounds[compoundName];

            var definition = SimulationParameters.Instance.GetCompoundDefinition(compoundName);

            if (!definition.IsEnvironmental)
            {
                // glucose, phosphates, iron, sulfur
                currentCompoundLevel.Density = levelChange;

                // TODO: instead of hardcoding the fallback value, maybe this could look in the event template biome?
                currentCompoundLevel.Amount = currentCompoundLevel.Amount == 0 ? 10000 : currentCompoundLevel.Amount;
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

    private void LogEvent(Patch patch, double totalTimePassed)
    {
        patch.LogEvent(new LocalizedString("METEOR_IMPACT_EVENT"),
            true, true, "MeteorImpactEvent.svg");

        if (selectedMeteor == null)
        {
            GD.PrintErr("Internal error in meteor impact event");
            return;
        }

        patch.AddPatchEventRecord(selectedMeteor.VisualEffect, totalTimePassed);
    }

    private void FinishEvent()
    {
        foreach (var index in modifiedPatchesIds)
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

        modifiedPatchesIds.Clear();
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
            // Add 0 value for compound if it doesn't exist (e.g. for iron)
            patch.Biome.ChangeableCompounds.TryAdd(compoundName, default);

            var currentCompoundLevel = patch.Biome.ChangeableCompounds[compoundName];

            var definition = SimulationParameters.Instance.GetCompoundDefinition(compoundName);

            if (!definition.IsEnvironmental)
            {
                // glucose, phosphates, iron, sulfur
                currentCompoundLevel.Density = -levelChange / 3;
                tempCompoundChanges[compoundName] = currentCompoundLevel.Density;
                tempCloudSizes[compoundName] = currentCompoundLevel.Amount;
            }

            // CO2 is not reduced back to normal values
        }

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, tempCompoundChanges, tempCloudSizes);
    }

    private void ResetEnvironment(Patch patch)
    {
        bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

        if (!hasSunlight)
        {
            GD.PrintErr("Meteor impact event encountered patch with unexpectedly no sunlight");
            return;
        }

        currentSunlight.Ambient /= Constants.METEOR_IMPACT_SUNLIGHT_MULTIPLICATION;
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
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
        var translatedText = modifiedPatchesIds.Count == 1 ?
            new LocalizedString("METEOR_STRIKE_START_EVENT_LOG_SINGULAR", modifiedPatchesIds.Count) :
            new LocalizedString("METEOR_STRIKE_START_EVENT_LOG_PLURAL", modifiedPatchesIds.Count);
        targetWorld.LogEvent(translatedText, true, true, "GlobalGlaciationEvent.svg");
    }
}
