using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class MeteorImpactEvent : IWorldEffect
{
    private const string TemplateBiomeForChunks = "aavolcanic_vent";
    private const string Prefix = "meteorImpact_";

    [JsonProperty]
    private readonly HashSet<int> modifiedPatchesIds = new();

    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private GameWorld targetWorld;

    [JsonProperty]
    private Meteor selectedMeteor;

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

        // Impact sizes:
        // 0 -> 1 patch; 1 -> all surface patches in region; 2 -> all surface patches in 2 neighbouring regions
        var impactSize = random.Next(0, 3);
        GetAffectedPatches(impactSize);
        GetMeteorType();

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (modifiedPatchesIds.Contains(patch.ID))
            {
                ChangePatchProperties(patch, totalTimePassed);
            }
        }

        LogBeginningOfMeteorStrike();
    }

    private void GetMeteorType()
    {
        var meteors = SimulationParameters.Instance.GetAllMeteors().ToList();
        var index = PatchEventUtils.GetRandomElementByProbability(meteors.Select(meteor => meteor.Probability).ToList(),
            random.NextDouble());
        selectedMeteor = meteors.ElementAt(index);
    }

    private void GetAffectedPatches(int impactSize)
    {
        var surfacePatches = targetWorld.Map.Patches.Values.Where(IsSurfacePatch).ToList();
        var selectedPatch = surfacePatches[random.Next(surfacePatches.Count)];
        var adjacentRegion = selectedPatch.Region.Adjacent.ToList()[random.Next(selectedPatch.Region.Adjacent.Count)];

        if (impactSize >= 0)
        {
            modifiedPatchesIds.Add(selectedPatch.ID);
        }

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
        AdjustEnvironment(patch);
        AddChunks(patch);
        LogEvent(patch, totalTimePassed);
    }

    private void AdjustEnvironment(Patch patch)
    {
        bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);
        bool hasCarboneDioxide =
            patch.Biome.ChangeableCompounds.TryGetValue(Compound.Carbondioxide, out var currentCarbonDioxide);

        if (!hasSunlight)
        {
            GD.PrintErr("Meteor impact event encountered patch with unexpectedly no sunlight");
            return;
        }

        if (!hasCarboneDioxide)
        {
            GD.PrintErr("Meteor impact event encountered patch with unexpectedly no carbonDioxide");
            return;
        }

        var changes = new Dictionary<Compound, float>();
        var cloudSizes = new Dictionary<Compound, float>();

        currentSunlight.Ambient *= Constants.METEOR_IMPACT_SUNLIGHT_MULTIPLICATION;
        currentCarbonDioxide.Ambient += 0.15f;
        currentCarbonDioxide.Ambient = Math.Clamp(currentCarbonDioxide.Ambient, 0, 1);
        changes[Compound.Carbondioxide] = currentCarbonDioxide.Ambient;

        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, cloudSizes);
    }

    /// <summary>
    ///   Gets chunks from Vents patch template and applies them to the patches.
    /// </summary>
    private void AddChunks(Patch patch)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForChunks);
        foreach (var configuration in selectedMeteor.Chunks)
        {
            var chunkConfiguration = templateBiome.Conditions.Chunks[configuration];
            chunkConfiguration.Density *= random.NextFloat() * 0.5f + 0.5f;
            patch.Biome.Chunks.Add(Prefix + configuration, chunkConfiguration);
        }
    }

    private void LogEvent(Patch patch, double totalTimePassed)
    {
        patch.LogEvent(new LocalizedString("METEOR_IMPACT_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");

        if (patch.Visibility == MapElementVisibility.Shown)
        {
            targetWorld.LogEvent(new LocalizedString("METEOR_IMPACT_EVENT_LOG", patch.Name),
                true, false, "GlobalGlaciationEvent.svg");
        }

        patch.AddPatchEventRecord(WorldEffectVisuals.MeteorImpact, totalTimePassed);
    }

    private void FinishEvent()
    {
        foreach (var index in modifiedPatchesIds)
        {
            if (!targetWorld.Map.Patches.TryGetValue(index, out var patch))
            {
                GD.PrintErr("Patch exited the world");
                continue;
            }

            ResetEnvironment(patch);
            RemoveChunks(patch);
        }

        modifiedPatchesIds.Clear();
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
        foreach (var configuration in selectedMeteor.Chunks)
        {
            patch.Biome.Chunks.Remove(Prefix + configuration);
        }
    }

    private void LogBeginningOfMeteorStrike()
    {
        targetWorld.LogEvent(new LocalizedString("METEOR_STRIKE_START_EVENT_LOG", modifiedPatchesIds.Count),
            true, true, "GlobalGlaciationEvent.svg");
    }
}
