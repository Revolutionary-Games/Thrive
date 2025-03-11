﻿using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

[JSONDynamicTypeAllowed]
public class GlobalGlaciationEvent : IWorldEffect
{
    private const string TemplateBiomeForIceChunks = "ice_shelf";
    private const string Prefix = "globalGlaciation_";
    private const string Background = "iceshelf";

    private static readonly string[] IceChunksConfigurations =
        ["iceShard", "iceChunkSmall", "iceChunkBig", "iceSnowflake"];

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

        // Mark patches with the event icon while the event lasts
        if (generationsLeft > 0)
            MarkPatches(totalTimePassed);

        if (generationsLeft == -1)
        {
            TryToTriggerEvent(totalTimePassed);
        }
        else if (generationsLeft == 0)
        {
            FinishEvent();
        }
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
                patch.AddPatchEventRecord(WorldEffectVisuals.GlobalGlaciation, totalTimePassed);
            }
        }
    }

    private void TryToTriggerEvent(double totalTimePassed)
    {
        if (!AreConditionsMet())
            return;

        eventDuration = random.Next(Constants.GLOBAL_GLACIATION_MIN_DURATION, Constants.GLOBAL_GLACIATION_MAX_DURATION);
        generationsLeft = eventDuration;

        foreach (var (index, patch) in targetWorld.Map.Patches)
        {
            if (IsSurfacePatch(patch))
            {
                ChangePatchProperties(index, patch, totalTimePassed);
            }
        }

        LogBeginningOfGlaciation();
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

    private void ChangePatchProperties(int index, Patch patch, double totalTimePassed)
    {
        modifiedPatchesIds.Add(index);
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
        bool hasTemperature =
            patch.Biome.ChangeableCompounds.TryGetValue(Compound.Temperature, out var currentTemperature);
        bool hasSunlight = patch.Biome.ChangeableCompounds.TryGetValue(Compound.Sunlight, out var currentSunlight);

        if (!hasTemperature)
        {
            GD.PrintErr("Patch has no temperature");
            return;
        }

        if (!hasSunlight)
        {
            GD.PrintErr("Patch has no sunlight");
            return;
        }

        currentTemperature.Ambient = random.Next(0, 5);
        currentSunlight.Ambient = 0.5f;

        patch.Biome.ModifyLongTermCondition(Compound.Temperature, currentTemperature);
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, currentSunlight);
    }

    /// <summary>
    ///   Gets chunks from Iceshelf patch template and applies them to the patches.
    /// </summary>
    private void AddIceChunks(Patch patch)
    {
        var templateBiome = SimulationParameters.Instance.GetBiome(TemplateBiomeForIceChunks);
        foreach (var configuration in IceChunksConfigurations)
        {
            var iceChunkConfiguration = templateBiome.Conditions.Chunks[configuration];
            iceChunkConfiguration.Density *= 10;
            patch.Biome.Chunks.Add(Prefix + configuration, iceChunkConfiguration);
        }
    }

    private void LogEvent(Patch patch, double totalTimePassed)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");

        if (patch.Visibility == MapElementVisibility.Shown)
        {
            targetWorld.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT_LOG", patch.Name),
                true, false, "GlobalGlaciationEvent.svg");
        }

        patch.AddPatchEventRecord(WorldEffectVisuals.GlobalGlaciation, totalTimePassed);
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
        foreach (var index in modifiedPatchesIds)
        {
            if (!targetWorld.Map.Patches.TryGetValue(index, out var patch))
            {
                GD.PrintErr("Patch exited the world");
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
        var previousTemperature = patchSnapshot.Biome.ChangeableCompounds[Compound.Temperature];
        var previousSunlight = patchSnapshot.Biome.ChangeableCompounds[Compound.Sunlight];

        patch.Biome.ModifyLongTermCondition(Compound.Temperature, previousTemperature);
        patch.Biome.ModifyLongTermCondition(Compound.Sunlight, previousSunlight);
    }

    private void RemoveChunks(Patch patch)
    {
        foreach (var configuration in IceChunksConfigurations)
        {
            patch.Biome.Chunks.Remove(Prefix + configuration);
        }
    }
}
