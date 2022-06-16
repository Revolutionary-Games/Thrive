using System;
using Newtonsoft.Json;

/// <summary>
///   Definition for a difficulty preset
/// </summary>
/// <remarks>
///   <para>
///     Values for each difficulty preset, as given in difficulty_preset.json
///   </para>
/// </remarks>
public class DifficultyPreset : IRegistryType
{
    /// <summary>
    ///   User readable name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   Index for this difficulty preset in the preset menu
    /// </summary>
    public int Index;

    /// <summary>
    ///   Multiplier for MP costs in the editor
    /// </summary>
    public float MPMultiplier;

    /// <summary>
    ///   Multiplier for AI species mutation rate
    /// </summary>
    public float AIMutationMultiplier;

    /// <summary>
    ///   Multiplier for compound cloud density in the environment
    /// </summary>
    public float CompoundDensity;

    /// <summary>
    ///   Multiplier for player species population loss after player death
    /// </summary>
    public float PlayerDeathPopulationPenalty;

    /// <summary>
    ///   Multiplier for rate of glucose decay in the environment
    /// </summary>
    public float GlucoseDecay;

    /// <summary>
    ///   Multiplier for player species osmoregulation cost
    /// </summary>
    public float OsmoregulationMultiplier;

    /// <summary>
    ///  Whether the player starts with a free glucose cloud each time they exit the editor
    /// </summary>
    public bool FreeGlucoseCloud;

#pragma warning disable 169,649 // Used through reflection
    private string? untranslatedName;
#pragma warning restore 169,649

    public string InternalName { get; set; } = null!;

    [JsonIgnore]
    public string UntranslatedName =>
        untranslatedName ?? throw new InvalidOperationException("Translations not initialized");

    public void Check(string name)
    {
        if (string.IsNullOrEmpty(Name))
            throw new InvalidRegistryDataException(name, GetType().Name, "Name is not set");

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);

        if (Index < 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Index is negative");

        // All presets other than custom must have valid values set for every parameter
        if (InternalName == "custom")
            return;

        if (MPMultiplier is > Constants.MAX_MP_MULTIPLIER or < Constants.MIN_MP_MULTIPLIER)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Invalid MP multiplier: " + MPMultiplier);
        }

        if (AIMutationMultiplier is > Constants.MAX_AI_MUTATION_RATE or < Constants.MIN_AI_MUTATION_RATE)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid AI multiplier: " + AIMutationMultiplier);
        }

        if (CompoundDensity is > Constants.MAX_COMPOUND_DENSITY or < Constants.MIN_COMPOUND_DENSITY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid compound density: " + CompoundDensity);
        }

        if (PlayerDeathPopulationPenalty is > Constants.MAX_PLAYER_DEATH_POPULATION_PENALTY or
            < Constants.MIN_PLAYER_DEATH_POPULATION_PENALTY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid player death population penalty: " + PlayerDeathPopulationPenalty);
        }

        if (GlucoseDecay is > Constants.MAX_GLUCOSE_DECAY or < Constants.MIN_GLUCOSE_DECAY)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Invalid glucose decay: " + GlucoseDecay);
        }

        if (OsmoregulationMultiplier is > Constants.MAX_OSMOREGULATION_MULTIPLIER or
            < Constants.MIN_OSMOREGULATION_MULTIPLIER)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid osmoregulation multiplier: " + OsmoregulationMultiplier);
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
