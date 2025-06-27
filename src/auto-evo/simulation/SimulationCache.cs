namespace AutoEvo;

using System;
using System.Collections.Generic;
using Godot;
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
///     TODO: would be better to reuse instances of this class after clearing them for next use (there's now a Clear
///     method for this future usecase)
///   </para>
/// </remarks>
public class SimulationCache
{
    private readonly CompoundDefinition oxytoxy = SimulationParameters.GetCompound(Compound.Oxytoxy);
    private readonly CompoundDefinition mucilage = SimulationParameters.GetCompound(Compound.Mucilage);

    private readonly WorldGenerationSettings worldSettings;

    private readonly Dictionary<(Species, SelectionPressure, Patch), float> cachedPressureScores = new();

    private readonly Dictionary<(MicrobeSpecies, IBiomeConditions), EnergyBalanceInfoSimple>
        cachedSimpleEnergyBalances = [];

    private readonly Dictionary<MicrobeSpecies, float> cachedBaseSpeeds = new();
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseHexSizes = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, CompoundDefinition, CompoundDefinition), float>
        cachedCompoundScores = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions, CompoundDefinition, CompoundDefinition), float>
        cachedGeneratedCompound = new();

    private readonly Dictionary<(MicrobeSpecies, MicrobeSpecies, IBiomeConditions), float> predationScores = new();

    private readonly Dictionary<(TweakedProcess, float, IBiomeConditions), ProcessSpeedInformation>
        cachedProcessSpeeds = new();

    private readonly Dictionary<MicrobeSpecies, (float, float, float, float)>
        cachedPredationToolsRawScores = new();

    private readonly Dictionary<(MicrobeSpecies, string), float> cachedEnzymeScores = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), bool> cachedUsesVaryingCompounds = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), float> cachedStorageScores = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), ResolvedMicrobeTolerances> cachedResolvedTolerances =
        new();

    public SimulationCache(WorldGenerationSettings worldSettings)
    {
        this.worldSettings = worldSettings;
    }

    public float GetPressureScore(SelectionPressure pressure, Patch patch, Species species)
    {
        var key = (species, pressure, patch);

        if (cachedPressureScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = pressure.Score(species, patch, this);

        cachedPressureScores.Add(key, cached);
        return cached;
    }

    public EnergyBalanceInfoSimple GetEnergyBalanceForSpecies(MicrobeSpecies species,
        BiomeConditions biomeConditions)
    {
        // TODO: this gets called an absolute ton with the new auto-evo so a more efficient caching method (to allow
        // different species but with same organelles to be able to use the same cache value) would be nice here
        var key = (species, biomeConditions);

        if (cachedSimpleEnergyBalances.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(species.Organelles);

        // TODO: check if caching instances of these objects would be better than always recreating
        cached = new EnergyBalanceInfoSimple();

        // Auto-evo uses the average values of compound during the course of a simulated day
        ProcessSystem.ComputeEnergyBalanceSimple(species.Organelles, biomeConditions,
            GetEnvironmentalTolerances(species, biomeConditions), species.MembraneType,
            maximumMovementDirection, true, species.PlayerSpecies, worldSettings, CompoundAmountType.Average, this,
            cached);

        cachedSimpleEnergyBalances.Add(key, cached);
        return cached;
    }

    // TODO: Both of these seem like something that could easily be stored on the species with OnEdited
    public float GetSpeedForSpecies(MicrobeSpecies species)
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

    public float GetCompoundConversionScoreForSpecies(CompoundDefinition fromCompound, CompoundDefinition toCompound,
        MicrobeSpecies species, BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions, fromCompound, toCompound);

        if (cachedCompoundScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var compoundIn = 0.0f;
        var compoundOut = 0.0f;

        // For maximum efficiency as this is called an absolute ton the following approach is used
        var organelles = species.Organelles.Organelles;
        var count = organelles.Count;

        for (var i = 0; i < count; ++i)
        {
            foreach (var process in organelles[i].Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.TryGetValue(fromCompound, out var inputAmount))
                {
                    if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                    {
                        // We don't multiply by speed here as it is about pure efficiency
                        compoundIn += inputAmount;
                        compoundOut += outputAmount;
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

    public float GetCompoundGeneratedFrom(CompoundDefinition fromCompound, CompoundDefinition toCompound,
        MicrobeSpecies species, BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions, fromCompound, toCompound);

        if (cachedGeneratedCompound.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = 0.0f;

        var organelles = species.Organelles.Organelles;
        var organelleCount = organelles.Count;

        var tolerances = GetEnvironmentalTolerances(species, biomeConditions);

        for (int i = 0; i < organelleCount; ++i)
        {
            foreach (var process in organelles[i].Definition.RunnableProcesses)
            {
                if (process.Process.Inputs.ContainsKey(fromCompound))
                {
                    if (process.Process.Outputs.TryGetValue(toCompound, out var outputAmount))
                    {
                        var processSpeed =
                            GetProcessMaximumSpeed(process, tolerances.ProcessSpeedModifier, biomeConditions)
                                .CurrentSpeed;

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
    /// <param name="speedModifier">
    ///   Process speed modifier from <see cref="ResolvedMicrobeTolerances.ProcessSpeedModifier"/>
    /// </param>
    /// <param name="biomeConditions">The biome conditions to use</param>
    /// <returns>The speed information for the process</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: check if this method's caching ability has been compromised with adding speedModifier
    ///   </para>
    /// </remarks>
    public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, float speedModifier,
        IBiomeConditions biomeConditions)
    {
        var key = (process, speedModifier, biomeConditions);

        if (cachedProcessSpeeds.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = ProcessSystem.CalculateProcessMaximumSpeed(process, speedModifier, biomeConditions,
            CompoundAmountType.Average, true);

        cachedProcessSpeeds.Add(key, cached);
        return cached;
    }

    public float GetPredationScore(Species predatorSpecies, Species preySpecies, BiomeConditions biomeConditions)
    {
        if (predatorSpecies is not MicrobeSpecies predator)
            return 0;

        if (preySpecies is not MicrobeSpecies prey)
            return 0;

        // No cannibalism
        if (predator == prey)
        {
            return 0.0f;
        }

        var key = (microbeSpecies: predator, prey, biomeConditions);

        if (predationScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        // TODO: If these two methods were combined it might result in better performance with needing just
        // one dictionary lookup
        var predatorHexSize = GetBaseHexSizeForSpecies(predator);
        var predatorSpeed = GetSpeedForSpecies(predator);
        var preyHexSize = GetBaseHexSizeForSpecies(prey);
        var preySpeed = GetSpeedForSpecies(prey);
        var enzymesScore = GetEnzymesScore(predator, prey.MembraneType.DissolverEnzyme);
        var (pilusScore, oxytoxyScore, predatorSlimeJetScore, _) =
            GetPredationToolsRawScores(predator);
        var (_, _, preySlimeJetScore, preyMucocystsScore) = GetPredationToolsRawScores(prey);

        var behaviourScore = predator.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        // Only assign engulf score if one can actually engulf (and digest)
        var engulfmentScore = 0.0f;
        if (predatorHexSize / preyHexSize >
            Constants.ENGULF_SIZE_RATIO_REQ && predator.CanEngulf && enzymesScore > 0.0f)
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
            catchScore += Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY * predatorHexSize;

            // Allow for some degree of lucky engulfment
            engulfmentScore = catchScore * Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;

            engulfmentScore *= enzymesScore;
        }

        // If the predator is faster than the prey they don't need slime jets that much
        if (predatorSpeed > preySpeed)
            predatorSlimeJetScore *= 0.5f;

        // Pili are much more useful if the microbe can close to melee
        pilusScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

        // Predators are less likely to use toxin against larger prey, unless they are opportunistic
        if (preyHexSize > predatorHexSize)
        {
            oxytoxyScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
        }

        // If you can store enough to kill the prey, producing more isn't as important
        var storageToKillRatio = predator.StorageCapacities.Nominal * Constants.OXYTOXY_DAMAGE /
            prey.MembraneType.Hitpoints * prey.MembraneType.ToxinResistance;
        if (storageToKillRatio > 1)
        {
            oxytoxyScore = MathF.Pow(oxytoxyScore, 0.8f);
        }
        else
        {
            oxytoxyScore = MathF.Pow(oxytoxyScore, storageToKillRatio * 0.8f);
        }

        // Prey that resist toxin are obviously weaker to it
        oxytoxyScore /= prey.MembraneType.ToxinResistance;

        var scoreMultiplier = 1.0f;

        if (!predator.CanEngulf)
        {
            // If you can't engulf, you just get energy from the chunks leaking.
            scoreMultiplier *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        cached = (scoreMultiplier * behaviourScore *
                (pilusScore + engulfmentScore + oxytoxyScore + predatorSlimeJetScore) -
                (preySlimeJetScore + preyMucocystsScore)) /
            GetEnergyBalanceForSpecies(predator, biomeConditions).TotalConsumption;

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

    /// <summary>
    ///   Clears all data in this cache. Can be used to re-use a cache object *but should not be called* while anything
    ///   might still be using this cache currently!
    /// </summary>
    public void Clear()
    {
        cachedPressureScores.Clear();
        cachedSimpleEnergyBalances.Clear();
        cachedBaseSpeeds.Clear();
        cachedBaseHexSizes.Clear();
        cachedCompoundScores.Clear();
        cachedGeneratedCompound.Clear();
        predationScores.Clear();
        cachedProcessSpeeds.Clear();
        cachedPredationToolsRawScores.Clear();
        cachedEnzymeScores.Clear();
        cachedUsesVaryingCompounds.Clear();
        cachedStorageScores.Clear();
        cachedResolvedTolerances.Clear();
    }

    public (float PilusScore, float OxytoxyScore, float SlimeJetScore, float MucocystsScore)
        GetPredationToolsRawScores(MicrobeSpecies microbeSpecies)
    {
        if (cachedPredationToolsRawScores.TryGetValue(microbeSpecies, out var cached))
            return cached;

        var oxytoxyScore = 0.0f;
        var pilusScore = Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
        var slimeJetScore = Constants.AUTO_EVO_SLIME_JET_SCORE;
        var mucocystsScore = Constants.AUTO_EVO_MUCOCYST_SCORE;

        var organelles = microbeSpecies.Organelles.Organelles;
        var organelleCount = organelles.Count;
        var pilusCount = 0;
        var slimeJetsCount = 0;
        var mucocystsCount = 0;
        var slimeJetsMultiplier = 1.0f;

        for (int i = 0; i < organelleCount; ++i)
        {
            var organelle = organelles[i];

            if (organelle.Definition.HasPilusComponent)
            {
                ++pilusCount;
                continue;
            }

            if (organelle.Definition.HasSlimeJetComponent)
            {
                if (organelle.Upgrades?.UnlockedFeatures.Contains(SlimeJetComponent.MUCOCYST_UPGRADE_NAME) == true)
                {
                    ++mucocystsCount;
                    continue;
                }

                ++slimeJetsCount;

                // Make sure that slime jets are positioned at the back of the cell, because otherwise they will
                // push the cell backwards (into the predator or away from the prey) or to the side
                slimeJetsMultiplier *= CalculateAngleMultiplier(organelle.Position);
                continue;
            }

            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                if (process.Process.Outputs.TryGetValue(oxytoxy, out var oxytoxyAmount))
                {
                    oxytoxyScore += oxytoxyAmount * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }
            }
        }

        // Having lots of extra pili, slime jets and mucocysts doesn't really help much
        pilusScore *= MathF.Sqrt(pilusCount);
        slimeJetScore *= MathF.Sqrt(slimeJetsCount);
        mucocystsScore *= MathF.Sqrt(mucocystsCount);

        slimeJetScore *= slimeJetsMultiplier;

        var predationToolsRawScores = (pilusScore, oxytoxyScore, slimeJetScore, mucocystsScore);

        cachedPredationToolsRawScores.Add(microbeSpecies, predationToolsRawScores);
        return predationToolsRawScores;
    }

    public float GetEnzymesScore(MicrobeSpecies predator, string dissolverEnzyme)
    {
        var key = (predator, dissolverEnzyme);
        if (cachedEnzymeScores.TryGetValue(key, out var cached))
            return cached;

        var organelles = predator.Organelles.Organelles;
        var isMembraneDigestible = dissolverEnzyme == Constants.LIPASE_ENZYME;
        var enzymesScore = 0.0f;

        if (isMembraneDigestible)
        {
            // Add the base digestion score that works even without any organelles added
            enzymesScore += Constants.AUTO_EVO_BASE_DIGESTION_SCORE;
        }

        var count = organelles.Count;
        for (var i = 0; i < count; ++i)
        {
            var organelle = organelles[i].Definition;
            if (!organelle.HasLysosomeComponent)
                continue;

            foreach (var enzyme in organelle.Enzymes)
            {
                if (enzyme.Key.InternalName != dissolverEnzyme)
                    continue;

                // No need to check the amount here as organelle data validates enzyme amounts are above 0

                isMembraneDigestible = true;

                // This doesn't use safety as it will be otherwise masking very subtle bugs with some enzyme not
                // working in auto-evo
                enzymesScore += Constants.AutoEvoLysosomeEnzymesScores[enzyme.Key.InternalName];
            }
        }

        // If not digestible, mark that as a 0 score
        if (!isMembraneDigestible)
            enzymesScore = 0;

        cachedEnzymeScores.Add(key, enzymesScore);
        return enzymesScore;
    }

    public ResolvedMicrobeTolerances GetEnvironmentalTolerances(MicrobeSpecies species,
        BiomeConditions biomeConditions)
    {
        var key = (species, biomeConditions);
        if (cachedResolvedTolerances.TryGetValue(key, out var cached))
            return cached;

        var tolerances = MicrobeEnvironmentalToleranceCalculations.CalculateTolerances(species, biomeConditions);

        var result = MicrobeEnvironmentalToleranceCalculations.ResolveToleranceValues(tolerances);

        cachedResolvedTolerances.Add(key, result);
        return result;
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
            species.Organelles, species.MembraneType, moving, species.PlayerSpecies, biomeConditions,
            GetEnvironmentalTolerances(species, biomeConditions), worldSettings,
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

    /// <summary>
    ///   Calculates cos of angle between the organelle and vertical axis
    /// </summary>
    private float CalculateAngleMultiplier(Hex pos)
    {
        // Slime jets are biased to go backwards at position (0,0)
        if (pos.R == 0 && pos.Q == 0)
            return 1;

        Vector3 organellePosition = Hex.AxialToCartesian(pos);
        Vector3 downVector = new Vector3(0, 0, 1);
        float angleCos = organellePosition.Normalized().Dot(downVector);

        // If degrees is higher than 40 then return 0
        return angleCos >= 0.75 ? angleCos : 0;
    }
}
