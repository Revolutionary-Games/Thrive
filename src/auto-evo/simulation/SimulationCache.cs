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
    public readonly Dictionary<(MicrobeSpecies, SelectionPressure), float> CachedPressureScores = new();

    private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
    private readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");

    private readonly WorldGenerationSettings worldSettings;
    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), EnergyBalanceInfo> cachedEnergyBalances = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseSpeeds = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseHexSizes = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, Compound, Compound), float> cachedCompoundScores =
        new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, Compound, Compound), float> cachedGeneratedCompound =
        new();

    private readonly Dictionary<(MicrobeSpecies, MicrobeSpecies, BiomeConditions), float> predationScores = new();

    private readonly Dictionary<(TweakedProcess, BiomeConditions), ProcessSpeedInformation> cachedProcessSpeeds =
        new();

    private readonly Dictionary<MicrobeSpecies, (float, float, float)> cachedPredationToolsRawScores = new();

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

        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(species.Organelles);

        // Auto-evo uses the average values of compound during the course of a simulated day
        cached = ProcessSystem.ComputeEnergyBalance(species.Organelles, biomeConditions, species.MembraneType,
            maximumMovementDirection, true, species.PlayerSpecies, worldSettings, CompoundAmountType.Average, this);

        cachedEnergyBalances.Add(key, cached);
        return cached;
    }

    // TODO: Both of these seem like something that could easily be stored on the species with OnEdited
    public float GetBaseSpeedForSpecies(MicrobeSpecies species)
    {
        if (cachedBaseSpeeds.TryGetValue(species, out var cached))
        {
            return cached;
        }

        cached = MicrobeInternalCalculations.CalculateSpeed(species.Organelles.Organelles, species.MembraneType,
            species.MembraneRigidity, species.IsBacteria, true);

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

    public float GetCompoundConversionScoreForSpecies(Compound fromCompound, Compound toCompound,
        MicrobeSpecies species, BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions, fromCompound, toCompound);

        if (cachedCompoundScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var compoundIn = 0.0f;
        var compoundOut = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(fromCompound, out var inputAmount))
                {
                    if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                    {
                        var processSpeed = GetProcessMaximumSpeed(process, biomeConditions).CurrentSpeed;

                        compoundIn += inputAmount;
                        compoundOut += outputAmount * processSpeed;
                    }
                }
            }
        }

        if (compoundIn <= 0)
        {
            cached = 0;
        }
        else
        {
            cached = compoundOut / compoundIn;
        }

        cachedCompoundScores.Add(key, cached);
        return cached;
    }

    public float GetCompoundGeneratedFrom(Compound fromCompound, Compound toCompound, MicrobeSpecies species,
        BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions, fromCompound, toCompound);

        if (cachedGeneratedCompound.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = 0.0f;

        foreach (var organelle in species.Organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(fromCompound))
                {
                    if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                    {
                        var processSpeed = GetProcessMaximumSpeed(process, biomeConditions).CurrentSpeed;

                        cached += outputAmount * processSpeed;
                    }
                }
            }
        }

        cachedGeneratedCompound.Add(key, cached);
        return cached;
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

        foreach (var input in process.Process.Inputs)
        {
            if (biomeConditions.Compounds.TryGetValue(input.Key, out var inputCompoundData))
            {
                if (inputCompoundData.Amount <= 0 && inputCompoundData.Ambient <= 0)
                {
                    cached.CurrentSpeed = 0;
                    break;
                }
            }
        }

        cachedProcessSpeeds.Add(key, cached);
        return cached;
    }

    public float GetPredationScore(MicrobeSpecies microbeSpecies, MicrobeSpecies prey, BiomeConditions biomeConditions)
    {
        // No cannibalism
        if (microbeSpecies == prey)
        {
            return 0.0f;
        }

        var key = (microbeSpecies, prey, biomeConditions);

        if (predationScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var preyHexSize = GetBaseHexSizeForSpecies(prey);
        var preySpeed = GetBaseSpeedForSpecies(prey);

        var behaviourScore = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        // TODO: if these two methods were combined it might result in better performance with needing just
        // one dictionary lookup
        var microbeSpeciesHexSize = GetBaseHexSizeForSpecies(microbeSpecies);
        var predatorSpeed = GetBaseSpeedForSpecies(microbeSpecies);

        // Only assign engulf score if one can actually engulf
        var engulfScore = 0.0f;
        if (microbeSpeciesHexSize / preyHexSize >
            Constants.ENGULF_SIZE_RATIO_REQ && microbeSpecies.CanEngulf)
        {
            // Catch scores grossly accounts for how many preys you catch in a run;
            var catchScore = 0.0f;

            // First, you may hunt individual preys, but only if you are fast enough...
            if (predatorSpeed > preySpeed)
            {
                // You catch more preys if you are fast, and if they are slow.
                // This incentivizes engulfment strategies in these cases.
                catchScore += predatorSpeed / preySpeed;
            }

            // ... but you may also catch them by luck (e.g. when they run into you),
            // and this is especially easy if you're huge.
            // This is also used to incentivize size in microbe species.
            catchScore += Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY * microbeSpeciesHexSize;

            // Allow for some degree of lucky engulfment
            engulfScore = catchScore * Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;
        }

        var (pilusScore, oxytoxyScore, mucilageScore) = GetPredationToolsRawScores(microbeSpecies);

        // don't use mucus for now
        mucilageScore *= 0;

        // Pili are much more useful if the microbe can close to melee
        pilusScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

        // having lots of extra Pili really doesn't help you THAT much
        pilusScore = MathF.Pow(pilusScore, 0.4f);

        // predators are less likely to use toxin against larger prey, unless they are opportunistic
        if (preyHexSize > microbeSpeciesHexSize)
        {
            oxytoxyScore *= microbeSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
        }

        // If you can store enough to kill the prey, producing more isn't as important
        var storageToKillRatio = microbeSpecies.StorageCapacities.Nominal * Constants.OXYTOXY_DAMAGE /
            prey.MembraneType.Hitpoints * prey.MembraneType.ToxinResistance;
        if (storageToKillRatio > 1)
        {
            oxytoxyScore = MathF.Pow(oxytoxyScore, 0.8f);
        }
        else
        {
            oxytoxyScore = MathF.Pow(oxytoxyScore, storageToKillRatio * 0.8f);
        }

        // prey that resist toxin are obviously weaker to it
        oxytoxyScore /= prey.MembraneType.ToxinResistance;

        cached = behaviourScore * (pilusScore + engulfScore + oxytoxyScore + mucilageScore) /
            GetEnergyBalanceForSpecies(microbeSpecies, biomeConditions).TotalConsumption;

        predationScores.Add(key, cached);
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
