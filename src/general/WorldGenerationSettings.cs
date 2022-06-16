using System;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
public class WorldGenerationSettings
{
    public enum LifeOrigin
    {
        Vent,
        Pond,
        Panspermia,
    }

    public enum PatchMapType
    {
        Procedural,
        Classic,
    }

    /*
    Values for this particular object
    */

    public bool Lawk { get; set; }
    public DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;
    public int Seed { get; set; } = new Random().Next();
    public float MPMultiplier { get; set; } = 1;
    public float AIMutationMultiplier { get; set; } = 1;
    public float CompoundDensity { get; set; } = 1;
    public float PlayerDeathPopulationPenalty { get; set; } = 1;
    public float GlucoseDecay { get; set; } = 0.8f;
    public float OsmoregulationMultiplier { get; set; } = 1;
    public bool FreeGlucoseCloud { get; set; }
    public PatchMapType MapType { get; set; } = PatchMapType.Procedural;
    public bool IncludeMulticellular { get; set; } = true;
    public bool EasterEggs { get; set; } = true;

    /*
    Static values for each difficulty preset
    */

    public static float GetMPMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.8f;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 1.2f;
            default:
                return 1;
        }
    }

    public static float GetAIMutationMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 1;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 2;
            default:
                return 1;
        }
    }

    public static float GetCompoundDensity(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 1.5f;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 0.5f;
            default:
                return 1;
        }
    }

    public static float GetPlayerDeathPopulationPenalty(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 1;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 2;
            default:
                return 1;
        }
    }

    public static float GetGlucoseDecay(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.9f;
            case DifficultyPreset.Normal:
                return 0.8f;
            case DifficultyPreset.Hard:
                return 0.5f;
            default:
                return 0.8f;
        }
    }

    public static float GetOsmoregulationMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.8f;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 1.2f;
            default:
                return 1;
        }
    }

    public static bool GetFreeGlucoseCloud(DifficultyPreset preset)
    {
        return preset != DifficultyPreset.Hard;
    }

    public override string ToString()
    {
        return "World generation settings: [" +
            "LAWK: " + Lawk +
            ", Difficulty preset: " + Difficulty +
            ", Life origin: " + Origin +
            ", Seed: " + Seed +
            ", MP multiplier: " + MPMultiplier +
            ", AI mutation multiplier: " + AIMutationMultiplier +
            ", Compound density: " + CompoundDensity +
            ", Player death population penalty: " + PlayerDeathPopulationPenalty +
            ", Glucose decay: " + GlucoseDecay +
            ", Osmoregulation multiplier: " + OsmoregulationMultiplier +
            ", Free glucose cloud: " + FreeGlucoseCloud +
            ", Map type: " + MapType +
            ", Include Multicellular: " + IncludeMulticellular +
            ", Easter eggs: " + EasterEggs +
            "]";
    }
}
