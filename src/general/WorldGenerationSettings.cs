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
    public DifficultyPreset difficultyPreset { get; set; } = DifficultyPreset.Normal;
    public LifeOrigin lifeOrigin { get; set; } = LifeOrigin.Vent;
    public int seed { get; set; } = new Random().Next();

    public override string ToString()
    {
        return "World generation settings: [LAWK: " + LAWK + ", Difficulty preset: " + difficultyPreset + ", Life origin: " + lifeOrigin + ", Seed: " + seed + "]";
    }
}
