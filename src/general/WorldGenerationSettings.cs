using System;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
public class WorldGenerationSettings
{
    /*
    Static values for min/max
    */

    public const double MIN_MP_MULTIPLIER = 0.2;
    public const double MAX_MP_MULTIPLIER = 2;
    public const double MIN_AI_MUTATION_RATE = 0.5;
    public const double MAX_AI_MUTATION_RATE = 5;
    public const double MIN_COMPOUND_DENSITY = 0.2;
    public const double MAX_COMPOUND_DENSITY = 2;
    public const double MIN_PLAYER_DEATH_POPULATION_PENALTY = 1;
    public const double MAX_PLAYER_DEATH_POPULATION_PENALTY = 5;
    public const double MIN_GLUCOSE_DECAY = 0.3;
    public const double MAX_GLUCOSE_DECAY = 0.95;
    public const double MIN_OSMOREGULATION_MULTIPLIER = 0.2;
    public const double MAX_OSMOREGULATION_MULTIPLIER = 2;

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
    public double MPMultiplier { get; set; } = 1;
    public double AIMutationMultiplier { get; set; } = 1;
    public double CompoundDensity { get; set; } = 1;
    public double PlayerDeathPopulationPenalty { get; set; } = 1;
    public double GlucoseDecay { get; set; } = 0.8;
    public double OsmoregulationMultiplier { get; set; } = 1;
    public bool FreeGlucoseCloud { get; set; }
    public PatchMapType MapType { get; set; } = PatchMapType.Procedural;
    public bool IncludeMulticellular { get; set; } = true;
    public bool EasterEggs { get; set; } = true;

    /*
    Static values for each difficulty preset
    */

    public static double GetMPMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.8;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 1.2;
            default:
                return 1;
        }
    }

    public static double GetAIMutationMultiplier(DifficultyPreset preset)
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

    public static double GetCompoundDensity(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 1.5;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 0.5;
            default:
                return 1;
        }
    }

    public static double GetPlayerDeathPopulationPenalty(DifficultyPreset preset)
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

    public static double GetGlucoseDecay(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.9;
            case DifficultyPreset.Normal:
                return 0.8;
            case DifficultyPreset.Hard:
                return 0.5;
            default:
                return 0.8;
        }
    }

    public static double GetOsmoregulationMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.8;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 1.2;
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
