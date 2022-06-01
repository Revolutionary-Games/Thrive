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
    }

    public enum LifeOrigin
    {
        Vent,
        Pond,
        Panspermia,
    }

    public bool LAWK { get; set; }
    public DifficultyPreset difficultyPreset { get; set; }
    public LifeOrigin lifeOrigin { get; set; }
    public int seed { get; set; }

    public override string ToString()
    {
        return "World generation settings: [LAWK: " + LAWK + ", Difficulty preset: " + difficultyPreset + ", Life origin: " + lifeOrigin + ", Seed: " + seed + "]";
    }
}
