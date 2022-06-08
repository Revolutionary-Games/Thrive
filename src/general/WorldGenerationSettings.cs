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
    Static values for min/max and each difficulty preset
    */

    public const double MIN_MP_MULTIPLIER = 0.2;
    public const double MAX_MP_MULTIPLIER = 2;
    public const double MIN_COMPOUND_DENSITY = 0.2;
    public const double MAX_COMPOUND_DENSITY = 2;
    public const int MIN_PLAYER_DEATH_POPULATION_PENALTY = 10;
    public const int MAX_PLAYER_DEATH_POPULATION_PENALTY = 100;
    public const double MIN_GLUCOSE_DECAY = 0.3;
    public const double MAX_GLUCOSE_DECAY = 0.95;

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

    public static int GetPlayerDeathPopulationPenalty(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 10;
            case DifficultyPreset.Normal:
                return 20;
            case DifficultyPreset.Hard:
                return 50;
            default:
                return 20;
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

    public static bool GetFreeGlucoseCloud(DifficultyPreset preset)
    {
        return preset == DifficultyPreset.Easy;
    }

    /*
    Values for this particular object
    */

    public bool LAWK { get; set; }
    public DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;
    public int Seed { get; set; } = new Random().Next();
    public double MPMultiplier { get; set; } = 1;
    public double CompoundDensity { get; set; } = 1;
    public int PlayerDeathPopulationPenalty { get; set; } = 20;
    public double GlucoseDecay { get; set; } = 0.8;
    public bool FreeGlucoseCloud { get; set; }
    public PatchMapType MapType { get; set; } = PatchMapType.Procedural;
    public bool IncludeMulticellular { get; set; } = true;

    public override string ToString()
    {
        return "World generation settings:" +
        "[LAWK: " + LAWK +
        ", Difficulty preset: " + Difficulty +
        ", Life origin: " + Origin +
        ", Seed: " + Seed +
        ", MP multiplier: " + MPMultiplier +
        ", Compound density: " + CompoundDensity +
        ", Player death population penalty: " + PlayerDeathPopulationPenalty +
        ", Glucose decay: " + GlucoseDecay +
        ", Free glucose cloud: " + FreeGlucoseCloud +
        ", Map type: " + MapType + 
        ", Include Multicellular: " + IncludeMulticellular +
        "]";
    }
}
