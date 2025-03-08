using System.Collections.Generic;
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
    /// Tells how many generations the event will last. "-1" means that it hasn't started at all.
    /// "0" means it has finished, and it won't happen again
    /// </summary>
    [JsonProperty]
    private int generationsLeft = -1;

    [JsonProperty]
    private int eventDuration = 0;

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
        if (generationsLeft > 0)
            generationsLeft -= 1;

        if (generationsLeft > 0)
            MarkPatches(totalTimePassed);

        if (hasEventAlreadyHappened)
            return;

        if (generationsLeft == -1)
        {
            TryToTriggerEvent(totalTimePassed);
        }
        else if (generationsLeft == 0)
        {
            FinishEvent();
        }
    }

    private void MarkPatches(double totalTimePassed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.Depth[0] == 0)
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
            if (patch.Depth[0] == 0)
            {
                ChangePatchProperties(index, patch, totalTimePassed);
            }
        }
    }

    private bool AreConditionsMet()
    {
        var numberOfSurfacePatches = 0;
        var patchesExceedingOxygenLevel = 0;
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (patch.Depth[0] != 0)
                continue;

            patch.Biome.TryGetCompound(Compound.Oxygen, CompoundAmountType.Biome, out var oxygenLevel);
            if (oxygenLevel.Ambient >= Constants.GLOBAL_GLACIATION_OXYGEN_THRESHOLD)
                patchesExceedingOxygenLevel += 1;

            numberOfSurfacePatches += 1;
        }

        // Just prevent dividing by zero, but that shouldn't be possible anyway
        numberOfSurfacePatches = numberOfSurfacePatches == 0 ? 1 : numberOfSurfacePatches;

        return (float)patchesExceedingOxygenLevel / numberOfSurfacePatches >= Constants.GLOBAL_GLACIATION_PATCHES_THRESHOLD
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
        patch.BiomeTemplate.Background = Background;
        patch.CurrentSnapshot.Background = Background;
        patch.BiomeTemplate.Sunlight.Colour = new Color(0.8f, 0.8f, 1, 1);
        patch.CurrentSnapshot.LightColour = new Color(0.8f, 0.8f, 1, 1);
    }

    private void AdjustEnvironment(Patch patch)
    {
        var currentTemperature = patch.Biome.ChangeableCompounds[Compound.Temperature];
        var currentSunlight = patch.Biome.ChangeableCompounds[Compound.Sunlight];

        currentTemperature.Ambient = random.Next(-1, 5) - currentTemperature.Ambient;
        currentSunlight.Ambient = 0.5f - currentSunlight.Ambient;

        var changes = new Dictionary<Compound, float>
        {
            [Compound.Temperature] = currentTemperature.Ambient,
            [Compound.Sunlight] = currentSunlight.Ambient,
        };

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, new Dictionary<Compound, float>());
    }

    /// <summary>
    /// Gets chunks from Iceshelf patch template and applies them to the patches.
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
            true, true, "PatchIceShelf.svg");

        if (patch.Visibility == MapElementVisibility.Shown)
        {
            targetWorld.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT_LOG", patch.Name),
                true, true, "PatchIceShelf.svg");
        }

        patch.AddPatchEventRecord(WorldEffectVisuals.GlobalGlaciation, totalTimePassed);
    }

    private void FinishEvent()
    {
        hasEventAlreadyHappened = true;
        foreach (var index in modifiedPatchesIds)
        {
            if (!targetWorld.Map.Patches.TryGetValue(index, out var patch))
                continue;

            PatchSnapshot patchSnapshot = patch.History[eventDuration];

            ResetBackground(patch, patchSnapshot);
            ResetEnvironment(patch, patchSnapshot);
            RemoveChunks(patch);
        }
    }

    private void ResetBackground(Patch patch, PatchSnapshot patchSnapshot)
    {
        var biomeBackground = patchSnapshot.Background;
        var biomeLightColour = patchSnapshot.LightColour;
        patch.BiomeTemplate.Background = biomeBackground;
        patch.BiomeTemplate.Sunlight.Colour = biomeLightColour;
        patch.CurrentSnapshot.Background = biomeBackground;
        patch.CurrentSnapshot.LightColour = biomeLightColour;
    }

    private void ResetEnvironment(Patch patch, PatchSnapshot patchSnapshot)
    {
        var currentTemperature = patch.Biome.ChangeableCompounds[Compound.Temperature];
        var currentSunlight = patch.Biome.ChangeableCompounds[Compound.Sunlight];
        var previousTemperature = patchSnapshot.Biome.ChangeableCompounds[Compound.Temperature];
        var previousSunlight = patchSnapshot.Biome.ChangeableCompounds[Compound.Sunlight];

        currentTemperature.Ambient = previousTemperature.Ambient - currentTemperature.Ambient;
        currentSunlight.Ambient = previousSunlight.Ambient - currentSunlight.Ambient;

        var changes = new Dictionary<Compound, float>
        {
            [Compound.Temperature] = currentTemperature.Ambient,
            [Compound.Sunlight] = currentSunlight.Ambient,
        };

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, new Dictionary<Compound, float>());
    }

    private void RemoveChunks(Patch patch)
    {
        foreach (var configuration in IceChunksConfigurations)
        {
            patch.Biome.Chunks.Remove(Prefix + configuration);
        }
    }
}
