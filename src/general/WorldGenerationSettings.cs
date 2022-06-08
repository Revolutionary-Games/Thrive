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

    /*
    Static values for min/max and each difficulty preset
    */

    public const double MIN_MP_MULTIPLIER = 0.2;
    public const double MAX_MP_MULTIPLIER = 1.2;
    public const double MIN_COMPOUND_DENSITY = 0.2;
    public const double MAX_COMPOUND_DENSITY = 1.2;

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

    /*
    Values for this particular object
    */

    public bool LAWK { get; set; }
    public DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;
    public int Seed { get; set; } = new Random().Next();
    public double MPMultiplier { get; set; } = 1;
    public double CompoundDensity { get; set; } = 1;

    public override string ToString()
    {
        return "World generation settings: [LAWK: " + LAWK + ", Difficulty preset: " + Difficulty + ", Life origin: " + Origin + ", Seed: " + Seed + ", MP multiplier: " + MPMultiplier + "]";
    }
}
