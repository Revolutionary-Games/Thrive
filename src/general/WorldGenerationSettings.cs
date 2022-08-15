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

    /// <summary>
    ///   Whether this game is restricted to only LAWK parts and abilities
    /// </summary>
    public bool LAWK { get; set; }

    /// <summary>
    ///   Chosen difficulty preset for this game
    /// </summary>
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
            PassiveGainOfReproductionCompounds = value.PassiveReproduction;
            LimitReproductionCompoundUseSpeed = value.LimitGrowthRate;
        }
    }

    /// <summary>
    ///   Origin of life (starting location) on this planet
    /// </summary>
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;

    /// <summary>
    ///   Random seed for generating this game's planet
    /// </summary>
    public int Seed { get; set; } = new Random().Next();

    /// <summary>
    ///   Multiplier for MP costs in the editor
    /// </summary>
    public float MPMultiplier { get; set; }

    /// <summary>
    ///   Multiplier for AI species mutation rate
    /// </summary>
    public float AIMutationMultiplier { get; set; }

    /// <summary>
    ///   Multiplier for compound cloud density in the environment
    /// </summary>
    public float CompoundDensity { get; set; }

    /// <summary>
    ///   Multiplier for player species population loss after player death
    /// </summary>
    public float PlayerDeathPopulationPenalty { get; set; }

    /// <summary>
    ///   Multiplier for rate of glucose decay in the environment
    /// </summary>
    public float GlucoseDecay { get; set; }

    /// <summary>
    ///   Multiplier for player species osmoregulation cost
    /// </summary>
    public float OsmoregulationMultiplier { get; set; }

    /// <summary>
    ///  Whether the player starts with a free glucose cloud each time they exit the editor
    /// </summary>
    public bool FreeGlucoseCloud { get; set; }

    /// <summary>
    ///  Whether microbes get free reproduction compounds at a steady background rate
    /// </summary>
    public bool PassiveGainOfReproductionCompounds { get; set; } = true;

    /// <summary>
    ///  Whether microbes are limited in how fast they can consume reproduction compounds to grow
    /// </summary>
    public bool LimitReproductionCompoundUseSpeed { get; set; } = true;

    /// <summary>
    ///  Basic patch map generation type (procedural or the static classic map)
    /// </summary>
    public PatchMapType MapType { get; set; } = PatchMapType.Procedural;

    /// <summary>
    ///  Whether the player can enter the Multicellular Stage in this game
    /// </summary>
    public bool IncludeMulticellular { get; set; } = true;

    /// <summary>
    ///  Whether Easter eggs are enabled in this game
    /// </summary>
    public bool EasterEggs { get; set; } = true;

    /// <summary>
    ///   The auto-evo configuration this world uses
    /// </summary>
    public AutoEvoConfiguration AutoEvoConfiguration { get; set; } = SimulationParameters.Instance.AutoEvoConfiguration;

    public override string ToString()
    {
        return "World generation settings: [" +
            $"LAWK: {LAWK}" +
            $", Difficulty preset: {Difficulty}" +
            $", Life origin: {Origin}" +
            $", Seed: {Seed}" +
            $", MP multiplier: {MPMultiplier}" +
            $", AI mutation multiplier: {AIMutationMultiplier}" +
            $", Compound density: {CompoundDensity}" +
            $", Player death population penalty: {PlayerDeathPopulationPenalty}" +
            $", Glucose decay: {GlucoseDecay}" +
            $", Osmoregulation multiplier: {OsmoregulationMultiplier}" +
            $", Free glucose cloud: {FreeGlucoseCloud}" +
            $", Passive Reproduction: {PassiveGainOfReproductionCompounds}" +
            $", Limit Growth Rate: {LimitReproductionCompoundUseSpeed}" +
            $", Map type: {MapType}" +
            $", Include Multicellular: {IncludeMulticellular}" +
            $", Easter eggs: {EasterEggs}" +
            "]";
    }
}
