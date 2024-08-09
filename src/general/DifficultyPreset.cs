using System;
using Newtonsoft.Json;

/// <summary>
///   Definition for a difficulty preset
/// </summary>
/// <remarks>
///   <para>
///     Values for each difficulty preset, as given in difficulty_presets.json
///   </para>
/// </remarks>
public class DifficultyPreset : IDifficulty, IRegistryType
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    private bool applyGrowthOverride;
    private bool growthLimitOverride;

    private bool limitGrowthRate;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    /// <summary>
    ///   Index for this difficulty preset in the preset menu
    /// </summary>
    [JsonProperty]
    public int Index { get; private set; }

    [JsonProperty]
    public float MPMultiplier { get; private set; }

    [JsonProperty]
    public float AIMutationMultiplier { get; private set; }

    [JsonProperty]
    public float CompoundDensity { get; private set; }

    [JsonProperty]
    public float PlayerDeathPopulationPenalty { get; private set; }

    [JsonProperty]
    public float GlucoseDecay { get; private set; }

    [JsonProperty]
    public float OsmoregulationMultiplier { get; private set; }

    [JsonProperty]
    public bool FreeGlucoseCloud { get; private set; }

    [JsonProperty]
    public bool PassiveReproduction { get; private set; }

    [JsonProperty]
    public bool SwitchSpeciesOnExtinction { get; private set; }

    [JsonProperty]
    public bool LimitGrowthRate
    {
        get
        {
            if (applyGrowthOverride)
                return growthLimitOverride;

            return limitGrowthRate;
        }
        private set => limitGrowthRate = value;
    }

    [JsonProperty]
    public FogOfWarMode FogOfWarMode { get; private set; }

    [JsonProperty]
    public bool OrganelleUnlocksEnabled { get; private set; }

    public string InternalName { get; set; } = null!;

    [JsonIgnore]
    public string UntranslatedName =>
        untranslatedName ?? throw new InvalidOperationException("Translations not initialized");

    public void SetGrowthRateLimitCheatOverride(bool newLimitSetting)
    {
        applyGrowthOverride = true;
        growthLimitOverride = newLimitSetting;
    }

    public void ClearGrowthRateLimitOverride()
    {
        applyGrowthOverride = false;
    }

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (Index < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Index is negative");

        // All presets other than custom must have valid values set for every parameter
        // Custom is used just as a placeholder item that is replaced with CustomDifficulty when used.
        if (InternalName == "custom")
            return;

        if (MPMultiplier is > Constants.MAX_MP_MULTIPLIER or < Constants.MIN_MP_MULTIPLIER)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, $"Invalid MP multiplier: {MPMultiplier}");
        }

        if (AIMutationMultiplier is > Constants.MAX_AI_MUTATION_RATE or < Constants.MIN_AI_MUTATION_RATE)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                $"Invalid AI multiplier: {AIMutationMultiplier}");
        }

        if (CompoundDensity is > Constants.MAX_COMPOUND_DENSITY or < Constants.MIN_COMPOUND_DENSITY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                $"Invalid compound density: {CompoundDensity}");
        }

        if (PlayerDeathPopulationPenalty is > Constants.MAX_PLAYER_DEATH_POPULATION_PENALTY or
            < Constants.MIN_PLAYER_DEATH_POPULATION_PENALTY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                $"Invalid player death population penalty: {PlayerDeathPopulationPenalty}");
        }

        if (GlucoseDecay is > Constants.MAX_GLUCOSE_DECAY or < Constants.MIN_GLUCOSE_DECAY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, $"Invalid glucose decay: {GlucoseDecay}");
        }

        if (OsmoregulationMultiplier is > Constants.MAX_OSMOREGULATION_MULTIPLIER or
            < Constants.MIN_OSMOREGULATION_MULTIPLIER)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                $"Invalid osmoregulation multiplier: {OsmoregulationMultiplier}");
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
