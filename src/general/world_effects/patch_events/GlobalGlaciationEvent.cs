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

    private readonly bool hasEventAlreadyHappened = false;

    private readonly Dictionary<int, Dictionary<Compound, float>> previousEnvironmentalChanges = new();
    private readonly Dictionary<int, string> previousBackground = new();
    private readonly Dictionary<int, Color> previousLightColour = new();

    /// <summary>
    /// Tells how many generations the event will last. "-1" means that it hasn't started at all.
    /// "0" means it has finished, and it won't happen again
    /// </summary>
    private int generationsLeft = -1;

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

    public void OnRegisterToWorld() { }

    public void OnTimePassed(double elapsed, double totalTimePassed)
    {
        if (generationsLeft > 0)
            generationsLeft -= 1;

        if (generationsLeft > 0)
            MarkPatches(totalTimePassed);

        if (hasEventAlreadyHappened)
            return;

        if (generationsLeft == -1)
            TryToTriggerEvent(totalTimePassed);
        else if (generationsLeft == 0)
            FinishEvent();
    }

    private void MarkPatches(double totalTimePassed)
    {
        foreach (var patch in targetWorld.Map.Patches.Values)
        {
            if (Constants.SurfaceBiomes.Contains(patch.BiomeType))
            {
                patch.AddPatchEventRecord(WorldEffectVisuals.GlaciationEvent, totalTimePassed);
            }
        }
    }

    private void TryToTriggerEvent(double totalTimePassed)
    {
        if (!AreConditionsMet())
            return;

        generationsLeft = random.Next(2, 6);

        foreach (var (index, patch) in targetWorld.Map.Patches)
        {
            if (Constants.SurfaceBiomes.Contains(patch.BiomeType))
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
            if (!Constants.SurfaceBiomes.Contains(patch.BiomeType))
                continue;

            var oxygenLevel = patch.Biome.ChangeableCompounds[Compound.Oxygen];
            if (oxygenLevel.Ambient >= Constants.OXYGEN_THRESHOLD)
                patchesExceedingOxygenLevel += 1;

            numberOfSurfacePatches += 1;
        }

        // Just prevent dividing by zero, but that shouldn't be possible anyway
        numberOfSurfacePatches = numberOfSurfacePatches == 0 ? 1 : numberOfSurfacePatches;

        return (float)patchesExceedingOxygenLevel / numberOfSurfacePatches >= Constants.OXYGEN_PATCHES_THRESHOLD
            && random.Next(100) >= Constants.GLOBAL_GLACIATION_CHANCE;
    }

    private void ChangePatchProperties(int index, Patch patch, double totalTimePassed)
    {
        AdjustBackground(index, patch);
        AdjustEnvironment(index, patch);
        AdjustChunks(patch);
        LogEvent(patch, totalTimePassed);
    }

    private void AdjustBackground(int index, Patch patch)
    {
        previousBackground.Add(index, patch.BiomeTemplate.Background);
        previousLightColour.Add(index, patch.BiomeTemplate.Sunlight.Colour);

        patch.BiomeTemplate.Background = Background;
        patch.BiomeTemplate.Sunlight.Colour = new Color(0.8f, 0.9f, 1, 1);
    }

    private void AdjustEnvironment(int index, Patch patch)
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
        previousEnvironmentalChanges.Add(index, changes);

        patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, null);
    }

    /// <summary>
    /// Gets chunks from Iceshelf patch template and applies them to the patches.
    /// </summary>
    private void AdjustChunks(Patch patch)
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

        patch.AddPatchEventRecord(WorldEffectVisuals.GlaciationEvent, totalTimePassed);
    }

    private void FinishEvent()
    {
        foreach (var index in previousEnvironmentalChanges.Keys)
        {
            if (!targetWorld.Map.Patches.TryGetValue(index, out var patch))
                continue;

            var biomeBackground = previousBackground[index];
            var biomeLightColour = previousLightColour[index];
            var changes = previousEnvironmentalChanges[index];

            // Reverse the changes in sunlight and temperature values
            foreach (var changeIndex in changes.Keys)
            {
                changes[changeIndex] = -changes[changeIndex];
            }

            patch.BiomeTemplate.Background = biomeBackground;
            patch.BiomeTemplate.Sunlight.Colour = biomeLightColour;
            patch.Biome.ApplyLongTermCompoundChanges(patch.BiomeTemplate, changes, null);

            RemoveChunks(patch);
        }
    }

    private void RemoveChunks(Patch patch)
    {
        foreach (var configuration in IceChunksConfigurations)
        {
            patch.Biome.Chunks.Remove(Prefix + configuration);
        }
    }
}
