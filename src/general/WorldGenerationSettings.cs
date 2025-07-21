using System;
using System.ComponentModel;
using Godot;
using Newtonsoft.Json;
using Xoshiro.PRNG64;

/// <summary>
///   Player configurable options for creating the game world
/// </summary>
[JsonObject(IsReference = true)]
public class WorldGenerationSettings
{
    [JsonConstructor]
    public WorldGenerationSettings(IDifficulty difficulty)
    {
        if (difficulty is DifficultyPreset preset && preset.InternalName ==
            SimulationParameters.Instance.GetDifficultyPreset("custom").InternalName)
        {
            GD.PrintErr($"Ignoring setting custom difficulty preset object to {nameof(WorldGenerationSettings)} " +
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

        var defaultDayNight = SimulationParameters.Instance.GetDayNightCycleConfiguration();

        HoursPerDay = defaultDayNight.HoursPerDay;
        DaytimeFraction = defaultDayNight.DaytimeFraction;
    }

    public enum LifeOrigin
    {
        [Description("LIFE_ORIGIN_VENTS")]
        Vent = 0,

        [Description("LIFE_ORIGIN_POND")]
        Pond = 1,

        [Description("LIFE_ORIGIN_PANSPERMIA")]
        Panspermia = 2,
    }

    /// <summary>
    ///   The possible world sizes used in Planet customization
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If changing the WorldSizeEnum members, always synchronize with the
    ///     explicit values in the WorldSize OptionButton
    ///   </para>
    /// </remarks>
    public enum WorldSizeEnum
    {
        [Description("WORLD_SIZE_SMALL")]
        Small = 0,

        [Description("WORLD_SIZE_MEDIUM")]
        Medium = 1,

        [Description("WORLD_SIZE_LARGE")]
        Large = 2,
    }

    /// <summary>
    ///   The possible temperature settings for the planet
    /// </summary>
    public enum WorldTemperatureEnum
    {
        [Description("WORLD_TEMPERATURE_COLD")]
        Cold = 0,

        [Description("WORLD_TEMPERATURE_TEMPERATE")]
        Temperate = 1,

        [Description("WORLD_TEMPERATURE_WARM")]
        Warm = 2,
    }

    /// <summary>
    ///   The possible sea level options for the planet
    /// </summary>
    public enum WorldOceanicCoverageEnum
    {
        [Description("WORLD_OCEANIC_COVERAGE_SMALL")]
        Small = 0,

        [Description("WORLD_OCEANIC_COVERAGE_MEDIUM")]
        Moderate = 1,

        [Description("WORLD_OCEANIC_COVERAGE_LARGE")]
        Large = 2,
    }

    /// <summary>
    ///   The geological activity levels of the planet
    /// </summary>
    public enum GeologicalActivityEnum
    {
        [Description("GEOLOGICAL_ACTIVITY_DORMANT")]
        Dormant = 0,

        [Description("GEOLOGICAL_ACTIVITY_AVERAGE")]
        Average = 1,

        [Description("GEOLOGICAL_ACTIVITY_ACTIVE")]
        Active = 2,
    }

    /// <summary>
    ///   The possible climate instability settings for the planet
    /// </summary>
    public enum ClimateInstabilityEnum
    {
        [Description("CLIMATE_STABILITY_STABLE")]
        Low = 0,

        [Description("CLIMATE_STABILITY_AVERAGE")]
        Medium = 1,

        [Description("CLIMATE_STABILITY_UNSTABLE")]
        High = 2,
    }

    /// <summary>
    ///   Whether this game is restricted to only LAWK parts and abilities
    /// </summary>
    public bool LAWK { get; set; }

    /// <summary>
    ///   Whether experimental features are enabled this game
    /// </summary>
    public bool ExperimentalFeatures { get; set; }

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
    public long Seed { get; set; } = new XoShiRo256starstar().Next64();

    /// <summary>
    ///   Size of World
    /// </summary>
    public WorldSizeEnum WorldSize { get; set; } = WorldSizeEnum.Medium;

    /// <summary>
    ///   Temperature of World
    /// </summary>
    public WorldTemperatureEnum WorldTemperature { get; set; } = WorldTemperatureEnum.Temperate;

    /// <summary>
    ///   Sea level of World
    /// </summary>
    public WorldOceanicCoverageEnum WorldOceanicCoverage { get; set; } = WorldOceanicCoverageEnum.Moderate;

    /// <summary>
    ///   Geological activity of World
    /// </summary>
    public GeologicalActivityEnum GeologicalActivity { get; set; } = GeologicalActivityEnum.Average;

    /// <summary>
    ///   Climate instability of World
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This value should not be changed as it might break world restoring from global glaciation event
    ///     <see cref="GlobalGlaciationEvent"/>. If it needs to be changed then the way the world is restored
    ///     needs to be changed as well.
    ///   </para>
    /// </remarks>
    public ClimateInstabilityEnum ClimateInstability { get; set; } = ClimateInstabilityEnum.Medium;

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
    public float PlayerAutoEvoStrength => Difficulty.PlayerAutoEvoStrength;

    [JsonIgnore]
    public FogOfWarMode FogOfWarMode => Difficulty.FogOfWarMode;

    [JsonIgnore]
    public bool FreeGlucoseCloud => Difficulty.FreeGlucoseCloud;

    [JsonIgnore]
    public bool SwitchSpeciesOnExtinction => Difficulty.SwitchSpeciesOnExtinction;

    [JsonIgnore]
    public bool LimitReproductionCompoundUseSpeed => Difficulty.LimitGrowthRate;

    /// <summary>
    ///   Whether the day/night cycle in this game is enabled
    /// </summary>
    public bool DayNightCycleEnabled { get; set; } = true;

    /// <summary>
    ///   Real-time length of a full day on the planet in seconds
    /// </summary>
    public int DayLength { get; set; } = Constants.DEFAULT_DAY_LENGTH;

    /// <inheritdoc cref="DayNightConfiguration.HoursPerDay"/>
    public float HoursPerDay { get; set; }

    /// <inheritdoc cref="DayNightConfiguration.DaytimeFraction"/>
    public float DaytimeFraction { get; set; }

    /// <summary>
    ///   Whether the player can enter the Multicellular Stage in this game
    /// </summary>
    public bool IncludeMulticellular { get; set; } = true;

    /// <summary>
    ///   Whether Easter eggs are enabled in this game
    /// </summary>
    public bool EasterEggs { get; set; } = true;

    /// <summary>
    ///   The auto-evo configuration this world uses
    /// </summary>
    public IAutoEvoConfiguration AutoEvoConfiguration { get; set; } =
        SimulationParameters.Instance.AutoEvoConfiguration;

    /// <summary>
    ///   Generates a formatted string containing translated difficulty details.
    /// </summary>
    public string GetTranslatedDifficultyString()
    {
        var translatedDifficulty = Difficulty is DifficultyPreset difficulty ?
            difficulty.Name :
            Localization.Translate("DIFFICULTY_PRESET_CUSTOM");

        return Localization.Translate("DIFFICULTY_DETAILS_STRING").FormatSafe(translatedDifficulty,
            MPMultiplier,
            AIMutationMultiplier,
            CompoundDensity,
            PlayerDeathPopulationPenalty,
            Localization.Translate("PERCENTAGE_VALUE").FormatSafe(Math.Round(GlucoseDecay * 100, 1)),
            OsmoregulationMultiplier,
            TranslationHelper.TranslateFeatureFlag(FreeGlucoseCloud),
            TranslationHelper.TranslateFeatureFlag(LimitReproductionCompoundUseSpeed));
    }

    /// <summary>
    ///   Generates a formatted string containing translated planet details.
    /// </summary>
    public string GetTranslatedPlanetString()
    {
        return Localization.Translate("PLANET_DETAILS_STRING").FormatSafe(TranslationHelper.TranslateFeatureFlag(LAWK),
            Localization.Translate(Origin.GetAttribute<DescriptionAttribute>().Description),
            TranslationHelper.TranslateFeatureFlag(DayNightCycleEnabled),
            DayLength,
            Seed);
    }

    /// <summary>
    ///   Generates a formatted string containing translated miscellaneous details.
    /// </summary>
    public string GetTranslatedMiscString()
    {
        return Localization.Translate("WORLD_MISC_DETAILS_STRING").FormatSafe(
            TranslationHelper.TranslateFeatureFlag(IncludeMulticellular),
            TranslationHelper.TranslateFeatureFlag(EasterEggs));
    }

    public override string ToString()
    {
        return "World generation settings: [" +
            $"LAWK: {LAWK}" +
            $", Difficulty: {Difficulty.GetDescriptionString()}" +
            $", Life origin: {Origin}" +
            $", Seed: {Seed}" +
            $", Size: {WorldSize}" +
            $", Day/night cycle enabled: {DayNightCycleEnabled}" +
            $", Day length: {DayLength}" +
            $", Include multicellular: {IncludeMulticellular}" +
            $", Easter eggs: {EasterEggs}" +
            "]";
    }
}
