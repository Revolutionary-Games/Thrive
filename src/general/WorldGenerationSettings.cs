using System;
using System.ComponentModel;
using System.Globalization;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
public class WorldGenerationSettings
{
    [JsonConstructor]
    public WorldGenerationSettings(IDifficulty difficulty)
    {
        if (difficulty is DifficultyPreset preset && preset.InternalName ==
            SimulationParameters.Instance.GetDifficultyPreset("custom").InternalName)
        {
            GD.PrintErr(
                $"Ignoring setting custom difficulty preset object to {nameof(WorldGenerationSettings)} " +
                "(using normal instead). This should only happen when loading older saves");
            Difficulty = SimulationParameters.Instance.GetDifficultyPreset("normal");
        }
        else
        {
            Difficulty = difficulty;
        }
    }

    public WorldGenerationSettings()
    {
        // Default to normal difficulty unless otherwise specified
        Difficulty = SimulationParameters.Instance.GetDifficultyPreset("normal");
    }

    public enum LifeOrigin
    {
        [Description("LIFE_ORIGIN_VENTS")]
        Vent,

        [Description("LIFE_ORIGIN_POND")]
        Pond,

        [Description("LIFE_ORIGIN_PANSPERMIA")]
        Panspermia,
    }

    public enum PatchMapType
    {
        [Description("PATCH_MAP_TYPE_PROCEDURAL")]
        Procedural,

        [Description("PATCH_MAP_TYPE_CLASSIC")]
        Classic,
    }

    /// <summary>
    ///   Whether this game is restricted to only LAWK parts and abilities
    /// </summary>
    public bool LAWK { get; set; }

    /// <summary>
    ///   Chosen difficulty for this game
    /// </summary>
    public IDifficulty Difficulty { get; set; }

    /// <summary>
    ///   Origin of life (starting location) on this planet
    /// </summary>
    public LifeOrigin Origin { get; set; } = LifeOrigin.Vent;

    /// <summary>
    ///   Random seed for generating this game's planet
    /// </summary>
    public int Seed { get; set; } = new Random().Next();

    // The following are helper proxies to the values from the difficulty
    [JsonIgnore]
    public float MPMultiplier => Difficulty.MPMultiplier;

    [JsonIgnore]
    public float AIMutationMultiplier => Difficulty.AIMutationMultiplier;

    [JsonIgnore]
    public float CompoundDensity => Difficulty.CompoundDensity;

    [JsonIgnore]
    public float PlayerDeathPopulationPenalty => Difficulty.PlayerDeathPopulationPenalty;

    [JsonIgnore]
    public float GlucoseDecay => Difficulty.GlucoseDecay;

    [JsonIgnore]
    public float OsmoregulationMultiplier => Difficulty.OsmoregulationMultiplier;

    [JsonIgnore]
    public bool FreeGlucoseCloud => Difficulty.FreeGlucoseCloud;

    [JsonIgnore]
    public bool PassiveGainOfReproductionCompounds => Difficulty.PassiveReproduction;

    [JsonIgnore]
    public bool LimitReproductionCompoundUseSpeed => Difficulty.LimitGrowthRate;

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
    public IAutoEvoConfiguration AutoEvoConfiguration { get; set; } =
        SimulationParameters.Instance.AutoEvoConfiguration;

    public override string ToString()
    {
        return "World generation settings: [" +
            $"LAWK: {LAWK}" +
            $", Difficulty: {Difficulty.GetDescriptionString()}" +
            $", Life origin: {Origin}" +
            $", Seed: {Seed}" +
            $", Map type: {MapType}" +
            $", Include Multicellular: {IncludeMulticellular}" +
            $", Easter eggs: {EasterEggs}" +
            "]";
    }

    public string GetTranslatedDifficultyString()
    {
        string translatedDifficulty = Difficulty is DifficultyPreset difficulty ?
            difficulty.Name :
            TranslationServer.Translate("DIFFICULTY_PRESET_CUSTOM");

        return string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("DIFFICULTY_DETAILS_STRING"),
            translatedDifficulty,
            MPMultiplier,
            AIMutationMultiplier,
            CompoundDensity,
            PlayerDeathPopulationPenalty,
            GlucoseDecay,
            OsmoregulationMultiplier,
            TranslationHelper.TranslateBool(FreeGlucoseCloud),
            TranslationHelper.TranslateBool(PassiveGainOfReproductionCompounds),
            TranslationHelper.TranslateBool(LimitReproductionCompoundUseSpeed));
    }

    public string GetTranslatedPlanetString()
    {
        return string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("PLANET_DETAILS_STRING"),
            TranslationServer.Translate(MapType.GetAttribute<DescriptionAttribute>()?.Description),
            TranslationHelper.TranslateBool(LAWK),
            TranslationServer.Translate(Origin.GetAttribute<DescriptionAttribute>()?.Description),
            Seed);
    }

    public string GetTranslatedMiscString()
    {
        return string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("MISC_DETAILS_STRING"),
            TranslationHelper.TranslateBool(IncludeMulticellular),
            TranslationHelper.TranslateBool(EasterEggs));
    }
}
