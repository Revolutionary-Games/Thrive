using System;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
public class WorldGenerationSettings
{
    public enum DifficultyPreset
    {
        Easy,
        Normal,
        Hard,
        Custom,
    }

    public enum LifeOrigin
    {
        Vent,
        Pond,
        Panspermia,
    }

    public bool LAWK { get; set; }
    public DifficultyPreset Difficulty { get; set; } = DifficultyPreset.Normal;
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;
    public int Seed { get; set; } = new Random().Next();
    public double MPMultiplier { get; set; } = 1;

    public override string ToString()
    {
        return "World generation settings: [LAWK: " + LAWK + ", Difficulty preset: " + Difficulty + ", Life origin: " + Origin + ", Seed: " + Seed + ", MP multiplier: " + MPMultiplier + "]";
    }

    public static double GetMPMultiplier(DifficultyPreset preset)
    {
        switch (preset)
        {
            case DifficultyPreset.Easy:
                return 0.5;
            case DifficultyPreset.Normal:
                return 1;
            case DifficultyPreset.Hard:
                return 1.5;
            default:
                return 1;
        }
    }
}
