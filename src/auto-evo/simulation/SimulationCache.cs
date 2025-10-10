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

    private readonly Dictionary<(MicrobeSpecies, CompoundDefinition, IBiomeConditions), float>
        chemoreceptorCloudScores = new();

    private readonly Dictionary<(MicrobeSpecies, ChunkConfiguration, CompoundDefinition, IBiomeConditions), float>
        chemoreceptorChunkScores = new();

    private readonly Dictionary<(TweakedProcess, float, IBiomeConditions), ProcessSpeedInformation>
        cachedProcessSpeeds = new();

    private readonly Dictionary<MicrobeSpecies, (float, float, float, float, float, float, float, float, float)>
        cachedPredationToolsRawScores = new();

    private readonly Dictionary<(MicrobeSpecies, string), float> cachedEnzymeScores = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), bool> cachedUsesVaryingCompounds = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), float> cachedStorageScores = new();

    private readonly Dictionary<(MicrobeSpecies, BiomeConditions), ResolvedMicrobeTolerances> cachedResolvedTolerances =
        new();

    private readonly Dictionary<Enzyme, int> tempEnzymes = new();

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
        var slowedPreySpeed = preySpeed;
        var preyIndividualCost = MichePopulation.CalculateMicrobeIndividualCost(prey, biomeConditions, this);
        var preyEnergyBalance = GetEnergyBalanceForSpecies(prey, biomeConditions);
        var preyOsmoregulationCost = preyEnergyBalance.Osmoregulation;
        var enzymesScore = GetEnzymesScore(predator, prey.MembraneType.DissolverEnzyme);
        var (pilusScore, toxicity, oxytoxyScore, cytotoxinScore, macrolideScore,
                channelInhibitorScore, oxygenMetabolismInhibitorScore, predatorSlimeJetScore, _) =
            GetPredationToolsRawScores(predator);
        var (_, _, _, _, _, _, _, preySlimeJetScore, preyMucocystsScore) = GetPredationToolsRawScores(prey);

        var behaviourScore = predator.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

        var hasChemoreceptor = false;
        foreach (var organelle in predator.Organelles.Organelles)
        {
            if (organelle.Definition.HasChemoreceptorComponent && organelle.GetActiveTargetSpecies() == prey)
                hasChemoreceptor = true;
        }

        var preyOxygenUsingOrganellesCount = 0;
        foreach (var organelle in prey.Organelles.Organelles)
        {
            if (organelle.Definition.IsOxygenMetabolism)
                preyOxygenUsingOrganellesCount += 1;
        }

        // Calculating "hit chance" modifier from toxicity
        var toxicityHitFactor = 1 - toxicity / Constants.AUTO_EVO_TOXICITY_HIT_MODIFIER;

        // Calculating prey energy production altered by channel inhbitor
        var inhibitedPreyEnergyProduction = preyEnergyBalance.TotalProduction *
            Constants.CHANNEL_INHIBITOR_ATP_DEBUFF *
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.ChannelInhibitor);

        // If inhibited energy production would affect movement, add (part of) the inhibitor score to macrolide score
        if (inhibitedPreyEnergyProduction < preyEnergyBalance.TotalConsumption)
        {
            var channelInhibitorSlowFactor = Math.Min(
                Math.Max(inhibitedPreyEnergyProduction - preyOsmoregulationCost, 0) /
                preyEnergyBalance.TotalMovement, 1);
            macrolideScore += channelInhibitorScore * channelInhibitorSlowFactor;
            slowedPreySpeed *= 1 - channelInhibitorSlowFactor;
        }

        // Calculating how much prey is slowed down by macrolide, and how frequently they are succesfully slowed down
        slowedPreySpeed *= 1 - Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF *
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.Macrolide);
        var slowedProportion = 1.0f - MathF.Exp(-Constants.AUTO_EVO_TOXIN_AFFECTED_PROPORTION_SCALING *
            macrolideScore * toxicityHitFactor);

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
                catchScore += (predatorSpeed / preySpeed) * (1 - slowedProportion);
            }

            if (predatorSpeed > slowedPreySpeed)
            {
                // You catch more preys if you are fast, and if they are slow.
                // This incentivizes engulfment strategies in these cases.
                catchScore += (predatorSpeed / slowedPreySpeed) * slowedProportion;
            }

            // If you have a chemoreceptor, active hunting types are more effective
            if (hasChemoreceptor)
            {
                catchScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;

                // Uses crude estimate of population density assuming same energy capture
                catchScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                    * float.Sqrt(preyIndividualCost);
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
        if (predatorSpeed ! > preySpeed)
        {
            pilusScore *= predatorSpeed > slowedPreySpeed ?
                slowedProportion + Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY :
                Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;
        }

        // Damaging toxin section

        // Not an ideal solution, but accounts for the fact that the oxytoxy and cyanide processes require oxygen to run
        biomeConditions.Compounds.TryGetValue(Compound.Oxygen, out BiomeCompoundProperties oxygen);
        if (oxygen.Ambient == 0)
        {
            oxytoxyScore = 0;
            oxygenMetabolismInhibitorScore = 0;
        }

        oxytoxyScore *= 1 - Math.Min(preyOxygenUsingOrganellesCount * Constants.OXYTOXY_DAMAGE_DEBUFF_PER_ORGANELLE,
            Constants.OXYTOXY_DAMAGE_DEBUFF_MAX);
        oxygenMetabolismInhibitorScore *= 1 + Math.Min(
            preyOxygenUsingOrganellesCount * Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_PER_ORGANELLE,
            Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_MAX);
        var damagingToxinScore = oxytoxyScore + cytotoxinScore + oxygenMetabolismInhibitorScore;

        // If toxin-inhibited energy production is lower than osmoregulation cost, channel inhibitor is a damaging toxin
        if (inhibitedPreyEnergyProduction < preyOsmoregulationCost)
            damagingToxinScore += channelInhibitorScore;

        // Predators are less likely to use toxin against larger prey, unless they are opportunistic
        if (preyHexSize > predatorHexSize)
        {
            damagingToxinScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
        }

        // If you can store enough to kill the prey, producing more isn't as important
        var storageToKillRatio = predator.StorageCapacities.Nominal * Constants.OXYTOXY_DAMAGE /
            prey.MembraneType.Hitpoints * prey.MembraneType.ToxinResistance;
        if (storageToKillRatio > 1)
        {
            damagingToxinScore = MathF.Pow(damagingToxinScore, 0.8f);
        }
        else
        {
            damagingToxinScore = MathF.Pow(damagingToxinScore, storageToKillRatio * 0.8f);
        }

        // Prey that resist toxin are of course vulnerable to being hunted with it
        damagingToxinScore /= prey.MembraneType.ToxinResistance;

        // If you have a chemoreceptor, active hunting types are more effective
        if (hasChemoreceptor)
        {
            pilusScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;
            pilusScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                * float.Sqrt(preyIndividualCost);
            damagingToxinScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;
            damagingToxinScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                * float.Sqrt(preyIndividualCost);
        }

        var scoreMultiplier = 1.0f;

        if (!predator.CanEngulf)
        {
            // If you can't engulf, you just get energy from the chunks leaking.
            scoreMultiplier *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        cached = (scoreMultiplier * behaviourScore *
                (pilusScore + engulfmentScore + damagingToxinScore + predatorSlimeJetScore) -
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

    public float GetChemoreceptorCloudScore(MicrobeSpecies species, CompoundDefinition compound,
        BiomeConditions biomeConditions)
    {
        var key = (species, compound, biomeConditions);

        if (chemoreceptorCloudScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = 0.0f;
        var hasChemoreceptor = false;
        foreach (var organelle in species.Organelles.Organelles)
        {
            var organelleTargetCompound = organelle.GetActiveTargetCompound();
            if (organelleTargetCompound == Compound.Invalid)
                continue;

            if (organelleTargetCompound == compound.ID)
                hasChemoreceptor = true;
        }

        if (hasChemoreceptor)
        {
            if (biomeConditions.AverageCompounds.TryGetValue(compound.ID, out var compoundData))
            {
                cached = Constants.AUTO_EVO_CHEMORECEPTOR_BASE_SCORE
                    + Constants.AUTO_EVO_CHEMORECEPTOR_VARIABLE_CLOUD_SCORE
                    / (compoundData.Density * compoundData.Amount);
            }
        }

        chemoreceptorCloudScores.Add(key, cached);
        return cached;
    }

    public float GetChemoreceptorChunkScore(MicrobeSpecies species, ChunkConfiguration chunk,
        CompoundDefinition compound, BiomeConditions biomeConditions)
    {
        var key = (species, chunk, compound, biomeConditions);

        if (chemoreceptorChunkScores.TryGetValue(key, out var cached))
        {
            return cached;
        }

        cached = 0.0f;
        var hasChemoreceptor = false;
        foreach (var organelle in species.Organelles.Organelles)
        {
            var organelleTargetCompound = organelle.GetActiveTargetCompound();
            if (organelleTargetCompound == Compound.Invalid)
                continue;

            if (organelleTargetCompound == compound.ID)
                hasChemoreceptor = true;
        }

        if (hasChemoreceptor)
        {
            // We use null suppression here
            // as this method is only meant to be called on chunks that are known to contain the given compound
            if (!chunk.Compounds!.TryGetValue(compound.ID, out var compoundAmount))
                throw new ArgumentException("Chunk does not contain compound");

            cached = Constants.AUTO_EVO_CHEMORECEPTOR_BASE_SCORE
                + Constants.AUTO_EVO_CHEMORECEPTOR_VARIABLE_CHUNK_SCORE
                / (chunk.Density * MathF.Pow(compoundAmount.Amount, Constants.AUTO_EVO_CHUNK_AMOUNT_NERF));
        }

        chemoreceptorChunkScores.Add(key, cached);
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
        chemoreceptorCloudScores.Clear();
        chemoreceptorChunkScores.Clear();
        cachedProcessSpeeds.Clear();
        cachedPredationToolsRawScores.Clear();
        cachedEnzymeScores.Clear();
        cachedUsesVaryingCompounds.Clear();
        cachedStorageScores.Clear();
        cachedResolvedTolerances.Clear();
    }

    public (float PilusScore, float AverageToxicity, float OxytoxyScore, float CytotoxinScore,
        float MacrolideScore, float ChannelInhibitorScore, float OxygenMetabolismInhibitorScore, float SlimeJetScore,
        float MucocystsScore)
        GetPredationToolsRawScores(MicrobeSpecies microbeSpecies)
    {
        if (cachedPredationToolsRawScores.TryGetValue(microbeSpecies, out var cached))
            return cached;

        var averageToxicity = 0.0f;
        var totalToxicity = 0.0f;
        var totalToxinScore = 0.0f;
        var everyToxinScore = 0.0f;
        var oxytoxyScore = 0.0f;
        var cytotoxinScore = 0.0f;
        var macrolideScore = 0.0f;
        var channelInhibitorScore = 0.0f;
        var oxygenMetabolismInhibitorScore = 0.0f;
        var pilusScore = Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
        var slimeJetScore = Constants.AUTO_EVO_SLIME_JET_SCORE;
        var mucocystsScore = Constants.AUTO_EVO_MUCOCYST_SCORE;

        var organelles = microbeSpecies.Organelles.Organelles;
        var organelleCount = organelles.Count;
        var totalToxinOrganellesCount = 0;
        var totalToxinTypesCount = 0;
        var pilusCount = 0;
        var slimeJetsCount = 0;
        var mucocystsCount = 0;
        var slimeJetsMultiplier = 1.0f;

        var hasOxytoxy = false;
        var hasCytoxin = false;
        var hasMacrolide = false;
        var hasChannelInhibitor = false;
        var hasOxygenMetabolismInhibitor = false;

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
                // Big branch to calculate scores for each toxin type
                if (process.Process.Outputs.TryGetValue(oxytoxy, out var toxinAmount))
                {
                    if (organelle.GetActiveToxin() == ToxinType.Oxytoxy && !hasOxytoxy)
                    {
                        totalToxinTypesCount += 1;
                        hasOxytoxy = true;
                    }

                    if (organelle.GetActiveToxin() == ToxinType.Cytotoxin && !hasCytoxin)
                    {
                        totalToxinTypesCount += 1;
                        hasCytoxin = true;
                    }

                    if (organelle.GetActiveToxin() == ToxinType.Macrolide && !hasMacrolide)
                    {
                        totalToxinTypesCount += 1;
                        hasMacrolide = true;
                    }

                    if (organelle.GetActiveToxin() == ToxinType.ChannelInhibitor && !hasChannelInhibitor)
                    {
                        totalToxinTypesCount += 1;
                        hasChannelInhibitor = true;
                    }

                    if (organelle.GetActiveToxin() == ToxinType.OxygenMetabolismInhibitor &&
                        !hasOxygenMetabolismInhibitor)
                    {
                        totalToxinTypesCount += 1;
                        hasOxygenMetabolismInhibitor = true;
                    }

                    totalToxicity += organelle.GetActiveToxicity();
                    totalToxinOrganellesCount += 1;
                    totalToxinScore += toxinAmount * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                }
            }
        }

        // Matching current gameplay mechanics of the toxin organelles:

        // Averaging out toxicity, as gameplay also does
        if (totalToxinOrganellesCount != 0)
            averageToxicity = totalToxicity / totalToxinOrganellesCount;

        // Pooled production of toxin compound, equally distributed among all available toxin types (firing in sequence)
        if (totalToxinTypesCount != 0)
        {
            everyToxinScore = totalToxinScore / totalToxinTypesCount;
        }

        if (hasOxytoxy)
        {
            oxytoxyScore = everyToxinScore * (Constants.OXYTOXY_DAMAGE / Constants.CYTOTOXIN_DAMAGE) *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity, ToxinType.Oxytoxy);
        }

        if (hasCytoxin)
        {
            cytotoxinScore = everyToxinScore *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity, ToxinType.Cytotoxin);
        }

        if (hasMacrolide)
            macrolideScore = everyToxinScore;
        if (hasChannelInhibitor)
            channelInhibitorScore = everyToxinScore;
        if (hasOxygenMetabolismInhibitor)
        {
            oxygenMetabolismInhibitorScore = everyToxinScore *
                (Constants.OXYGEN_INHIBITOR_DAMAGE / Constants.CYTOTOXIN_DAMAGE) *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(
                    averageToxicity, ToxinType.OxygenMetabolismInhibitor);
        }

        // Having lots of extra pili, slime jets and mucocysts doesn't really help much
        pilusScore *= MathF.Sqrt(pilusCount);
        slimeJetScore *= MathF.Sqrt(slimeJetsCount);
        mucocystsScore *= MathF.Sqrt(mucocystsCount);

        slimeJetScore *= slimeJetsMultiplier;

        var predationToolsRawScores = (pilusScore, averageToxicity, oxytoxyScore,
            cytotoxinScore, macrolideScore, channelInhibitorScore, oxygenMetabolismInhibitorScore, slimeJetScore,
            mucocystsScore);

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
            var placedOrganelle = organelles[i];

            if (placedOrganelle.GetActiveEnzymes(tempEnzymes))
            {
                foreach (var enzyme in tempEnzymes)
                {
                    if (enzyme.Key.InternalName != dissolverEnzyme)
                        continue;

                    // No need to check the amount here as organelle data validates enzyme amounts are above 0

                    isMembraneDigestible = true;

                    // This doesn't use safety as it will be otherwise masking very subtle bugs with some enzyme not
                    // working in auto-evo
                    enzymesScore += Constants.AutoEvoLysosomeEnzymesScores[enzyme.Key.InternalName];
                }

                tempEnzymes.Clear();
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
