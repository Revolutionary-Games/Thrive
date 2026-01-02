using System;
using System.Collections.Generic;
using Godot;
using SharedBase.Archive;
using Xoshiro.PRNG64;

public class GlobalGlaciationEvent : IWorldEffect
{
    public const ushort SERIALIZATION_VERSION = 1;

    private const string TemplateBiomeForIceChunks = "patchEventTemplateBiome";
    private const string Background = "iceshelf";

    private static readonly string[] IceChunksConfigurations =
    [
        "glaciationIceShard", "glaciationIceChunkSmall", "glaciationIceChunkBig", "glaciationIceSnowflake",
        "glaciationIceSnowflakeBig",
    ];

    private readonly XoShiRo256starstar random;

    private readonly List<int> modifiedPatchesIds = new();

    private readonly GameWorld targetWorld;

    private bool hasEventAlreadyHappened;

    /// <summary>
    ///   Tells how many generations the event will last. "-1" means that it hasn't started at all.
    ///   "0" means it has finished, and it won't happen again
    /// </summary>
    private int generationsLeft = -1;

    private int generationsToTrigger = -1;

    private int eventDuration;

    public GlobalGlaciationEvent(GameWorld targetWorld, long randomSeed)
    {
        this.targetWorld = targetWorld;
        random = new XoShiRo256starstar(randomSeed);
    }

    private GlobalGlaciationEvent(GameWorld targetWorld, XoShiRo256starstar random, List<int> modifiedPatchesIds)
    {
        this.targetWorld = targetWorld;
        this.random = random;
        this.modifiedPatchesIds = modifiedPatchesIds;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.GlobalGlaciationEvent;
    public bool CanBeReferencedInArchive => false;

    public static GlobalGlaciationEvent ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new GlobalGlaciationEvent(reader.ReadObject<GameWorld>(),
            reader.ReadObject<XoShiRo256starstar>(), reader.ReadObject<List<int>>())
        {
            hasEventAlreadyHappened = reader.ReadBool(),
            generationsLeft = reader.ReadInt32(),
            generationsToTrigger = reader.ReadInt32(),
            eventDuration = reader.ReadInt32(),
        };

        return instance;
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(targetWorld);
        writer.WriteAnyRegisteredValueAsObject(random);
        writer.WriteObject(modifiedPatchesIds);

        writer.Write(hasEventAlreadyHappened);
        writer.Write(generationsLeft);
        writer.Write(generationsToTrigger);
        writer.Write(eventDuration);
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
            TryToTriggerEvent();
        }
        else if (generationsLeft == 0)
        {
            FinishEvent();
        }
    }

    private void TryToTriggerEvent()
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
            eventDuration = GetEventDuration();
            generationsLeft = eventDuration;

            foreach (var patch in targetWorld.Map.Patches.Values)
            {
                if (patch.IsSurfacePatch())
                {
                    ChangePatchProperties(patch);
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
            if (!patch.IsSurfacePatch())
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

    private void ChangePatchProperties(Patch patch)
    {
        modifiedPatchesIds.Add(patch.ID);
        AdjustBackground(patch);
        AdjustEnvironment(patch);
        AddIceChunks(patch);
        LogEvent(patch);
    }

    private void AdjustBackground(Patch patch)
    {
        patch.CurrentSnapshot.Background = Background;
    }

    private void AdjustEnvironment(Patch patch)
    {
        var isIceShelf = patch.BiomeType == BiomeType.IceShelf;

        PatchEventProperties eventProperties = new()
        {
            SunlightAmbientMultiplier = Math.Min(GetSunlightMultiplier() * (isIceShelf ? 1.5f : 1), 1.0f),
            TemperatureAmbientFixedValue = isIceShelf ? random.Next(-7, 0) : random.Next(-4, 2),
        };

        patch.CurrentSnapshot.ActivePatchEvents.Add(PatchEventTypes.GlobalGlaciation, eventProperties);
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

    private int GetEventDuration()
    {
        switch (targetWorld.WorldSettings.ClimateInstability)
        {
            case WorldGenerationSettings.ClimateInstabilityEnum.Low:
                return random.Next(Math.Max((int)(Constants.GLOBAL_GLACIATION_MIN_DURATION / 1.5f), 1),
                    Math.Max((int)(Constants.GLOBAL_GLACIATION_MAX_DURATION / 1.5f), 1));
            case WorldGenerationSettings.ClimateInstabilityEnum.High:
                return random.Next((int)(Constants.GLOBAL_GLACIATION_MIN_DURATION * 1.5f),
                    (int)(Constants.GLOBAL_GLACIATION_MAX_DURATION * 1.5f));
            case WorldGenerationSettings.ClimateInstabilityEnum.Medium:
            default:
                return random.Next(Constants.GLOBAL_GLACIATION_MIN_DURATION,
                    Constants.GLOBAL_GLACIATION_MAX_DURATION);
        }
    }

    /// <summary>
    ///   Decides what amount the sunlight level should be multiplied or dived by
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The ClimateInstability should not be changed after the world is created otherwise it will break.
    ///     If it is to be changed then this code needs a refactor
    ///   </para>
    /// </remarks>
    private float GetSunlightMultiplier()
    {
        switch (targetWorld.WorldSettings.ClimateInstability)
        {
            case WorldGenerationSettings.ClimateInstabilityEnum.Low:
                return Math.Min(Constants.GLOBAL_GLACIATION_SUNLIGHT_MULTIPLICATION * 1.25f, 1);
            case WorldGenerationSettings.ClimateInstabilityEnum.High:
                return Constants.GLOBAL_GLACIATION_SUNLIGHT_MULTIPLICATION * 0.75f;
            case WorldGenerationSettings.ClimateInstabilityEnum.Medium:
            default:
                return Constants.GLOBAL_GLACIATION_SUNLIGHT_MULTIPLICATION;
        }
    }

    private void LogEvent(Patch patch)
    {
        patch.LogEvent(new LocalizedString("GLOBAL_GLACIATION_EVENT"),
            true, true, "GlobalGlaciationEvent.svg");
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
        foreach (var patchId in modifiedPatchesIds)
        {
            if (!targetWorld.Map.Patches.TryGetValue(patchId, out var patch))
            {
                GD.PrintErr("Patch exited the world in global glaciation event");
                continue;
            }

            PatchSnapshot patchSnapshot = patch.History[eventDuration];

            ResetBackground(patch, patchSnapshot);
            ResetEnvironment(patch);
            RemoveChunks(patch);
        }

        LogEndOfGlaciation();
    }

    private void ResetBackground(Patch patch, PatchSnapshot patchSnapshot)
    {
        patch.CurrentSnapshot.Background = patchSnapshot.Background;
    }

    private void ResetEnvironment(Patch patch)
    {
        patch.CurrentSnapshot.ActivePatchEvents.Remove(PatchEventTypes.GlobalGlaciation);
    }

    private void RemoveChunks(Patch patch)
    {
        foreach (var configuration in IceChunksConfigurations)
        {
            patch.Biome.Chunks.Remove(configuration);
        }
    }
}
