using System;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
public class WorldGenerationSettings
{
    private DifficultyPreset difficulty = null!;

    public WorldGenerationSettings()
    {
        // Default to normal difficulty unless otherwise specified
        Difficulty = SimulationParameters.Instance.GetDifficultyPreset("normal");
    }

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

    public bool LAWK { get; set; }

    public DifficultyPreset Difficulty
    {
        get => difficulty;
        set
        {
            difficulty = value;

            if (value.InternalName == "custom")
                return;

            MPMultiplier = value.MPMultiplier;
            AIMutationMultiplier = value.AIMutationMultiplier;
            CompoundDensity = value.CompoundDensity;
            PlayerDeathPopulationPenalty = value.PlayerDeathPopulationPenalty;
            GlucoseDecay = value.GlucoseDecay;
            OsmoregulationMultiplier = value.OsmoregulationMultiplier;
            FreeGlucoseCloud = value.FreeGlucoseCloud;
        }
    }

    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;
    public int Seed { get; set; } = new Random().Next();
    public float MPMultiplier { get; set; }
    public float AIMutationMultiplier { get; set; }
    public float CompoundDensity { get; set; }
    public float PlayerDeathPopulationPenalty { get; set; }
    public float GlucoseDecay { get; set; }
    public float OsmoregulationMultiplier { get; set; }
    public bool FreeGlucoseCloud { get; set; }
    public PatchMapType MapType { get; set; } = PatchMapType.Procedural;
    public bool IncludeMulticellular { get; set; } = true;
    public bool EasterEggs { get; set; } = true;

    public override string ToString()
    {
        return "World generation settings: [" +
            "LAWK: " + LAWK +
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
