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

    private static readonly string[] ChunksConfigurations =
        ["ironSmallChunk", "ironBigChunk", "phosphateSmallChunk", "phosphateBigChunk", "radioactiveChunk"];

    [JsonProperty]
    private readonly HashSet<int> modifiedPatchesIds = new();

    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private GameWorld targetWorld;

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

        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (modifiedPatchesIds.Contains(patch.ID))
            {
                ChangePatchProperties(patch, totalTimePassed);
            }
        }

        LogBeginningOfMeteorStrike();
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

        if (!hasSunlight)
        {
            GD.PrintErr("Meteor impact event encountered patch with unexpectedly no sunlight");
            return;
        }

        currentSunlight.Ambient *= Constants.METEOR_IMPACT_SUNLIGHT_MULTIPLICATION;
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
    }

    /// <summary>
    ///   Gets chunks from Vents patch template and applies them to the patches.
    /// </summary>
    private void AddChunks(Patch patch)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForChunks);
        foreach (var configuration in ChunksConfigurations)
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
        foreach (var configuration in ChunksConfigurations)
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
