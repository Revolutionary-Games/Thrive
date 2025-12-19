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
    private readonly Dictionary<MicrobeSpecies, float> cachedBaseRotationSpeeds = new();

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

    private readonly Dictionary<MicrobeSpecies, PredationToolsRawScores>
        cachedPredationToolsRawScores = new();

    private readonly Dictionary<MicrobeSpecies, List<TweakedProcess>> cachedProcessLists = new();

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

    public float GetRotationSpeedForSpecies(MicrobeSpecies species)
    {
        if (cachedBaseRotationSpeeds.TryGetValue(species, out var cached))
        {
            return cached;
        }

        cached = MicrobeInternalCalculations.CalculateRotationSpeed(species.Organelles.Organelles);

        cachedBaseRotationSpeeds.Add(species, cached);
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
        var activeProcessList = GetActiveProcessList(species);

        // For maximum efficiency as this is called an absolute ton the following approach is used
        foreach (var process in activeProcessList)
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

        var activeProcessList = GetActiveProcessList(species);

        var tolerances = GetEnvironmentalTolerances(species, biomeConditions);

        foreach (var process in activeProcessList)
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

        // First values necessary to check whether predation is possible at all
        var predatorToolScores = GetPredationToolsRawScores(predator);

        var pilusScore = predatorToolScores.PilusScore;
        var injectisomeScore = predatorToolScores.InjectisomeScore;
        var oxytoxyScore = predatorToolScores.OxytoxyScore;
        var cytotoxinScore = predatorToolScores.CytotoxinScore;
        var channelInhibitorScore = predatorToolScores.ChannelInhibitorScore;
        var canEngulf = predator.CanEngulf;

        // Don't bother with the rest if predator cannot predate
        var engulfOnly = false;

        if (pilusScore == 0 &&
            injectisomeScore == 0 &&
            oxytoxyScore == 0 &&
            cytotoxinScore == 0 &&
            channelInhibitorScore == 0)
        {
            if (canEngulf)
            {
                engulfOnly = true;
            }
            else
            {
                cached = 0;
                predationScores.Add(key, cached);
                return cached;
            }
        }

        var predatorHexSize = GetBaseHexSizeForSpecies(predator);
        var preyHexSize = GetBaseHexSizeForSpecies(prey);
        var enzymesScore = GetEnzymesScore(predator, prey.MembraneType.DissolverEnzyme);
        var canDigestPrey = predatorHexSize / preyHexSize > Constants.ENGULF_SIZE_RATIO_REQ && canEngulf &&
            enzymesScore > 0.0f;

        if (engulfOnly && !canDigestPrey)
        {
            cached = 0;
            predationScores.Add(key, cached);
            return cached;
        }

        // Constants
        var sprintMultiplier = Constants.SPRINTING_FORCE_MULTIPLIER;
        var sprintingStrain = Constants.SPRINTING_STRAIN_INCREASE_PER_SECOND / 5;
        var strainPerHex = Constants.SPRINTING_STRAIN_INCREASE_PER_HEX / 5;

        var sizeAffectedProjectileMissFactor = Constants.AUTO_EVO_SIZE_AFFECTED_PROJECTILE_MISS_FACTOR;
        var toxicityHitModifier = Constants.AUTO_EVO_TOXICITY_HIT_MODIFIER;
        var oxytoxyDebuffPerOrganelle = Constants.OXYTOXY_DAMAGE_DEBUFF_PER_ORGANELLE;
        var oxytoxyDebuffMax = Constants.OXYTOXY_DAMAGE_DEBUFF_MAX;
        var oxygenInhibitorBuffPerOrganelle = Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_PER_ORGANELLE;
        var oxygenInhibitorBuffMax = Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_MAX;
        var oxytoxyDamage = Constants.OXYTOXY_DAMAGE;
        var channelInhibitorATPDebuff = Constants.CHANNEL_INHIBITOR_ATP_DEBUFF;

        var signallingBonus = Constants.AUTO_EVO_SIGNALLING_BONUS;

        // We want prey defensive measures to only reduce predation score, not eliminate it.
        // (Predation Score is reduced to 0 anyway if the "prey" has higher predation score to the predator)
        var defenseScoreModifier = Constants.AUTO_EVO_PREDATION_DEFENSE_SCORE_MODIFIER;

        // TODO: If these two methods were combined it might result in better performance with needing just
        // one dictionary lookup
        var predatorSpeed = GetSpeedForSpecies(predator);
        var predatorRotationSpeed = GetRotationSpeedForSpecies(predator);
        var predatorEnergyBalance = GetEnergyBalanceForSpecies(predator, biomeConditions);
        var predatorOsmoregulationCost = predatorEnergyBalance.Osmoregulation;

        var preySpeed = GetSpeedForSpecies(prey);
        var preyRotationSpeed = GetRotationSpeedForSpecies(prey);
        var slowedPreySpeed = preySpeed;
        var preyIndividualCost = MichePopulation.CalculateMicrobeIndividualCost(prey, biomeConditions, this);
        var preyEnergyBalance = GetEnergyBalanceForSpecies(prey, biomeConditions);
        var preyOsmoregulationCost = preyEnergyBalance.Osmoregulation;

        // uses an HP estimate without taking into account environmental tolerance effect
        var predatorHP = predator.MembraneType.Hitpoints + predator.MembraneRigidity *
            Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
        var preyHP = prey.MembraneType.Hitpoints + prey.MembraneRigidity *
            Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;

        var preyToolScores = GetPredationToolsRawScores(prey);

        var toxicity = predatorToolScores.AverageToxicity;
        var macrolideScore = predatorToolScores.MacrolideScore;
        var oxygenMetabolismInhibitorScore = predatorToolScores.OxygenMetabolismInhibitorScore;
        var predatorSlimeJetScore = predatorToolScores.SlimeJetScore;
        var pullingCiliaModifier = predatorToolScores.PullingCiliaModifier;
        var strongPullingCiliaModifier = pullingCiliaModifier * pullingCiliaModifier;
        var predatorToxinResistance = predator.MembraneType.ToxinResistance;
        var predatorPhysicalResistance = predator.MembraneType.PhysicalResistance;

        var preySlimeJetScore = preyToolScores.SlimeJetScore;
        var preyMucocystsScore = preyToolScores.MucocystsScore;
        var preyPilusScore = preyToolScores.PilusScore;
        var preyInjectisomeScore = preyToolScores.InjectisomeScore;
        var preyToxicity = preyToolScores.AverageToxicity;
        var preyOxytoxyScore = preyToolScores.OxytoxyScore;
        var preyCytotoxinScore = preyToolScores.CytotoxinScore;
        var preyMacrolideScore = preyToolScores.MacrolideScore;
        var preyChannelInhibitorScore = preyToolScores.ChannelInhibitorScore;
        var preyOxygenMetabolismInhibitorScore = preyToolScores.OxygenMetabolismInhibitorScore;
        var defensivePilusScore = preyToolScores.DefensivePilusScore;
        var defensiveInjectisomeScore = preyToolScores.DefensiveInjectisomeScore;
        var preyToxinResistance = prey.MembraneType.ToxinResistance;

        // Not an ideal solution, but accounts for the fact that the oxytoxy and cyanide processes require oxygen to run
        biomeConditions.Compounds.TryGetValue(Compound.Oxygen, out BiomeCompoundProperties oxygen);
        if (oxygen.Ambient == 0)
        {
            oxytoxyScore = 0;
            preyOxytoxyScore = 0;
            oxygenMetabolismInhibitorScore = 0;
            preyOxygenMetabolismInhibitorScore = 0;
        }

        var aggressionScore = predator.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;
        var activityScore = predator.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY;

        var preyFearScore = prey.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;
        var preyAggressionScore = prey.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;
        var preyOpportunismScore = prey.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;

        // prey effectiveness at running away depends on how quickly they choose to run away
        preySpeed *= preyFearScore;

        // Sprinting calculations
        var predatorSprintSpeed = predatorSpeed * sprintMultiplier;
        var predatorSprintConsumption = sprintingStrain + predatorHexSize * strainPerHex;
        var predatorSprintTime = MathF.Max(predatorEnergyBalance.FinalBalance / predatorSprintConsumption, 0.0f);

        var preySprintSpeed = preySpeed * sprintMultiplier;
        var preySprintConsumption = sprintingStrain + preyHexSize * strainPerHex;
        var preySprintTime = MathF.Max(preyEnergyBalance.FinalBalance / preySprintConsumption, 0.0f);

        // Give damage resistance if you have a nucleus (50 % general damage resistance)
        if (!predator.IsBacteria)
            predatorHP *= 2;
        if (!prey.IsBacteria)
            preyHP *= 2;

        // This makes rotation "speed" not matter until the editor shows ~300,
        // which is where it also becomes noticeable in-game.
        // The mechanical microbe rotation speed value is reverse to intuitive: higher value means slower turning.
        // (The editor reverses this to make it intuitive to the player)
        var predatorRotationModifier = float.Min(1.0f, 1.5f - predatorRotationSpeed * 1.45f);
        var preyRotationModifier = float.Min(1.0f, 1.5f - preyRotationSpeed * 1.45f);

        // Simple estimation of slime jet propulsion.
        var predatorSlimeSpeed = predatorSpeed + predatorSlimeJetScore / (predatorHexSize * 11);
        var preySlimeSpeed = preySpeed + preySlimeJetScore / (preyHexSize * 11);

        var hasChemoreceptor = false;
        var hasSignallingAgent = false;
        var preyHasSignallingAgent = false;
        var predatorOxygenUsingOrganellesCount = 0;
        var preyOxygenUsingOrganellesCount = 0;

        var organelles = predator.Organelles.Organelles;
        int count = organelles.Count;
        for (int i = 0; i < count; ++i)
        {
            var organelle = organelles[i];
            if (organelle.Definition.HasChemoreceptorComponent && organelle.GetActiveTargetSpecies() == prey)
                hasChemoreceptor = true;
            if (organelle.Definition.HasSignalingFeature)
                hasSignallingAgent = true;
            if (organelles[i].Definition.IsOxygenMetabolism)
                ++predatorOxygenUsingOrganellesCount;
        }

        var preyOrganelles = prey.Organelles.Organelles;
        int preyOrganellesCount = preyOrganelles.Count;
        for (int i = 0; i < preyOrganellesCount; ++i)
        {
            var organelle = preyOrganelles[i];
            if (organelle.Definition.HasSignalingFeature)
                preyHasSignallingAgent = true;
            if (preyOrganelles[i].Definition.IsOxygenMetabolism)
                ++preyOxygenUsingOrganellesCount;
        }

        // Calculating "hit chance" modifier from prey size and predator toxicity
        var sizeHitFactor = sizeAffectedProjectileMissFactor / float.Sqrt(preyHexSize);
        var toxicityHitFactor = toxicity / toxicityHitModifier;
        var hitProportion = 1 - sizeHitFactor - toxicityHitFactor;

        // Calculating prey energy production altered by channel inhbitor
        var preyInhibitedPreyEnergyProduction = preyEnergyBalance.TotalProduction;
        if (channelInhibitorScore > 0)
        {
            preyInhibitedPreyEnergyProduction *= 1 - channelInhibitorATPDebuff *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.ChannelInhibitor);

            // If inhibited energy production would affect movement,
            // add (part of) the inhibitor score to macrolide score
            if (preyInhibitedPreyEnergyProduction < preyEnergyBalance.TotalConsumption)
            {
                var channelInhibitorSlowFactor = Math.Min(
                    Math.Max(preyInhibitedPreyEnergyProduction - preyOsmoregulationCost, 0) /
                    preyEnergyBalance.TotalMovement, 1);
                macrolideScore += channelInhibitorScore * channelInhibitorSlowFactor;
                slowedPreySpeed *= 1 - channelInhibitorSlowFactor;
            }
        }

        // Calculating predator energy production altered by channel inhbitor
        var predatorInhibitedPreyEnergyProduction = predatorEnergyBalance.TotalProduction;
        if (preyChannelInhibitorScore > 0)
        {
            predatorInhibitedPreyEnergyProduction *= 1 - channelInhibitorATPDebuff *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(preyToxicity, ToxinType.ChannelInhibitor);
        }

        // Calculating how much prey is slowed down by macrolide, and how frequently they are succesfully slowed down
        var slowedProportion = 0.0f;
        if (macrolideScore > 0)
        {
            slowedPreySpeed *= 1 - Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF *
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.Macrolide);
            slowedProportion = 1.0f - MathF.Exp(-Constants.AUTO_EVO_TOXIN_AFFECTED_PROPORTION_SCALING *
                macrolideScore * hitProportion);
        }

        // Catch scores grossly accounts for how many preys you catch in melee in a run;
        var catchScore = 0.0f;
        var accidentalCatchScore = 0.0f;

        // Only calculate catch score if one can actually engulf (and digest) or use pili
        var engulfmentScore = 0.0f;
        if (canDigestPrey || pilusScore > 0.0f || injectisomeScore > 0.0f)
        {
            // First, you may hunt individual preys, but only if you are fast enough...
            if (predatorSpeed > preySpeed)
            {
                // You catch more preys if you are fast, and if they are slow.
                // This incentivizes engulfment strategies in these cases.
                // Sigmoidal calculation to avoid divisions by zero
                catchScore += (predatorSpeed + 0.001f) / (preySpeed + 0.0001f) * (1 - slowedProportion);
            }

            // If you can slow the target, some proportion of prey are easier to catch
            if (predatorSpeed > slowedPreySpeed)
            {
                catchScore += (predatorSpeed + 0.001f) / (slowedPreySpeed + 0.0001f) * slowedProportion;
            }

            // Sprinting can help catch prey.
            if (predatorSprintSpeed > preySpeed)
            {
                catchScore += (predatorSprintSpeed + 0.001f) / (preySpeed + 0.0001f) * (1 - slowedProportion) *
                    predatorSprintTime;
            }

            if (predatorSprintSpeed > slowedPreySpeed)
            {
                catchScore += (predatorSprintSpeed + 0.001f) / (slowedPreySpeed + 0.0001f) * slowedProportion *
                    predatorSprintTime;
            }

            // Sprinting can also help prey escape.
            if (preySprintSpeed > predatorSpeed)
            {
                catchScore -= (preySprintSpeed + 0.001f) / (predatorSpeed + 0.0001f) * preySprintTime;
            }

            // If you have Slime Jets, this can help you catch targets.
            if (predatorSlimeSpeed > preySpeed)
            {
                catchScore += (predatorSlimeSpeed + 0.001f) / (preySpeed + 0.0001f) * (1 - slowedProportion);
            }

            if (predatorSlimeSpeed > slowedPreySpeed)
            {
                catchScore += (predatorSlimeSpeed + 0.001f) / (slowedPreySpeed + 0.0001f) * slowedProportion;
            }

            // Having Slime Jets can also help prey escape.
            if (preySlimeSpeed > predatorSpeed)
            {
                catchScore += (preySlimeSpeed + 0.001f) / (predatorSpeed + 0.0001f);
            }

            // prevent potential negative catchScore.
            catchScore = MathF.Max(catchScore, 0);

            // But prey may escape if they move away before you can turn to chase them
            catchScore *= predatorRotationModifier;

            // Pulling Cilia help with catching
            catchScore *= pullingCiliaModifier;

            // If you have a chemoreceptor, active hunting types are more effective
            if (hasChemoreceptor)
            {
                catchScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;

                // Uses crude estimate of population density assuming same energy capture
                catchScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                    * float.Sqrt(preyIndividualCost);
            }

            // Active hunting is more effective for active species
            catchScore *= activityScore;

            // ... but you may also catch them by luck (e.g. when they run into you),
            // Prey that can't turn away fast enough are more likely to get caught.
            accidentalCatchScore = Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY *
                strongPullingCiliaModifier * preyRotationModifier;
        }

        // targets that resist physical damage are of course less vulnerable to it
        pilusScore /= preyHP * prey.MembraneType.PhysicalResistance;
        preyPilusScore /= predatorHP * predatorPhysicalResistance;
        defensivePilusScore /= predatorHP * predatorPhysicalResistance;

        // But targets that resist toxin damage are less vulnerable to the injectisome
        injectisomeScore /= preyHP * preyToxinResistance;
        preyInjectisomeScore /= predatorHP * predatorToxinResistance;
        defensiveInjectisomeScore /= predatorHP * predatorToxinResistance;

        // Combine pili for further calculations
        pilusScore += injectisomeScore;
        preyPilusScore += preyInjectisomeScore;
        defensivePilusScore += defensiveInjectisomeScore;

        // defensive pili need to be turned directly away from the predator to work
        defensivePilusScore *= preyRotationModifier * preyFearScore;

        // Calling for allies helps with combat.
        if (hasSignallingAgent)
            pilusScore *= signallingBonus;
        if (preyHasSignallingAgent)
            preyPilusScore *= signallingBonus;

        // Use catch score for Pili
        pilusScore -= defensivePilusScore;
        if (pilusScore < 0)
            pilusScore = 0;
        pilusScore *= catchScore + accidentalCatchScore;

        // Prey can use offensive pili for defense in these encounters, but only if they have the right behaviour
        preyPilusScore *= (catchScore + accidentalCatchScore) * preyRotationModifier * defenseScoreModifier *
            preyAggressionScore * (1 - preyFearScore);

        if (canDigestPrey)
        {
            // total prey toxin amount for anti-engulfment purposes
            // Toxin content is higher if the toxin are not being shot for offense
            var totalPreyToxinContent = preyOxytoxyScore + preyCytotoxinScore + preyMacrolideScore +
                preyChannelInhibitorScore + preyOxygenMetabolismInhibitorScore;
            totalPreyToxinContent *= (1 - preyAggressionScore) + preyAggressionScore;
            if (predatorHexSize > preyHexSize)
            {
                totalPreyToxinContent *= 1 - preyOpportunismScore * preyAggressionScore * (1 - preyFearScore);
            }
            else
            {
                totalPreyToxinContent *= 1 - preyAggressionScore * (1 - preyFearScore);
            }

            totalPreyToxinContent *= Constants.AUTO_EVO_TOXIN_ENGULFMENT_DEFENSE_MODIFIER;
            totalPreyToxinContent /= predatorHP;

            // Final engulfment score calculation
            // Engulfing prey by luck is especially easy if you are huge.
            // This is also used to incentivize size in microbe species.
            engulfmentScore = (catchScore + accidentalCatchScore * predatorHexSize) *
                (Constants.AUTO_EVO_ENGULF_PREDATION_SCORE - defensivePilusScore - totalPreyToxinContent);
            if (engulfmentScore < 0)
                engulfmentScore = 0;

            engulfmentScore *= enzymesScore;
        }

        // Damaging toxin section

        oxytoxyScore *= 1 - Math.Min(preyOxygenUsingOrganellesCount * oxytoxyDebuffPerOrganelle, oxytoxyDebuffMax);
        oxygenMetabolismInhibitorScore *= 1 + Math.Min(preyOxygenUsingOrganellesCount * oxygenInhibitorBuffPerOrganelle,
            oxygenInhibitorBuffMax);
        var damagingToxinScore = oxytoxyScore + cytotoxinScore + oxygenMetabolismInhibitorScore;

        preyOxytoxyScore *= 1 - Math.Min(predatorOxygenUsingOrganellesCount * oxytoxyDebuffPerOrganelle,
            oxytoxyDebuffMax);
        preyOxygenMetabolismInhibitorScore *= 1 + Math.Min(
            predatorOxygenUsingOrganellesCount * oxygenInhibitorBuffPerOrganelle, oxygenInhibitorBuffMax);
        var preyDamagingToxinScore = preyOxytoxyScore + preyCytotoxinScore + preyOxygenMetabolismInhibitorScore;

        // If toxin-inhibited energy production is lower than osmoregulation cost, channel inhibitor is a damaging toxin
        if (preyInhibitedPreyEnergyProduction < preyOsmoregulationCost)
            damagingToxinScore += channelInhibitorScore;
        if (predatorInhibitedPreyEnergyProduction < predatorOsmoregulationCost)
            damagingToxinScore += channelInhibitorScore;

        if (damagingToxinScore > 0)
        {
            // Applying projectile hit chance to damaging toxins
            damagingToxinScore *= hitProportion;

            // Predators are less likely to use toxin against larger prey, unless they are opportunistic
            if (preyHexSize > predatorHexSize)
            {
                damagingToxinScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            }

            // If you can store enough to kill the prey, producing more isn't as important
            var storageToKillRatio = predator.StorageCapacities.Nominal * oxytoxyDamage /
                (preyHP * preyToxinResistance);
            if (storageToKillRatio > 1)
            {
                damagingToxinScore = MathF.Pow(damagingToxinScore, 0.8f);
            }
            else
            {
                damagingToxinScore = MathF.Pow(damagingToxinScore, storageToKillRatio * 0.8f);
            }

            // Targets that resist toxin are of course less vulnerable to being damaged with it
            damagingToxinScore /= preyHP * preyToxinResistance;

            // Toxins also require facing and tracking the target
            damagingToxinScore *= predatorRotationModifier;

            // Calling for allies helps with combat.
            if (hasSignallingAgent)
                damagingToxinScore *= signallingBonus;

            // If you have a chemoreceptor, active hunting types are more effective
            if (hasChemoreceptor)
            {
                damagingToxinScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;
                damagingToxinScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                    * float.Sqrt(preyIndividualCost);
            }

            // Active hunting is more effective for active species
            damagingToxinScore *= activityScore;
        }

        if (preyDamagingToxinScore > 0)
        {
            // Calculating "hit chance" modifier from predator size and prey toxicity
            var predatorSizeHitFactor = sizeAffectedProjectileMissFactor / float.Sqrt(predatorHexSize);
            var preyToxicityHitFactor = preyToxicity / toxicityHitModifier;
            var preyHitProportion = 1 - predatorSizeHitFactor - preyToxicityHitFactor;

            // Applying projectile hit chance to damaging toxins
            preyDamagingToxinScore *= preyHitProportion;

            // Prey are less likely to use toxin against larger predators, unless they are opportunistic
            if (predatorHexSize > preyHexSize)
            {
                damagingToxinScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            }

            // If you can store enough to kill the predator, producing more isn't as important
            var preyStorageToKillRatio = prey.StorageCapacities.Nominal * oxytoxyDamage /
                (predatorHP * predatorToxinResistance);
            if (preyStorageToKillRatio > 1)
            {
                preyDamagingToxinScore = MathF.Pow(preyDamagingToxinScore, 0.8f);
            }
            else
            {
                preyDamagingToxinScore = MathF.Pow(preyDamagingToxinScore, preyStorageToKillRatio * 0.8f);
            }

            // Targets that resist toxin are of course less vulnerable to being damaged with it
            preyDamagingToxinScore /= predatorHP * predatorToxinResistance;

            // Toxins also require facing and tracking the target
            preyDamagingToxinScore *= preyRotationModifier;

            // Calling for allies helps with combat.
            if (preyHasSignallingAgent)
                preyDamagingToxinScore *= signallingBonus;

            // Prey can use toxins for defense, but only if they have the right behaviour
            preyDamagingToxinScore *= preyRotationModifier * defenseScoreModifier *
                preyAggressionScore * (1 - preyFearScore);
        }

        var scoreMultiplier = 1.0f;

        if (!canEngulf)
        {
            // If you can't engulf, you just get energy from the chunks leaking.
            scoreMultiplier *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
        }

        // predators that have slime jets themselves ignore the immobilising effect of prey slimejets
        preySlimeJetScore = MathF.Sqrt(preySlimeJetScore);
        if (predatorSlimeJetScore > 0)
            preySlimeJetScore = 0;

        cached = scoreMultiplier * aggressionScore *
            (pilusScore + engulfmentScore + damagingToxinScore) - (preySlimeJetScore + preyMucocystsScore +
                preyPilusScore + preyDamagingToxinScore);
        if (cached < 0)
            cached = 0;

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
            if (biomeConditions.AverageCompounds.TryGetValue(compound.ID, out var compoundData) &&
                compoundData.Density > 0)
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

        // Need to have chemoreceptor to be able to "smell" chunks
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

        // If the chunk doesn't spawn, it doesn't give any of its compound
        if (hasChemoreceptor && chunk.Density > 0)
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
        cachedBaseRotationSpeeds.Clear();
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
        cachedProcessLists.Clear();
    }

    public List<TweakedProcess> GetActiveProcessList(MicrobeSpecies microbeSpecies)
    {
        if (cachedProcessLists.TryGetValue(microbeSpecies, out var cached))
        {
            return cached;
        }

        ProcessSystem.ComputeActiveProcessList(microbeSpecies.Organelles, ref cached);
        cachedProcessLists.Add(microbeSpecies, cached);

        return cached;
    }

    public PredationToolsRawScores GetPredationToolsRawScores(MicrobeSpecies microbeSpecies)
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
        var injectisomeScore = Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
        var defensivePilusScore = Constants.AUTO_EVO_PILUS_DEFENSE_SCORE;
        var defensiveInjectisomeScore = Constants.AUTO_EVO_PILUS_DEFENSE_SCORE;
        var slimeJetScore = Constants.AUTO_EVO_SLIME_JET_SCORE;
        var mucocystsScore = Constants.AUTO_EVO_MUCOCYST_SCORE;
        var pullingCiliaModifier = 1.0f;

        var organelles = microbeSpecies.Organelles.Organelles;
        var organelleCount = organelles.Count;
        var totalToxinOrganellesCount = 0;
        var totalToxinTypesCount = 0;
        var pilusCount = 0.0f;
        var injectisomeCount = 0.0f;
        var defensivePilusCount = 0.0f;
        var defensiveInjectisomeCount = 0.0f;
        var slimeJetsCount = 0;
        var mucocystsCount = 0;
        var pullingCiliasCount = 0;
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
                // Make sure that pili are positioned at the front of the cell for offensive action,
                // and the back of the cell for defensive action
                var piliValue = CalculateAngleMultiplier(organelle.Position, true);
                var defensivePiliValue = CalculateAngleMultiplier(organelle.Position, false);
                if (organelle.Upgrades.HasInjectisomeUpgrade())
                {
                    injectisomeCount += piliValue;
                    defensiveInjectisomeCount += defensivePiliValue;
                    continue;
                }

                pilusCount += piliValue;
                defensivePilusCount += defensivePiliValue;
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
                slimeJetsMultiplier *= CalculateAngleMultiplier(organelle.Position, false);
                continue;
            }

            if (organelle.Definition.HasCiliaComponent)
            {
                if (organelle.Upgrades != null &&
                    organelle.Upgrades.UnlockedFeatures.Contains(CiliaComponent.CILIA_PULL_UPGRADE_NAME))
                {
                    ++pullingCiliasCount;
                    continue;
                }
            }

            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                // Big branch to calculate scores for each toxin type
                if (process.Process.Outputs.TryGetValue(oxytoxy, out var toxinAmount))
                {
                    var activeToxin = organelle.GetActiveToxin();
                    if (activeToxin == ToxinType.Oxytoxy && !hasOxytoxy)
                    {
                        totalToxinTypesCount += 1;
                        hasOxytoxy = true;
                    }

                    if (activeToxin == ToxinType.Cytotoxin && !hasCytoxin)
                    {
                        totalToxinTypesCount += 1;
                        hasCytoxin = true;
                    }

                    if (activeToxin == ToxinType.Macrolide && !hasMacrolide)
                    {
                        totalToxinTypesCount += 1;
                        hasMacrolide = true;
                    }

                    if (activeToxin == ToxinType.ChannelInhibitor && !hasChannelInhibitor)
                    {
                        totalToxinTypesCount += 1;
                        hasChannelInhibitor = true;
                    }

                    if (activeToxin == ToxinType.OxygenMetabolismInhibitor &&
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
                MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity,
                    ToxinType.OxygenMetabolismInhibitor);
        }

        // Having lots of mucocysts and pulling cilias doesn't really help much
        mucocystsScore *= MathF.Sqrt(mucocystsCount);
        pullingCiliaModifier *= 1 + MathF.Sqrt(pullingCiliasCount) * Constants.AUTO_EVO_PULL_CILIA_MODIFIER;

        // Having lots of extra pili also does not help, even if they are two different types
        if (pilusCount != 0 || injectisomeCount != 0)
        {
            var pilusScale = MathF.Sqrt(pilusCount + injectisomeCount) / (pilusCount + injectisomeCount);
            pilusScore *= pilusCount * pilusScale;
            injectisomeScore *= injectisomeCount * pilusScale;
        }
        else
        {
            pilusScore *= pilusCount;
            injectisomeScore *= injectisomeCount;
        }

        if (defensivePilusCount != 0 || defensiveInjectisomeCount != 0)
        {
            var pilusScale = MathF.Sqrt(defensivePilusCount + defensiveInjectisomeCount) /
                (defensivePilusCount + defensiveInjectisomeCount);
            defensivePilusScore *= defensivePilusCount * pilusScale;
            defensiveInjectisomeScore *= defensiveInjectisomeCount * pilusScale;
        }
        else
        {
            defensivePilusScore *= defensivePilusCount;
            defensiveInjectisomeScore *= defensiveInjectisomeCount;
        }

        slimeJetScore *= slimeJetsCount;
        slimeJetScore *= slimeJetsMultiplier;

        // bonus score for upgrades because auto-evo does not like adding them much
        injectisomeScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS_SMALL;
        oxytoxyScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
        macrolideScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
        channelInhibitorScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
        oxygenMetabolismInhibitorScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;

        var predationToolsRawScores = new PredationToolsRawScores(pilusScore, injectisomeScore, defensivePilusScore,
            defensiveInjectisomeScore, averageToxicity, oxytoxyScore, cytotoxinScore, macrolideScore,
            channelInhibitorScore, oxygenMetabolismInhibitorScore, slimeJetScore, mucocystsScore, pullingCiliaModifier);

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
    private float CalculateAngleMultiplier(Hex pos, bool front)
    {
        // Slime jets are biased to go backwards at position (0,0)
        if (pos.R == 0 && pos.Q == 0)
            return 1;

        Vector3 organellePosition = Hex.AxialToCartesian(pos);
        Vector3 downVector = front ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
        float angleCos = organellePosition.Normalized().Dot(downVector);

        // If degrees is higher than 40 then return 0
        return angleCos >= 0.75 ? angleCos : 0;
    }

    // helper for GetPredationToolsRawScores
    public readonly record struct PredationToolsRawScores(float PilusScore,
        float InjectisomeScore,
        float DefensivePilusScore,
        float DefensiveInjectisomeScore,
        float AverageToxicity,
        float OxytoxyScore,
        float CytotoxinScore,
        float MacrolideScore,
        float ChannelInhibitorScore,
        float OxygenMetabolismInhibitorScore,
        float SlimeJetScore,
        float MucocystsScore,
        float PullingCiliaModifier);
}
