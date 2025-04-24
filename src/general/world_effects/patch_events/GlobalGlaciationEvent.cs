using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class GlobalGlaciationEvent : IWorldEffect
{
    private const string TemplateBiomeForIceChunks = "patchEventTemplateBiome";
    private const string Background = "iceshelf";

    private static readonly string[] IceChunksConfigurations =
        ["glaciationIceShard", "glaciationIceChunkSmall", "glaciationIceChunkBig", "glaciationIceSnowflake"];

    [JsonProperty]
    private readonly XoShiRo256starstar random;

    [JsonProperty]
    private readonly List<int> modifiedPatchesIds = new();

    [JsonProperty]
    private bool hasEventAlreadyHappened;

    /// <summary>
    ///   Tells how many generations the event will last. "-1" means that it hasn't started at all.
    ///   "0" means it has finished, and it won't happen again
    /// </summary>
    [JsonProperty]
    private int generationsLeft = -1;

    [JsonProperty]
    private int generationsToTrigger = -1;

    [JsonProperty]
    private int eventDuration;

    [JsonProperty]
    private GameWorld targetWorld;

    public GlobalGlaciationEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    [JsonConstructor]
    public GlobalGlaciationEvent(GameWorld targetWorld, XoShiRo256starstar random)
    {
        this.targetWorld = targetWorld;
        this.random = random;
    }

    public void OnRegisterToWorld()
    {
    }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        if (hasEventAlreadyHappened)
            return;

        if (generationsLeft > 0)
            generationsLeft -= 1;

        if (generationsLeft == -1)
        {
            TryToTriggerEvent(totalTimePassed);
        }
        else if (generationsLeft == 0)
        {
            FinishEvent();
        }

        // Mark patches with the event icon while the event lasts
        if (generationsLeft > 0)
            MarkPatches(totalTimePassed);
    }

    private bool IsSurfacePatch(Patch patch)
    {
        return patch.Depth[0] == 0 && patch.BiomeType != BiomeType.Cave;
    }

    private void MarkPatches(double totalTimePassed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (IsSurfacePatch(patch))
            {
                patch.AddPatchEventRecord(WorldEffectTypes.GlobalGlaciation, totalTimePassed);
            }
        }
    }

    private void TryToTriggerEvent(double totalTimePassed)
    {
        if (generationsToTrigger == -1)
        {
            if (!AreConditionsMet())
                return;

            generationsToTrigger = Constants.GLOBAL_GLACIATION_HEADS_UP_DURATION;
            return;
        }

        if (generationsToTrigger > 0)
        {
            LogHeadUpEventWarning();
            generationsToTrigger -= 1;
        }
        else if (generationsToTrigger == 0)
        {
            eventDuration = random.Next(Constants.GLOBAL_GLACIATION_MIN_DURATION,
                Constants.GLOBAL_GLACIATION_MAX_DURATION);
            generationsLeft = eventDuration;

            foreach (var patch in targetWorld.Map.Patches.Values)
            {
                if (IsSurfacePatch(patch))
                {
                    ChangePatchProperties(patch, totalTimePassed);
                }
            }

            LogBeginningOfGlaciation();
        }
    }

    private bool AreConditionsMet()
    {
        var numberOfSurfacePatches = 0;
        var patchesExceedingOxygenLevel = 0;
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (!IsSurfacePatch(patch))
                continue;

            patch.Biome.TryGetCompound(Compound.Oxygen, CompoundAmountType.Biome, out var oxygenLevel);
            if (oxygenLevel.Ambient >= Constants.GLOBAL_GLACIATION_OXYGEN_THRESHOLD)
                patchesExceedingOxygenLevel += 1;

            numberOfSurfacePatches += 1;
        }

        // Just prevent dividing by zero, but that shouldn't be possible anyway
        numberOfSurfacePatches = numberOfSurfacePatches == 0 ? 1 : numberOfSurfacePatches;

        return (float)patchesExceedingOxygenLevel / numberOfSurfacePatches >=
            Constants.GLOBAL_GLACIATION_PATCHES_THRESHOLD
            && random.NextFloat() <= Constants.GLOBAL_GLACIATION_CHANCE;
    }

    private void ChangePatchProperties(Patch patch, double totalTimePassed)
    {
        modifiedPatchesIds.Add(patch.ID);
        AdjustBackground(patch);
        AdjustEnvironment(patch);
        AddIceChunks(patch);
        LogEvent(patch, totalTimePassed);
    }

    private void AdjustBackground(Patch patch)
    {
        patch.CurrentSnapshot.Background = Background;
    }

    private void AdjustEnvironment(Patch patch)
    {
        if (patch.BiomeType == BiomeType.IceShelf)
        {
            return;
        }

        bool hasTemperature =
            patch.Biome.ChangeableCompounds.TryGetValue(Compound.Temperature, out var currentTemperature);
        bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

        if (!hasTemperature)
        {
            GD.PrintErr("Glaciation event encountered patch with unexpectedly no temperature.");
            return;
        }

        if (!hasSunlight)
        {
            GD.PrintErr("Glaciation event encountered patch with unexpectedly no sunlight.");
            return;
        }

        if (patch.BiomeType != BiomeType.IceShelf)
        {
            currentSunlight.Ambient *= Constants.GLOBAL_GLACIATION_SUNLIGHT_MULTIPLICATION;
            patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
        }

        currentTemperature.Ambient = random.Next(0, 5);
        currentSunlight.Ambient = 0.5f;

        patch.Biome.ModifyLongTermCondition(Compound.Temperature, currentTemperature);
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
    }

    /// <summary>
    ///   Gets chunks from event template patch and applies them to the patches.
    /// </summary>
    private void AddIceChunks(Patch patch)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForIceChunks);
        foreach (var configuration in IceChunksConfigurations)
        {
            var iceChunkConfiguration = templateBiome.Conditions.Chunks[configuration];
            patch.Biome.Chunks.Add(configuration, iceChunkConfiguration);
        }
    }

    private void LogEvent(Patch patch, double totalTimePassed)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");

        patch.AddPatchEventRecord(WorldEffectTypes.GlobalGlaciation, totalTimePassed);
    }

    private void LogHeadUpEventWarning()
    {
        var translatedText = generationsToTrigger == 1 ?
            new LocalizedString("GLOBAL_GLACIATION_EVENT_WARNING_LOG_SINGULAR", generationsToTrigger) :
            new LocalizedString("GLOBAL_GLACIATION_EVENT_WARNING_LOG_PLURAL", generationsToTrigger);
        targetWorld.LogEvent(translatedText, true, true, "GlobalGlaciationEvent.svg");
    }

    private void LogBeginningOfGlaciation()
    {
        targetWorld.LogEvent(new LocalizedString("GLOBAL_GLACIATION_START_EVENT_LOG"),
            true, true, "GlobalGlaciationEvent.svg");
    }

    private void LogEndOfGlaciation()
    {
        targetWorld.LogEvent(new LocalizedString("GLOBAL_GLACIATION_END_EVENT_LOG"),
            true, true, "GlobalGlaciationEvent.svg");
    }

    private void FinishEvent()
    {
        hasEventAlreadyHappened = true;
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (!modifiedPatchesIds.Contains(patch.ID))
            {
                GD.PrintErr("Patch exited the world in global glaciation event");
                continue;
            }

            PatchSnapshot patchSnapshot = patch.History[eventDuration];

            ResetBackground(patch, patchSnapshot);
            ResetEnvironment(patch, patchSnapshot);
            RemoveChunks(patch);
        }

        LogEndOfGlaciation();
    }

    private void ResetBackground(Patch patch, PatchSnapshot patchSnapshot)
    {
        patch.CurrentSnapshot.Background = patchSnapshot.Background;
    }

    private void ResetEnvironment(Patch patch, PatchSnapshot patchSnapshot)
    {
        if (patch.BiomeType != BiomeType.IceShelf)
        {
            bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

            if (!hasSunlight)
            {
                GD.PrintErr("Glaciation event encountered patch with unexpectedly no sunlight");
                return;
            }

            currentSunlight.Ambient /= Constants.GLOBAL_GLACIATION_SUNLIGHT_MULTIPLICATION;
            patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
        }

        var previousTemperature = patchSnapshot.Biome.ChangeableCompounds[Compound.Temperature];
        patch.Biome.ModifyLongTermCondition(Compound.Temperature, previousTemperature);
    }

    private void RemoveChunks(Patch patch)
    {
        foreach (var configuration in IceChunksConfigurations)
        {
            patch.Biome.Chunks.Remove(configuration);
        }
    }
}
