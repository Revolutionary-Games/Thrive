namespace AutoEvo;

using System;
using System.Collections.Generic;
using Systems;

/// <summary>
///   Caches some information in auto-evo runs to speed them up
/// </summary>
/// <remarks>
///   <para>
///     Some information will get outdated when data that the auto-evo relies on changes. If in the future
///     caching is moved to a higher level in the auto-evo, that needs to be considered.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     TODO: would be better to reuse instances of this class after clearing them for next use
///   </para>
/// </remarks>
public class SimulationCache
{
    private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

    private readonly WorldGenerationSettings worldSettings;
    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), EnergyBalanceInfo> cachedEnergyBalances = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseSpeeds = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseHexSizes = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedStorageCapacities = new();
    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, Compound), float> cachedCompoundScores = new();

    private readonly Dictionary<(TweakedProcess, BiomeConditions), ProcessSpeedInformation> cachedProcessSpeeds =
        new();

    private readonly Dictionary<MicrobeSpecies, (float, float, float)> cachedPredationToolsRawScores = new();

    private readonly Dictionary<(OrganelleDefinition, BiomeConditions, Compound), float>
        cachedEnergyCreationScoreForOrganelle = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, Compound), float>
        cachedEnergyCreationScoreForSpecies = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), bool> cachedUsesVaryingCompounds = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), float> cachedStorageScores = new();

    public SimulationCache(WorldGenerationSettings worldSettings)
    {
        this.worldSettings = worldSettings;
    }

    public EnergyBalanceInfo GetEnergyBalanceForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions);

        if (cachedEnergyBalances.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // Auto-evo uses the average values of compound during the course of a simulated day
        cached = ProcessSystem.ComputeEnergyBalance(species.Organelles, biomeConditions, species.MembraneType,
            true, species.PlayerSpecies, worldSettings, CompoundAmountType.Average);

        cachedEnergyBalances.Add(key, cached);
        return cached;
    }

    public float GetBaseSpeedForSpecies(MicrobeSpecies species)
    {
        if (cachedBaseSpeeds.TryGetValue(species, out var cached))
        {
            return cached;
        }

        cached = species.BaseSpeed;

        cachedBaseSpeeds.Add(species, cached);
        return cached;
    }

    public float GetBaseHexSizeForSpecies(MicrobeSpecies species)
    {
        if (cachedBaseHexSizes.TryGetValue(species, out var cached))
        {
            return cached;
        }

        cached = species.BaseHexSize;

        cachedBaseHexSizes.Add(species, cached);
        return cached;
    }

    public float GetStorageCapacityForSpecies(MicrobeSpecies species)
    {
        if (cachedStorageCapacities.TryGetValue(species, out var cached))
            return cached;

        cached = species.StorageCapacity;

        cachedStorageCapacities.Add(species, cached);
        return cached;
    }

    public float GetCompoundUseScoreForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions,
        Compound compound)
    {
        var key = (species, biomeConditions, compound);

        if (cachedCompoundScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = 0.0f;

        // We check generation from all the processes of the cell../
        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                // ... that uses the given compound (regardless of usage)
                if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                {
                    var processEfficiency = GetProcessMaximumSpeed(process, biomeConditions).Efficiency;

                    cached += inputAmount * processEfficiency;
                }
            }
        }

        cachedCompoundScores.Add(key, cached);
        return cached;
    }

    public float GetEnergyCreationScoreForOrganelle(OrganelleDefinition organelle, BiomeConditions biomeConditions,
        Compound compound)
    {
        var key = (organelle, biomeConditions, compound);
        if (cachedEnergyCreationScoreForOrganelle.TryGetValue(key, out var cached))
            return cached;

        var energyCreationScore = 0.0f;

        // We check generation from all processes of the cell
        foreach (var process in organelle.RunnableProcesses)
        {
            // ... that uses the given compound...
            if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
            {
                var processEfficiency = GetProcessMaximumSpeed(process, biomeConditions).Efficiency;

                // ... and that produce glucose
                if (process.Process.Outputs.TryGetValue(glucose, out var glucoseAmount))
                {
                    // Better ratio means that we transform stuff more efficiently and need less input
                    var compoundRatio = glucoseAmount / inputAmount;

                    // Better output is a proxy for more time dedicated to reproduction than energy production
                    var absoluteOutput = glucoseAmount * processEfficiency;

                    energyCreationScore += (float)(
                        Math.Pow(compoundRatio, Constants.AUTO_EVO_COMPOUND_RATIO_POWER_BIAS)
                        * Math.Pow(absoluteOutput, Constants.AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS)
                        * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER);
                }

                // ... and that produce ATP
                if (process.Process.Outputs.TryGetValue(atp, out var atpAmount))
                {
                    // Better ratio means that we transform stuff more efficiently and need less input
                    var compoundRatio = atpAmount / inputAmount;

                    // Better output is a proxy for more time dedicated to reproduction than energy production
                    var absoluteOutput = atpAmount * processEfficiency;

                    energyCreationScore += (float)(
                        Math.Pow(compoundRatio, Constants.AUTO_EVO_COMPOUND_RATIO_POWER_BIAS)
                        * Math.Pow(absoluteOutput, Constants.AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS)
                        * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER);
                }
            }
        }

        cachedEnergyCreationScoreForOrganelle.Add(key, energyCreationScore);
        return energyCreationScore;
    }

    /// <summary>
    ///   A measure of how good the species is for generating energy from a given compound.
    /// </summary>
    /// <returns>
    ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
    ///   so different implementations do not need to worry about scale.
    /// </returns>
    public float GetEnergyGenerationScoreForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions,
        Compound compound)
    {
        var key = (species, biomeConditions, compound);

        if (cachedEnergyCreationScoreForSpecies.TryGetValue(key, out var cached))
            return cached;

        var energyCreationScore = 0.0f;

        // We check generation from all the processes of the cell.
        foreach (var organelle in species.Organelles)
        {
            energyCreationScore += GetEnergyCreationScoreForOrganelle(organelle.Definition, biomeConditions,
                compound);
        }

        cachedEnergyCreationScoreForSpecies.Add(key, energyCreationScore);
        return energyCreationScore;
    }

    /// <summary>
    ///   Calculates a maximum speed for a process that can happen given the environmental. Environmental compounds
    ///   are always used at the average amount in auto-evo.
    /// </summary>
    /// <param name="process">The process to calculate the speed for</param>
    /// <param name="biomeConditions">The biome conditions to use</param>
    /// <returns>The speed information for the process</returns>
    public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, BiomeConditions biomeConditions)
    {
        var key = (process, biomeConditions);

        if (cachedProcessSpeeds.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = ProcessSystem.CalculateProcessMaximumSpeed(process, biomeConditions, CompoundAmountType.Average);

        cachedProcessSpeeds.Add(key, cached);
        return cached;
    }

    public bool GetUsesVaryingCompoundsForSpecies(MicrobeSpecies species, BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions);

        if (cachedUsesVaryingCompounds.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = MicrobeInternalCalculations.UsesDayVaryingCompounds(species.Organelles, biomeConditions, null);

        cachedUsesVaryingCompounds.Add(key, cached);
        return cached;
    }

    public float GetStorageAndDayGenerationScore(MicrobeSpecies species, BiomeConditions biomeConditions,
        Compound compound)
    {
        var key = (species, biomeConditions);

        if (cachedStorageScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = CalculateStorageScore(species, biomeConditions, compound);

        cachedStorageScores.Add(key, cached);
        return cached;
    }

    public bool MatchesSettings(WorldGenerationSettings checkAgainst)
    {
        return worldSettings.Equals(checkAgainst);
    }

    public (float PilusScore, float OxytoxyScore, float MucilageScore) GetPredationToolsRawScores(
        MicrobeSpecies microbeSpecies)
    {
        if (cachedPredationToolsRawScores.TryGetValue(microbeSpecies, out var cached))
            return cached;

        var pilusScore = 0.0f;
        var oxytoxyScore = 0.0f;
        var mucilageScore = 0.0f;

        foreach (var organelle in microbeSpecies.Organelles)
        {
            if (organelle.Definition.HasPilusComponent)
            {
                pilusScore += Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
                continue;
            }

            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.TryGetValue(oxytoxy, out var oxytoxyAmount))
                {
                    oxytoxyScore += oxytoxyAmount * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }

                if (process.Process.Outputs.TryGetValue(mucilage, out var mucilageAmount))
                {
                    mucilageScore += mucilageAmount * Constants.AUTO_EVO_MUCILAGE_PREDATION_SCORE;
                }
            }
        }

        var predationToolsRawScores = (pilusScore, oxytoxyScore, mucilageScore);

        cachedPredationToolsRawScores.Add(microbeSpecies, predationToolsRawScores);
        return predationToolsRawScores;
    }

    private float CalculateStorageScore(MicrobeSpecies species, BiomeConditions biomeConditions, Compound compound)
    {
        // TODO: maybe a bit lower value to determine when moving kicks in (though optimally the calculation could
        // take in a float in range 0-1 to make much more gradual behaviour changes possible)
        var moving = species.Behaviour.Activity >= Constants.AI_ACTIVITY_TO_BE_FULLY_ACTIVE_DURING_NIGHT;

        float daySeconds = worldSettings.DayLength * worldSettings.DaytimeFraction;

        var cachedCapacities =
            MicrobeInternalCalculations.GetTotalSpecificCapacity(species.Organelles, out var cachedCapacity);

        Dictionary<Compound, CompoundBalance>? dayCompoundBalances = null;
        var (canSurvive, requiredAmounts) = MicrobeInternalCalculations.CalculateNightStorageRequirements(
            species.Organelles, species.MembraneType, moving, species.PlayerSpecies, biomeConditions, worldSettings,
            ref dayCompoundBalances);

        if (dayCompoundBalances == null)
            throw new Exception("Day compound balance should have been calculated");

        var resultCompounds =
            MicrobeInternalCalculations.GetCompoundsProducedByProcessesTakingIn(compound, species.Organelles);

        float cacheScore = 0;
        int scoreCount = 0;

        foreach (var requiredAmount in requiredAmounts)
        {
            // Handle only the relevant compound type
            if (requiredAmount.Value <= 0 ||
                (!resultCompounds.Contains(requiredAmount.Key) && requiredAmount.Key != compound))
            {
                continue;
            }

            cacheScore += cachedCapacities.GetValueOrDefault(requiredAmount.Key, cachedCapacity) / requiredAmount.Value;
            ++scoreCount;
        }

        if (scoreCount == 0)
        {
            // No scores (maybe all production is negative or irrelevant compound type)
            return 1;
        }

        // Additionally penalize species that cannot generate enough compounds during the day to fill required
        // amount of storage
        foreach (var handledCompound in resultCompounds)
        {
            if (!dayCompoundBalances.TryGetValue(handledCompound, out var dayBalance) || !(dayBalance.Balance >= 0))
                continue;

            var dayGenerated = dayBalance.Balance * daySeconds;
            var required = requiredAmounts.GetValueOrDefault(handledCompound, 0);

            if (!(dayGenerated < required))
                continue;

            if (required <= 0)
                throw new Exception("Required compound amount should not be zero or negative");

            float insufficientProductionScore = dayGenerated / required;
            cacheScore *= insufficientProductionScore;
        }

        cacheScore /= scoreCount;

        // Extra penalty if cell cannot store enough stuff to survive to make that situation much more harsh
        if (!canSurvive)
        {
            cacheScore *= Constants.AUTO_EVO_NIGHT_STORAGE_NOT_ENOUGH_PENALTY;
        }

        return Math.Clamp(cacheScore, 0, Constants.AUTO_EVO_MAX_BONUS_FROM_ENVIRONMENTAL_STORAGE);
    }
}
