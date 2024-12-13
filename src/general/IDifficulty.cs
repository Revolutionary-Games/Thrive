﻿using ThriveScriptsShared;

/// <summary>
///   Game difficulty data
/// </summary>
[SupportsCustomizedRegistryType(typeof(CustomDifficulty))]
public interface IDifficulty : IRegistryAssignable
{
    /// <summary>
    ///   Multiplier for MP costs in the editor
    /// </summary>
    public float MPMultiplier { get; }

    /// <summary>
    ///   Multiplier for AI species mutation rate
    /// </summary>
    public float AIMutationMultiplier { get; }

    /// <summary>
    ///   Multiplier for compound cloud density in the environment
    /// </summary>
    public float CompoundDensity { get; }

    /// <summary>
    ///   Multiplier for player species population loss after player death
    /// </summary>
    public float PlayerDeathPopulationPenalty { get; }

    /// <summary>
    ///   Multiplier for rate of glucose decay in the environment
    /// </summary>
    public float GlucoseDecay { get; }

    /// <summary>
    ///   Multiplier for player species osmoregulation cost
    /// </summary>
    public float OsmoregulationMultiplier { get; }

    /// <summary>
    ///   Whether the player starts with a free glucose cloud each time they exit the editor
    /// </summary>
    public bool FreeGlucoseCloud { get; }

    /// <summary>
    ///   Whether microbes get free reproduction compounds at a steady background rate
    /// </summary>
    public bool PassiveReproduction { get; }

    /// <summary>
    ///   Sets what happens when the player reproduces to their stored compounds
    /// </summary>
    public ReproductionCompoundHandling ReproductionCompounds { get; }

    /// <summary>
    ///   Whether the player is allowed to switch to a related species on extinction (so can continue instead of
    ///   losing the game)
    /// </summary>
    public bool SwitchSpeciesOnExtinction { get; }

    /// <summary>
    ///   Whether microbes are limited in how fast they can consume reproduction compounds to grow
    /// </summary>
    public bool LimitGrowthRate { get; }

    /// <summary>
    ///   How intense should the fog-of-war be
    /// </summary>
    public FogOfWarMode FogOfWarMode { get; }

    /// <summary>
    ///   Whether organelle unlocks are enabled or not
    ///   If not, all organelles are unlocked by default
    /// </summary>
    public bool OrganelleUnlocksEnabled { get; }

    /// <summary>
    ///   Sets a temporary value that overrides the normal growth rate
    /// </summary>
    public void SetGrowthRateLimitCheatOverride(bool limitGrowthRate);

    public void ClearGrowthRateLimitOverride();
}

public static class DifficultyHelpers
{
    public static CustomDifficulty Clone(this IDifficulty difficulty)
    {
        return new CustomDifficulty
        {
            MPMultiplier = difficulty.MPMultiplier,
            AIMutationMultiplier = difficulty.AIMutationMultiplier,
            CompoundDensity = difficulty.CompoundDensity,
            PlayerDeathPopulationPenalty = difficulty.PlayerDeathPopulationPenalty,
            GlucoseDecay = difficulty.GlucoseDecay,
            OsmoregulationMultiplier = difficulty.OsmoregulationMultiplier,
            FreeGlucoseCloud = difficulty.FreeGlucoseCloud,
            PassiveReproduction = difficulty.PassiveReproduction,
            SwitchSpeciesOnExtinction = difficulty.SwitchSpeciesOnExtinction,
            LimitGrowthRate = difficulty.LimitGrowthRate,
            FogOfWarMode = difficulty.FogOfWarMode,
            OrganelleUnlocksEnabled = difficulty.OrganelleUnlocksEnabled,
        };
    }

    public static string GetDescriptionString(this IDifficulty difficulty)
    {
        if (difficulty is DifficultyPreset preset)
            return $"{preset.InternalName} preset";

        return $"custom: MP multiplier: {difficulty.MPMultiplier}" +
            $", AI mutation multiplier: {difficulty.AIMutationMultiplier}" +
            $", Compound density: {difficulty.CompoundDensity}" +
            $", Player death population penalty: {difficulty.PlayerDeathPopulationPenalty}" +
            $", Glucose decay: {difficulty.GlucoseDecay}" +
            $", Osmoregulation multiplier: {difficulty.OsmoregulationMultiplier}" +
            $", Free glucose cloud: {difficulty.FreeGlucoseCloud}" +
            $", Passive Reproduction: {difficulty.PassiveReproduction}" +
            $", Switch on Extinction: {difficulty.SwitchSpeciesOnExtinction}" +
            $", Limit Growth Rate: {difficulty.LimitGrowthRate}" +
            $", Fog Of War Mode: {difficulty.FogOfWarMode}" +
            $", Organelle Unlocks Enabled: {difficulty.OrganelleUnlocksEnabled}";
    }
}
