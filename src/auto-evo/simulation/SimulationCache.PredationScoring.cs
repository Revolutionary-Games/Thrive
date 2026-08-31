// If set, enables checking whether GetHashCode causes serious duplicate cache value sharing problems
// This define is file-local. To cover predation scoring, uncomment it in both SimulationCache.cs and
// SimulationCache.PredationScoring.cs.
// This uses a ton of extra memory and time, so only enable it while debugging hash reuse.
// #define CHECK_HASH_CODE_REUSED_INSTANCES

namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Systems;

/// <summary>
///   Partial class containing predation scoring and its cached state
/// </summary>
public partial class SimulationCache
{
    /// <summary>
    ///   Raw scores returned by <see cref="GetPredationToolsRawScores(MicrobeSpecies)"/>
    /// </summary>
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

    /// <summary>
    ///   Owns the predation scoring interface and its cached state.
    /// </summary>
    private readonly struct PredationScoring
    {
        private readonly SimulationCache owner;
        private readonly CompoundDefinition oxytoxy = SimulationParameters.GetCompound(Compound.Oxytoxy);

        private readonly Dictionary<(int, int, IBiomeConditions), float> predationScores = new();
        private readonly Dictionary<int, PredationToolsRawScores> cachedPredationToolsRawScores = new();

        public PredationScoring(SimulationCache owner)
        {
            this.owner = owner;
        }

        public float GetScore(Species predatorSpecies, Species preySpecies,
            BiomeConditions biomeConditions)
        {
            if (predatorSpecies is not MicrobeSpecies and not MulticellularSpecies ||
                preySpecies is not MicrobeSpecies and not MulticellularSpecies)
            {
                throw new ArgumentException("Wrong type of Species passed to Microbe/Multicellular Species miche tree");
            }

            // No cannibalism
            if (predatorSpecies == preySpecies)
                return 0.0f;

#if CHECK_HASH_CODE_REUSED_INSTANCES
            owner.CheckSpecies(predatorSpecies);
            owner.CheckSpecies(preySpecies);
#endif

            var key = (microbeSpecies: owner.GetSpeciesCacheKey(predatorSpecies),
                owner.GetSpeciesCacheKey(preySpecies), biomeConditions);

            ref var score = ref CollectionsMarshal.GetValueRefOrNullRef(predationScores, key);
            if (!Unsafe.IsNullRef(ref score))
                return score;

            var calculatedScore = CalculatePredationScore(predatorSpecies, preySpecies, biomeConditions);
            predationScores.Add(key, calculatedScore);
            return calculatedScore;
        }

        public PredationToolsRawScores GetPredationToolsRawScores(MicrobeSpecies microbeSpecies)
        {
            // Seems like this takes twice the amount of time from the predation score calculation
            // if this is not cached,
            // so this should definitely use caching.
            var key = owner.GetSpeciesCacheKey(microbeSpecies);
            ref var score = ref CollectionsMarshal.GetValueRefOrNullRef(cachedPredationToolsRawScores, key);
            if (!Unsafe.IsNullRef(ref score))
            {
                return score;
            }

            var predationToolsRawScores = CalculateMicrobePredationToolsRawScores(microbeSpecies);

            cachedPredationToolsRawScores.Add(key, predationToolsRawScores);
            return predationToolsRawScores;
        }

        public PredationToolsRawScores GetPredationToolsRawScores(MulticellularSpecies multicellularSpecies)
        {
            // Seems like this takes twice the amount of time from the predation score calculation
            // if this is not cached,
            // so this should definitely use caching.
            var key = owner.GetSpeciesCacheKey(multicellularSpecies);
            ref var score = ref CollectionsMarshal.GetValueRefOrNullRef(cachedPredationToolsRawScores, key);
            if (!Unsafe.IsNullRef(ref score))
            {
                return score;
            }

            var predationToolsRawScores = CalculateMulticellularPredationToolsRawScores(multicellularSpecies);

            cachedPredationToolsRawScores.Add(key, predationToolsRawScores);
            return predationToolsRawScores;
        }

        public void Clear()
        {
            predationScores.Clear();
            cachedPredationToolsRawScores.Clear();
        }

        private static ToxinToolScores CalculateToxinToolScores(float averageToxicity, float everyToxinScore,
            in ToxinPresence presence)
        {
            var oxytoxyScore = 0.0f;
            var cytotoxinScore = 0.0f;
            var macrolideScore = 0.0f;
            var channelInhibitorScore = 0.0f;
            var oxygenMetabolismInhibitorScore = 0.0f;

            if (presence.HasOxytoxy)
            {
                oxytoxyScore = everyToxinScore * (Constants.OXYTOXY_DAMAGE / Constants.CYTOTOXIN_DAMAGE) *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity, ToxinType.Oxytoxy);
            }

            if (presence.HasCytotoxin)
            {
                cytotoxinScore = everyToxinScore *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity, ToxinType.Cytotoxin);
            }

            if (presence.HasMacrolide)
                macrolideScore = everyToxinScore;
            if (presence.HasChannelInhibitor)
                channelInhibitorScore = everyToxinScore;
            if (presence.HasOxygenMetabolismInhibitor)
            {
                oxygenMetabolismInhibitorScore = everyToxinScore *
                    (Constants.OXYGEN_INHIBITOR_DAMAGE / Constants.CYTOTOXIN_DAMAGE) *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(averageToxicity,
                        ToxinType.OxygenMetabolismInhibitor);
            }

            return new ToxinToolScores(oxytoxyScore, cytotoxinScore, macrolideScore, channelInhibitorScore,
                oxygenMetabolismInhibitorScore);
        }

        private static PilusToolScores CalculatePilusToolScores(in PilusToolCounts counts)
        {
            var pilusScore = Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
            var injectisomeScore = Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
            var defensivePilusScore = Constants.AUTO_EVO_PILUS_DEFENSE_SCORE;
            var defensiveInjectisomeScore = Constants.AUTO_EVO_PILUS_DEFENSE_SCORE;

            if (counts.Pilus != 0 || counts.Injectisome != 0)
            {
                var pilusScale = MathF.Sqrt(counts.Pilus + counts.Injectisome) / (counts.Pilus + counts.Injectisome);
                pilusScore *= counts.Pilus * pilusScale;
                injectisomeScore *= counts.Injectisome * pilusScale;
            }
            else
            {
                pilusScore *= counts.Pilus;
                injectisomeScore *= counts.Injectisome;
            }

            if (counts.DefensivePilus != 0 || counts.DefensiveInjectisome != 0)
            {
                var pilusScale = MathF.Sqrt(counts.DefensivePilus + counts.DefensiveInjectisome) /
                    (counts.DefensivePilus + counts.DefensiveInjectisome);
                defensivePilusScore *= counts.DefensivePilus * pilusScale;
                defensiveInjectisomeScore *= counts.DefensiveInjectisome * pilusScale;
            }
            else
            {
                defensivePilusScore *= counts.DefensivePilus;
                defensiveInjectisomeScore *= counts.DefensiveInjectisome;
            }

            return new PilusToolScores(pilusScore, injectisomeScore, defensivePilusScore, defensiveInjectisomeScore);
        }

        private PredationToolsRawScores CalculateMicrobePredationToolsRawScores(MicrobeSpecies species)
        {
            var averageToxicity = 0.0f;
            var totalToxicity = 0.0f;
            var totalToxinScore = 0.0f;
            var everyToxinScore = 0.0f;
            var slimeJetScore = Constants.AUTO_EVO_SLIME_JET_SCORE;
            var mucocystsScore = Constants.AUTO_EVO_MUCOCYST_SCORE;
            var pullingCiliaModifier = 1.0f;

            var organelles = species.Organelles.Organelles;
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
            var hasCytotoxin = false;
            var hasMacrolide = false;
            var hasChannelInhibitor = false;
            var hasOxygenMetabolismInhibitor = false;

            for (int i = 0; i < organelleCount; ++i)
            {
                var organelle = organelles[i];

                var organelleDefinition = organelle.Definition;
                if (organelleDefinition.HasPilusComponent)
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

                if (organelleDefinition.HasSlimeJetComponent)
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

                if (organelleDefinition.HasCiliaComponent)
                {
                    if (organelle.Upgrades != null &&
                        organelle.Upgrades.UnlockedFeatures.Contains(CiliaComponent.CILIA_PULL_UPGRADE_NAME))
                    {
                        ++pullingCiliasCount;
                        continue;
                    }
                }

                foreach (var process in organelleDefinition.RunnableProcesses)
                {
                    ref var toxinAmount = ref CollectionsMarshal.GetValueRefOrNullRef(process.Process.Outputs, oxytoxy);
                    if (Unsafe.IsNullRef(ref toxinAmount))
                        continue;

                    // Big branch to calculate scores for each toxin type
                    var activeToxin = organelle.GetActiveToxin();
                    if (activeToxin == ToxinType.Oxytoxy && !hasOxytoxy)
                    {
                        totalToxinTypesCount += 1;
                        hasOxytoxy = true;
                    }

                    if (activeToxin == ToxinType.Cytotoxin && !hasCytotoxin)
                    {
                        totalToxinTypesCount += 1;
                        hasCytotoxin = true;
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

            // Matching current gameplay mechanics of the toxin organelles:

            // Averaging out toxicity, as gameplay also does
            if (totalToxinOrganellesCount != 0)
                averageToxicity = totalToxicity / totalToxinOrganellesCount;

            // Pooled production of toxin compound, equally distributed among all available toxin types
            // (firing in sequence)
            if (totalToxinTypesCount != 0)
            {
                everyToxinScore = totalToxinScore / totalToxinTypesCount;
            }

            var toxinPresence = new ToxinPresence(hasOxytoxy, hasCytotoxin, hasMacrolide, hasChannelInhibitor,
                hasOxygenMetabolismInhibitor);
            var toxinScores = CalculateToxinToolScores(averageToxicity, everyToxinScore, in toxinPresence);
            var oxytoxyScore = toxinScores.Oxytoxy;
            var cytotoxinScore = toxinScores.Cytotoxin;
            var macrolideScore = toxinScores.Macrolide;
            var channelInhibitorScore = toxinScores.ChannelInhibitor;
            var oxygenMetabolismInhibitorScore = toxinScores.OxygenMetabolismInhibitor;

            // Having lots of mucocysts and pulling cilias doesn't really help much
            mucocystsScore *= MathF.Sqrt(mucocystsCount);
            pullingCiliaModifier *= 1 + MathF.Sqrt(pullingCiliasCount) * Constants.AUTO_EVO_PULL_CILIA_MODIFIER;

            // Having lots of extra pili also does not help, even if they are two different types
            var pilusCounts = new PilusToolCounts(pilusCount, injectisomeCount, defensivePilusCount,
                defensiveInjectisomeCount);
            var pilusScores = CalculatePilusToolScores(in pilusCounts);
            var pilusScore = pilusScores.Pilus;
            var injectisomeScore = pilusScores.Injectisome;
            var defensivePilusScore = pilusScores.DefensivePilus;
            var defensiveInjectisomeScore = pilusScores.DefensiveInjectisome;

            slimeJetScore *= slimeJetsCount;
            slimeJetScore *= slimeJetsMultiplier;

            // application of specializationBonus to appropriate scores (microbe, so only CellTypeSpecializationBonus)
            var specializationBonus = species.CellTypeSpecializationBonus;

            oxytoxyScore *= specializationBonus;
            cytotoxinScore *= specializationBonus;
            channelInhibitorScore *= specializationBonus;
            macrolideScore *= specializationBonus;
            slimeJetScore *= specializationBonus;
            pullingCiliaModifier *= specializationBonus;

            // bonus score for upgrades because auto-evo does not like adding them much
            injectisomeScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS_SMALL;
            oxytoxyScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
            macrolideScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
            channelInhibitorScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;
            oxygenMetabolismInhibitorScore *= Constants.AUTO_EVO_ARTIFICIAL_UPGRADE_BONUS;

            var predationToolsRawScores = new PredationToolsRawScores(pilusScore, injectisomeScore, defensivePilusScore,
                defensiveInjectisomeScore, averageToxicity, oxytoxyScore, cytotoxinScore, macrolideScore,
                channelInhibitorScore, oxygenMetabolismInhibitorScore, slimeJetScore, mucocystsScore,
                pullingCiliaModifier);
            return predationToolsRawScores;
        }

        private PredationToolsRawScores CalculateMulticellularPredationToolsRawScores(MulticellularSpecies species)
        {
            var averageToxicity = 0.0f;
            var totalToxicity = 0.0f;
            var totalToxinAmount = 0.0f;
            var everyToxinScore = 0.0f;
            var slimeJetScore = Constants.AUTO_EVO_SLIME_JET_SCORE;
            var mucocystsScore = Constants.AUTO_EVO_MUCOCYST_SCORE;
            var pullingCiliaModifier = 1.0f;

            var totalToxinOrganellesCount = 0;
            var totalToxinTypesCount = 0;
            var pilusCount = 0.0f;
            var injectisomeCount = 0.0f;
            var defensivePilusCount = 0.0f;
            var defensiveInjectisomeCount = 0.0f;
            var slimeJetsCount = 0.0f;
            var mucocystsCount = 0;
            var pullingCiliasCount = 0.0f;
            var slimeJetsMultiplier = 1.0f;
            var slimeJetsMultiplierSum = 0.0f;
            var slimeJetsMultiplierCount = 0;

            var hasOxytoxy = false;
            var hasCytotoxin = false;
            var hasMacrolide = false;
            var hasChannelInhibitor = false;
            var hasOxygenMetabolismInhibitor = false;

            var cellTypes = species.CellTypes;
            for (var i = 0; i < cellTypes.Count; ++i)
            {
                var cellType = cellTypes[i];

                var cellTypeToxinAmount = 0.0f;
                var cellTypeToxinOrganellesCount = 0;
                var cellTypeToxinTypesCount = 0;
                var cellTypeToxicity = 0.0f;
                var cellTypePilusCount = 0.0f;
                var cellTypeInjectisomeCount = 0.0f;
                var cellTypeDefensivePilusCount = 0.0f;
                var cellTypeDefensiveInjectisomeCount = 0.0f;
                var cellTypeSlimeJetsCount = 0;
                var cellTypeMucocystsCount = 0;
                var cellTypePullingCiliasCount = 0;
                var cellTypeSlimeJetsMultiplier = 1.0f;

                var organelles = cellType.Organelles;
                foreach (var organelle in organelles)
                {
                    var organelleDefinition = organelle.Definition;
                    if (organelleDefinition.HasPilusComponent)
                    {
                        // Make sure that pili are positioned at the front of the cell for offensive action,
                        // and the back of the cell for defensive action
                        var piliValue = CalculateAngleMultiplier(organelle.Position, true);
                        var defensivePiliValue = CalculateAngleMultiplier(organelle.Position, false);
                        if (organelle.Upgrades.HasInjectisomeUpgrade())
                        {
                            cellTypeInjectisomeCount += piliValue;
                            cellTypeDefensiveInjectisomeCount += defensivePiliValue;
                            continue;
                        }

                        cellTypePilusCount += piliValue;
                        cellTypeDefensivePilusCount += defensivePiliValue;
                        continue;
                    }

                    if (organelleDefinition.HasSlimeJetComponent)
                    {
                        if (organelle.Upgrades?.UnlockedFeatures.Contains(SlimeJetComponent.MUCOCYST_UPGRADE_NAME) ==
                            true)
                        {
                            ++cellTypeMucocystsCount;
                            continue;
                        }

                        ++cellTypeSlimeJetsCount;

                        // Make sure that slime jets are positioned at the back of the cell, because otherwise they will
                        // push the cell backwards (into the predator or away from the prey) or to the side
                        cellTypeSlimeJetsMultiplier *= CalculateAngleMultiplier(organelle.Position, false);
                        continue;
                    }

                    if (organelleDefinition.HasCiliaComponent)
                    {
                        if (organelle.Upgrades != null &&
                            organelle.Upgrades.UnlockedFeatures.Contains(CiliaComponent.CILIA_PULL_UPGRADE_NAME))
                        {
                            ++cellTypePullingCiliasCount;
                            continue;
                        }
                    }

                    foreach (var process in organelleDefinition.RunnableProcesses)
                    {
                        ref var toxinAmount =
                            ref CollectionsMarshal.GetValueRefOrNullRef(process.Process.Outputs, oxytoxy);
                        if (Unsafe.IsNullRef(ref toxinAmount))
                            continue;

                        // Big branch to calculate scores for each toxin type
                        var activeToxin = organelle.GetActiveToxin();
                        if (activeToxin == ToxinType.Oxytoxy && !hasOxytoxy)
                        {
                            cellTypeToxinTypesCount += 1;
                            hasOxytoxy = true;
                        }

                        if (activeToxin == ToxinType.Cytotoxin && !hasCytotoxin)
                        {
                            cellTypeToxinTypesCount += 1;
                            hasCytotoxin = true;
                        }

                        if (activeToxin == ToxinType.Macrolide && !hasMacrolide)
                        {
                            cellTypeToxinTypesCount += 1;
                            hasMacrolide = true;
                        }

                        if (activeToxin == ToxinType.ChannelInhibitor && !hasChannelInhibitor)
                        {
                            cellTypeToxinTypesCount += 1;
                            hasChannelInhibitor = true;
                        }

                        if (activeToxin == ToxinType.OxygenMetabolismInhibitor &&
                            !hasOxygenMetabolismInhibitor)
                        {
                            cellTypeToxinTypesCount += 1;
                            hasOxygenMetabolismInhibitor = true;
                        }

                        cellTypeToxicity += organelle.GetActiveToxicity();
                        cellTypeToxinOrganellesCount += 1;
                        cellTypeToxinAmount += toxinAmount;
                    }
                }

                // There are likely more accurate ways to approximate the real gameplay effects in the future, but this
                // will do for now
                totalToxinTypesCount += cellTypeToxinTypesCount;

                var cells = species.EditorCells;

                foreach (var hex in cells)
                {
                    var cell = hex.Data;
                    if (cell != null && ReferenceEquals(cell.CellType, cellType))
                    {
                        totalToxinOrganellesCount += cellTypeToxinOrganellesCount;
                        totalToxicity += cellTypeToxicity;
                        pilusCount += cellTypePilusCount;
                        injectisomeCount += cellTypeInjectisomeCount;
                        defensivePilusCount += cellTypeDefensivePilusCount;
                        defensiveInjectisomeCount += cellTypeDefensiveInjectisomeCount;
                        mucocystsCount += cellTypeMucocystsCount;
                        slimeJetsMultiplierSum += cellTypeSlimeJetsMultiplier;
                        ++slimeJetsMultiplierCount;

                        // application of specializationBonus to appropriate scores
                        var specializationBonus = cellType.CellTypeSpecializationBonus *
                            CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(cell, cells);

                        totalToxinAmount += cellTypeToxinAmount * specializationBonus;
                        slimeJetsCount += cellTypeSlimeJetsCount * specializationBonus;
                        pullingCiliasCount += cellTypePullingCiliasCount * specializationBonus;
                    }
                }
            }

            // Matching current gameplay mechanics of the toxin organelles:

            // Averaging out toxicity, as gameplay also does
            if (totalToxinOrganellesCount != 0)
                averageToxicity = totalToxicity / totalToxinOrganellesCount;

            // Pooled production of toxin compound, equally distributed among all available toxin types
            // (firing in sequence)
            if (totalToxinTypesCount != 0)
            {
                everyToxinScore = totalToxinAmount * Constants.AUTO_EVO_TOXIN_PREDATION_SCORE / totalToxinTypesCount;
            }

            var toxinPresence = new ToxinPresence(hasOxytoxy, hasCytotoxin, hasMacrolide, hasChannelInhibitor,
                hasOxygenMetabolismInhibitor);
            var toxinScores = CalculateToxinToolScores(averageToxicity, everyToxinScore, in toxinPresence);
            var oxytoxyScore = toxinScores.Oxytoxy;
            var cytotoxinScore = toxinScores.Cytotoxin;
            var macrolideScore = toxinScores.Macrolide;
            var channelInhibitorScore = toxinScores.ChannelInhibitor;
            var oxygenMetabolismInhibitorScore = toxinScores.OxygenMetabolismInhibitor;

            // Having lots of mucocysts and pulling cilias doesn't really help much
            mucocystsScore *= MathF.Sqrt(mucocystsCount);
            pullingCiliaModifier *= 1 + MathF.Sqrt(pullingCiliasCount) * Constants.AUTO_EVO_PULL_CILIA_MODIFIER;

            // Having lots of extra pili also does not help, even if they are two different types
            var pilusCounts = new PilusToolCounts(pilusCount, injectisomeCount, defensivePilusCount,
                defensiveInjectisomeCount);
            var pilusScores = CalculatePilusToolScores(in pilusCounts);
            var pilusScore = pilusScores.Pilus;
            var injectisomeScore = pilusScores.Injectisome;
            var defensivePilusScore = pilusScores.DefensivePilus;
            var defensiveInjectisomeScore = pilusScores.DefensiveInjectisome;

            if (slimeJetsMultiplierCount > 0)
                slimeJetsMultiplier = slimeJetsMultiplierSum / slimeJetsMultiplierCount;
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
                channelInhibitorScore, oxygenMetabolismInhibitorScore, slimeJetScore, mucocystsScore,
                pullingCiliaModifier);
            return predationToolsRawScores;
        }

        private float CalculatePredationScore(Species predatorSpecies, Species preySpecies,
            BiomeConditions biomeConditions)
        {
            var noPredationScore = 0.0f;

            if (!TryGetPredatorCapabilities(predatorSpecies, out var predatorCapabilities))
                return 0;

            var predatorToolScores = predatorCapabilities.ToolScores;
            var canEngulf = predatorCapabilities.CanEngulf;

            var pilusScore = predatorToolScores.PilusScore;
            var injectisomeScore = predatorToolScores.InjectisomeScore;
            var oxytoxyScore = predatorToolScores.OxytoxyScore;
            var cytotoxinScore = predatorToolScores.CytotoxinScore;
            var oxygenMetabolismInhibitorScore = predatorToolScores.OxygenMetabolismInhibitorScore;
            var channelInhibitorScore = predatorToolScores.ChannelInhibitorScore;

            // Don't bother with the rest if the predator cannot predate
            var engulfOnly = false;

            if (pilusScore == 0 &&
                injectisomeScore == 0 &&
                oxytoxyScore == 0 &&
                cytotoxinScore == 0 &&
                oxygenMetabolismInhibitorScore == 0 &&
                channelInhibitorScore == 0)
            {
                if (canEngulf)
                {
                    engulfOnly = true;
                }
                else
                {
                    return noPredationScore;
                }
            }

            // Constants
            const float sprintMultiplier = Constants.SPRINTING_FORCE_MULTIPLIER;
            const float sprintingStrain = Constants.SPRINTING_STRAIN_INCREASE_PER_SECOND / 5;
            const float strainPerHex = Constants.SPRINTING_STRAIN_INCREASE_PER_HEX / 5;

            const float membraneRigidityHitpointsModifier = Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;

            const float sizeAffectedProjectileMissFactor = Constants.AUTO_EVO_SIZE_AFFECTED_PROJECTILE_MISS_FACTOR;
            const float toxicityHitModifier = Constants.AUTO_EVO_TOXICITY_HIT_MODIFIER;
            const float oxytoxyDebuffPerOrganelle = Constants.OXYTOXY_DAMAGE_DEBUFF_PER_ORGANELLE;
            const float oxytoxyDebuffMax = Constants.OXYTOXY_DAMAGE_DEBUFF_MAX;
            const float oxygenInhibitorBuffPerOrganelle = Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_PER_ORGANELLE;
            const float oxygenInhibitorBuffMax = Constants.OXYGEN_INHIBITOR_DAMAGE_BUFF_MAX;
            const float oxytoxyDamage = Constants.OXYTOXY_DAMAGE;
            const float channelInhibitorATPDebuff = Constants.CHANNEL_INHIBITOR_ATP_DEBUFF;

            const float signallingBonus = Constants.AUTO_EVO_SIGNALLING_BONUS;

            // full calculation of values for PredationScore follows
            if (!TryCollectPreyPredationData(preySpecies, membraneRigidityHitpointsModifier, out var preyData))
                return 0;

            if (!TryCollectPredatorPredationData(predatorSpecies, preySpecies, membraneRigidityHitpointsModifier,
                    canEngulf, in preyData, out var predatorData))
                return 0;

            var preyToolScores = preyData.ToolScores;
            var preyHexSize = preyData.HexSize;

            var predatorHexSize = predatorData.HexSize;
            var hasChemoreceptor = predatorData.HasChemoreceptor;
            var enzymesScore = predatorData.EnzymesScore;

            var canDigestPrey = enzymesScore > 0.0f;

            if (engulfOnly && !canDigestPrey)
            {
                noPredationScore = 0;
                return noPredationScore;
            }

            // We want prey defensive measures to only reduce predation score, not eliminate it.
            // (Predation Score is reduced to 0 anyway if the "prey" has a higher predation score to the predator)
            const float defenseScoreModifier = Constants.AUTO_EVO_PREDATION_DEFENSE_SCORE_MODIFIER;

            var predatorSpeed = owner.GetSpeedForSpecies(predatorSpecies);
            var predatorRotationSpeed = owner.GetRotationSpeedForSpecies(predatorSpecies);
            var predatorEnergyBalance = owner.GetEnergyBalanceForSpecies(predatorSpecies, biomeConditions);
            var predatorOsmoregulationCost = predatorEnergyBalance.Osmoregulation;

            var preySpeed = owner.GetSpeedForSpecies(preySpecies);
            var preyRotationSpeed = owner.GetRotationSpeedForSpecies(preySpecies);
            var slowedPreySpeed = preySpeed;
            var preyEnergyBalance = owner.GetEnergyBalanceForSpecies(preySpecies, biomeConditions);
            var preyOsmoregulationCost = preyEnergyBalance.Osmoregulation;
            var preyIndividualCost = MichePopulation.CalculateIndividualCost(preySpecies, biomeConditions, owner);

            var toxicity = predatorToolScores.AverageToxicity;
            var macrolideScore = predatorToolScores.MacrolideScore;
            var predatorSlimeJetScore = predatorToolScores.SlimeJetScore;

            var preySlimeJetScore = preyToolScores.SlimeJetScore;
            var preyMucocystsScore = preyToolScores.MucocystsScore;
            var preyToxicity = preyToolScores.AverageToxicity;
            var preyOxytoxyScore = preyToolScores.OxytoxyScore;
            var preyCytotoxinScore = preyToolScores.CytotoxinScore;
            var preyChannelInhibitorScore = preyToolScores.ChannelInhibitorScore;
            var preyOxygenMetabolismInhibitorScore = preyToolScores.OxygenMetabolismInhibitorScore;

            // Not an ideal solution, but accounts for the fact that the oxytoxy and cyanide processes
            // require oxygen to run
            biomeConditions.Compounds.TryGetValue(Compound.Oxygen, out var oxygen);
            if (oxygen.Ambient == 0)
            {
                oxytoxyScore = 0;
                preyOxytoxyScore = 0;
                oxygenMetabolismInhibitorScore = 0;
                preyOxygenMetabolismInhibitorScore = 0;
            }

            var aggressionScore = predatorSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;
            var activityScore = MathF.Pow(predatorSpecies.Behaviour.Activity / Constants.MAX_SPECIES_ACTIVITY, 0.5f);
            var opportunismScore = predatorSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            var focusScore = predatorSpecies.Behaviour.Focus / Constants.MAX_SPECIES_FOCUS;

            var preyFearScore = preySpecies.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;
            var preyAggressionScore = preySpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;
            var preyOpportunismScore = preySpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            var preyFocusScore = preySpecies.Behaviour.Focus / Constants.MAX_SPECIES_FOCUS;

            // prey's effectiveness at running away depends on how quickly they choose to run away
            preySpeed *= preyFearScore * (1 - preyAggressionScore);

            // Sprinting calculations
            var predatorSprintSpeed = predatorSpeed * sprintMultiplier;
            var predatorSprintConsumption = sprintingStrain + predatorHexSize * strainPerHex;
            var predatorSprintTime = MathF.Max(predatorEnergyBalance.FinalBalance / predatorSprintConsumption, 0.0f);

            var preySprintSpeed = preySpeed * sprintMultiplier;
            var preySprintConsumption = sprintingStrain + preyHexSize * strainPerHex;
            var preySprintTime = MathF.Max(preyEnergyBalance.FinalBalance / preySprintConsumption, 0.0f);

            // This makes rotation "speed" not matter until the editor shows ~300,
            // which is where it also becomes noticeable in-game.
            // The mechanical microbe rotation speed value is reverse to intuitive: higher value means slower turning.
            // (The editor reverses this to make it intuitive to the player)
            var predatorRotationModifier = float.Min(1.0f, 1.5f - predatorRotationSpeed * 1.45f);
            var preyRotationModifier = float.Min(1.0f, 1.5f - preyRotationSpeed * 1.45f);

            // Simple estimation of slime jet propulsion.
            var predatorSlimeSpeed = predatorSpeed + predatorSlimeJetScore / (predatorHexSize * 11);
            var preySlimeSpeed = preySpeed + preySlimeJetScore / (preyHexSize * 11);

            // Calculating "hit chance" modifier from prey size and predator toxicity
            var sizeHitFactor = sizeAffectedProjectileMissFactor / float.Sqrt(preyHexSize);
            var toxicityHitFactor = toxicity / toxicityHitModifier;
            var hitProportion = 1 - sizeHitFactor - toxicityHitFactor;

            // Calculating prey energy production altered by channel inhibitor
            var preyInhibitedPreyEnergyProduction = preyEnergyBalance.TotalProduction;
            if (channelInhibitorScore > 0)
            {
                preyInhibitedPreyEnergyProduction *= 1 - channelInhibitorATPDebuff *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.ChannelInhibitor);

                // If inhibited energy production affects movement,
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

            // Calculating predator energy production altered by channel inhibitor
            var predatorInhibitedPreyEnergyProduction = predatorEnergyBalance.TotalProduction;
            if (preyChannelInhibitorScore > 0)
            {
                predatorInhibitedPreyEnergyProduction *= 1 - channelInhibitorATPDebuff *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(preyToxicity, ToxinType.ChannelInhibitor);
            }

            // Calculating how much prey is slowed down by macrolide, and how frequently they are
            // succesfully slowed down
            var slowedProportion = 0.0f;
            if (macrolideScore > 0)
            {
                slowedPreySpeed *= 1 - Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF *
                    MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.Macrolide);
                slowedProportion = 1.0f - MathF.Exp(-Constants.AUTO_EVO_TOXIN_AFFECTED_PROPORTION_SCALING *
                    macrolideScore * hitProportion);
            }

            var catchScore = CalculateCatchScores(canDigestPrey, in predatorToolScores, predatorSpeed, preySpeed,
                slowedProportion, slowedPreySpeed, predatorSprintSpeed, predatorSprintTime, preySprintSpeed,
                preySprintTime, predatorSlimeSpeed, preySlimeSpeed, predatorRotationModifier, hasChemoreceptor,
                preyIndividualCost, activityScore, focusScore, preyRotationModifier, preyOpportunismScore,
                preyFocusScore,
                out var accidentalCatchScore);

            pilusScore = CalculatePhysicalPredationScores(in predatorData, in preyData, in predatorToolScores,
                preyOxytoxyScore, preyOxygenMetabolismInhibitorScore, preyRotationModifier, preyFearScore,
                preyAggressionScore, preyOpportunismScore, catchScore, accidentalCatchScore, defenseScoreModifier,
                signallingBonus, canDigestPrey, out var preyPilusScore, out var engulfmentScore);

            // Damaging toxin section

            var predatorToxins = (Oxytoxy: oxytoxyScore, Cytotoxin: cytotoxinScore,
                OxygenMetabolismInhibitor: oxygenMetabolismInhibitorScore, ChannelInhibitor: channelInhibitorScore);
            var preyToxins = (Oxytoxy: preyOxytoxyScore, Cytotoxin: preyCytotoxinScore,
                OxygenMetabolismInhibitor: preyOxygenMetabolismInhibitorScore, Toxicity: preyToxicity);
            var inhibitedEnergy = (PreyProduction: preyInhibitedPreyEnergyProduction,
                PreyOsmoregulationCost: preyOsmoregulationCost,
                PredatorProduction: predatorInhibitedPreyEnergyProduction,
                PredatorOsmoregulationCost: predatorOsmoregulationCost);
            var preyToxinBehaviour = (Fear: preyFearScore, Aggression: preyAggressionScore,
                Opportunism: preyOpportunismScore);
            var predatorToxinBehaviour = (Activity: activityScore, Focus: focusScore);
            var toxinEncounter = (HitProportion: hitProportion, PredatorRotationModifier: predatorRotationModifier,
                PreyRotationModifier: preyRotationModifier, PreyIndividualCost: preyIndividualCost);
            var toxinConstants = (OxytoxyDebuffPerOrganelle: oxytoxyDebuffPerOrganelle,
                OxytoxyDebuffMax: oxytoxyDebuffMax,
                OxygenInhibitorBuffPerOrganelle: oxygenInhibitorBuffPerOrganelle,
                OxygenInhibitorBuffMax: oxygenInhibitorBuffMax, OxytoxyDamage: oxytoxyDamage,
                SizeAffectedProjectileMissFactor: sizeAffectedProjectileMissFactor,
                ToxicityHitModifier: toxicityHitModifier);
            var toxinModifiers = (SignallingBonus: signallingBonus, DefenseScoreModifier: defenseScoreModifier);

            var (damagingToxinScore, preyDamagingToxinScore) = CalculateToxinScores(predatorSpecies,
                in predatorData, in preyData, in predatorToxins, in preyToxins, in inhibitedEnergy,
                in preyToxinBehaviour, in predatorToxinBehaviour, in toxinEncounter, in toxinConstants,
                in toxinModifiers);

            var predatorScores = (Pilus: pilusScore, Engulfment: engulfmentScore,
                DamagingToxin: damagingToxinScore);
            var preyDefenseScores = (SlimeJet: preySlimeJetScore, Mucocysts: preyMucocystsScore,
                Pilus: preyPilusScore, DamagingToxin: preyDamagingToxinScore);

            return CombinePredationScores(canEngulf, predatorSlimeJetScore, aggressionScore, opportunismScore,
                in predatorScores, in preyDefenseScores);
        }

        private float CombinePredationScores(bool canEngulf, float predatorSlimeJetScore, float aggressionScore,
            float opportunismScore, in (float Pilus, float Engulfment, float DamagingToxin) predatorScores,
            in (float SlimeJet, float Mucocysts, float Pilus, float DamagingToxin) preyDefenseScores)
        {
            var scoreMultiplier = 1.0f;

            if (!canEngulf)
            {
                // If you can't engulf, you just get energy from the chunks leaking.
                scoreMultiplier *= Constants.AUTO_EVO_CHUNK_LEAK_MULTIPLIER;
            }

            // predators that have slime jets themselves ignore the immobilising effect of prey slimejets
            var preySlimeJetScore = MathF.Sqrt(preyDefenseScores.SlimeJet);
            if (predatorSlimeJetScore > 0)
                preySlimeJetScore = 0;

            var combinedScore = scoreMultiplier * MathF.Pow(aggressionScore, 0.5f) *
                (1 + MathF.Pow(opportunismScore, 0.5f * Constants.AUTO_EVO_MAX_OPPORTUNISM_BONUS)) *
                ((predatorScores.Pilus + predatorScores.Engulfment + predatorScores.DamagingToxin) /
                    Math.Max(1, preySlimeJetScore + preyDefenseScores.Mucocysts + preyDefenseScores.Pilus +
                        preyDefenseScores.DamagingToxin));
            if (combinedScore < 0)
                combinedScore = 0;

            return combinedScore;
        }

        private (float Predator, float Prey) CalculateToxinScores(Species predatorSpecies,
            in PredatorPredationData predatorData, in PreyPredationData preyData,
            in (float Oxytoxy, float Cytotoxin, float OxygenMetabolismInhibitor, float ChannelInhibitor) predatorToxins,
            in (float Oxytoxy, float Cytotoxin, float OxygenMetabolismInhibitor, float Toxicity) preyToxins,
            in (float PreyProduction, float PreyOsmoregulationCost, float PredatorProduction,
                float PredatorOsmoregulationCost) inhibitedEnergy,
            in (float Fear, float Aggression, float Opportunism) preyBehaviour,
            in (float Activity, float Focus) predatorBehaviour,
            in (float HitProportion, float PredatorRotationModifier, float PreyRotationModifier,
                float PreyIndividualCost) encounter,
            in (float OxytoxyDebuffPerOrganelle, float OxytoxyDebuffMax, float OxygenInhibitorBuffPerOrganelle,
                float OxygenInhibitorBuffMax, float OxytoxyDamage, float SizeAffectedProjectileMissFactor,
                float ToxicityHitModifier) toxinConstants,
            in (float SignallingBonus, float DefenseScoreModifier) modifiers)
        {
            var oxytoxyScore = predatorToxins.Oxytoxy;
            oxytoxyScore *= 1 - Math.Min(preyData.OxygenUsingOrganellesCount *
                toxinConstants.OxytoxyDebuffPerOrganelle, toxinConstants.OxytoxyDebuffMax);
            var oxygenMetabolismInhibitorScore = predatorToxins.OxygenMetabolismInhibitor;
            oxygenMetabolismInhibitorScore *= 1 + Math.Min(preyData.OxygenUsingOrganellesCount *
                toxinConstants.OxygenInhibitorBuffPerOrganelle, toxinConstants.OxygenInhibitorBuffMax);
            var damagingToxinScore = oxytoxyScore + predatorToxins.Cytotoxin + oxygenMetabolismInhibitorScore;

            var preyOxytoxyScore = preyToxins.Oxytoxy;
            preyOxytoxyScore *= 1 - Math.Min(predatorData.OxygenUsingOrganellesCount *
                toxinConstants.OxytoxyDebuffPerOrganelle, toxinConstants.OxytoxyDebuffMax);
            var preyOxygenMetabolismInhibitorScore = preyToxins.OxygenMetabolismInhibitor;
            preyOxygenMetabolismInhibitorScore *= 1 + Math.Min(
                predatorData.OxygenUsingOrganellesCount * toxinConstants.OxygenInhibitorBuffPerOrganelle,
                toxinConstants.OxygenInhibitorBuffMax);
            var preyDamagingToxinScore = preyOxytoxyScore + preyToxins.Cytotoxin +
                preyOxygenMetabolismInhibitorScore;

            // If toxin-inhibited energy production is lower than osmoregulation cost, channel inhibitor is a
            // damaging toxin
            if (inhibitedEnergy.PreyProduction < inhibitedEnergy.PreyOsmoregulationCost)
                damagingToxinScore += predatorToxins.ChannelInhibitor;
            if (inhibitedEnergy.PredatorProduction < inhibitedEnergy.PredatorOsmoregulationCost)
                damagingToxinScore += predatorToxins.ChannelInhibitor;

            // MicrobeAISystem makes prey not fire toxins against predators under this condition
            if (preyBehaviour.Fear >= preyBehaviour.Aggression)
                preyDamagingToxinScore = 0;

            if (damagingToxinScore > 0)
            {
                // Applying projectile hit chance to damaging toxins
                damagingToxinScore *= encounter.HitProportion;

                // Predators are less likely to use toxin against larger prey, unless they are opportunistic
                if (preyData.HexSize > predatorData.HexSize)
                {
                    damagingToxinScore *= predatorSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
                }

                // If you can store enough to kill the prey, producing more isn't as important
                var storageToKillRatio = predatorData.StorageNominal * toxinConstants.OxytoxyDamage /
                    (preyData.Hitpoints * preyData.ToxinResistance);
                storageToKillRatio = Math.Min(storageToKillRatio, 1);

                damagingToxinScore = MathF.Pow(damagingToxinScore, storageToKillRatio * 0.8f);

                // Targets that resist toxin are of course less vulnerable to being damaged with it
                damagingToxinScore /= preyData.Hitpoints * preyData.ToxinResistance;

                // Toxins also require facing and tracking the target
                damagingToxinScore *= encounter.PredatorRotationModifier;

                // Calling for allies helps with combat.
                if (predatorData.HasSignallingAgent)
                    damagingToxinScore *= modifiers.SignallingBonus;

                // If you have a chemoreceptor, active hunting types are more effective
                if (predatorData.HasChemoreceptor)
                {
                    damagingToxinScore *= Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_BASE_MODIFIER;
                    damagingToxinScore *= 1 + Constants.AUTO_EVO_CHEMORECEPTOR_PREDATION_VARIABLE_MODIFIER
                        * float.Sqrt(encounter.PreyIndividualCost);
                }

                // Active hunting is more effective for active species
                damagingToxinScore *= predatorBehaviour.Activity;
                damagingToxinScore *= 1 + predatorBehaviour.Focus;
            }

            if (preyDamagingToxinScore > 0)
            {
                // Calculating "hit chance" modifier from predator size and prey toxicity
                var predatorSizeHitFactor = toxinConstants.SizeAffectedProjectileMissFactor /
                    float.Sqrt(predatorData.HexSize);
                var preyToxicityHitFactor = preyToxins.Toxicity / toxinConstants.ToxicityHitModifier;
                var preyHitProportion = 1 - predatorSizeHitFactor - preyToxicityHitFactor;

                // Applying projectile hit chance to damaging toxins
                preyDamagingToxinScore *= preyHitProportion;

                // Prey are less likely to use toxin against larger predators, unless they are opportunistic
                if (predatorData.HexSize > preyData.HexSize)
                {
                    preyDamagingToxinScore *= preyBehaviour.Opportunism;
                }

                // If you can store enough to kill the predator, producing more isn't as important
                var preyStorageToKillRatio = preyData.StorageNominal * toxinConstants.OxytoxyDamage /
                    (predatorData.Hitpoints * predatorData.ToxinResistance);
                preyStorageToKillRatio = Math.Min(preyStorageToKillRatio, 1);

                preyDamagingToxinScore = MathF.Pow(preyDamagingToxinScore, preyStorageToKillRatio * 0.8f);

                // Targets that resist toxin are of course less vulnerable to being damaged with it
                preyDamagingToxinScore /= predatorData.Hitpoints * predatorData.ToxinResistance;

                // Toxins also require facing and tracking the target
                preyDamagingToxinScore *= encounter.PreyRotationModifier;

                // Calling for allies helps with combat.
                if (preyData.HasSignallingAgent)
                    preyDamagingToxinScore *= modifiers.SignallingBonus;

                // Prey can use toxins for defense, but only if they have the right behaviour
                preyDamagingToxinScore *= encounter.PreyRotationModifier * modifiers.DefenseScoreModifier *
                    preyBehaviour.Aggression * (1 - preyBehaviour.Fear);
            }

            return (damagingToxinScore, preyDamagingToxinScore);
        }

        private float CalculatePhysicalPredationScores(in PredatorPredationData predatorData,
            in PreyPredationData preyData, in PredationToolsRawScores predatorToolScores, float preyOxytoxyScore,
            float preyOxygenMetabolismInhibitorScore, float preyRotationModifier, float preyFearScore,
            float preyAggressionScore, float preyOpportunismScore, float catchScore, float accidentalCatchScore,
            float defenseScoreModifier, float signallingBonus, bool canDigestPrey, out float preyPilusScore,
            out float engulfmentScore)
        {
            var preyToolScores = preyData.ToolScores;

            var pilusScore = predatorToolScores.PilusScore;
            var injectisomeScore = predatorToolScores.InjectisomeScore;
            preyPilusScore = preyToolScores.PilusScore;
            var preyInjectisomeScore = preyToolScores.InjectisomeScore;
            var defensivePilusScore = preyToolScores.DefensivePilusScore;
            var defensiveInjectisomeScore = preyToolScores.DefensiveInjectisomeScore;

            var preyHP = preyData.Hitpoints;
            var preyToxinResistance = preyData.ToxinResistance;
            var preyPhysicalResistance = preyData.PhysicalResistance;
            var preyCytotoxinScore = preyToolScores.CytotoxinScore;
            var preyMacrolideScore = preyToolScores.MacrolideScore;
            var preyChannelInhibitorScore = preyToolScores.ChannelInhibitorScore;
            var preyHasSignallingAgent = preyData.HasSignallingAgent;
            var preyHexSize = preyData.HexSize;

            var predatorHP = predatorData.Hitpoints;
            var predatorToxinResistance = predatorData.ToxinResistance;
            var predatorPhysicalResistance = predatorData.PhysicalResistance;
            var predatorHexSize = predatorData.HexSize;
            var hasSignallingAgent = predatorData.HasSignallingAgent;
            var enzymesScore = predatorData.EnzymesScore;

            engulfmentScore = 0.0f;

            // targets that resist physical damage are of course less vulnerable to it
            pilusScore /= preyHP * preyPhysicalResistance;
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
            defensivePilusScore *= preyRotationModifier * preyFearScore * (1 - preyAggressionScore);

            // Calling for allies helps with combat.
            if (hasSignallingAgent)
                pilusScore *= signallingBonus;
            if (preyHasSignallingAgent)
                preyPilusScore *= signallingBonus;

            // Use catch score for Pili
            pilusScore /= Math.Max(1, defensivePilusScore);
            pilusScore *= catchScore + accidentalCatchScore;

            // Prey can use offensive pili for defense in these encounters, but only if they have the right behavior
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
                totalPreyToxinContent /= predatorHP * predatorToxinResistance;

                // Final engulfment score calculation
                // Engulfing prey by luck is especially easy if you are huge.
                // This is also used to incentivize size in microbe species.
                engulfmentScore = (catchScore + accidentalCatchScore * predatorHexSize) *
                    (Constants.AUTO_EVO_ENGULF_PREDATION_SCORE /
                        Math.Max(1, defensivePilusScore + totalPreyToxinContent));
                engulfmentScore *= enzymesScore;
            }

            return pilusScore;
        }

        private float CalculateCatchScores(bool canDigestPrey, in PredationToolsRawScores predatorToolScores,
            float predatorSpeed, float preySpeed, float slowedProportion, float slowedPreySpeed,
            float predatorSprintSpeed,
            float predatorSprintTime, float preySprintSpeed, float preySprintTime, float predatorSlimeSpeed,
            float preySlimeSpeed, float predatorRotationModifier, bool hasChemoreceptor, float preyIndividualCost,
            float activityScore, float focusScore, float preyRotationModifier, float preyOpportunismScore,
            float preyFocusScore, out float accidentalCatchScore)
        {
            var pilusScore = predatorToolScores.PilusScore;
            var injectisomeScore = predatorToolScores.InjectisomeScore;
            var pullingCiliaModifier = predatorToolScores.PullingCiliaModifier;
            var strongPullingCiliaModifier = pullingCiliaModifier * pullingCiliaModifier;

            // Catch scores grossly accounts for how many preys you catch in melee in a run;
            var catchScore = 0.0f;
            accidentalCatchScore = 0.0f;

            // Only calculate catch score if one can actually engulf (and digest) or use pili
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
                catchScore *= 1 + focusScore;

                // ... but you may also catch them by luck (e.g. when they run into you),
                // Prey that can't turn away fast enough are more likely to get caught.
                accidentalCatchScore = Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY *
                    strongPullingCiliaModifier * preyRotationModifier;

                // Less cautious and more focused prey are slightly more likely to get into a dangerous situation
                var opportunismPenalty = MathF.Pow(preyOpportunismScore, 1.5f)
                    * Constants.AUTO_EVO_MAX_OPPORTUNISM_PENALTY;
                var focusPenalty = MathF.Pow(preyFocusScore, 1.5f)
                    * Constants.AUTO_EVO_MAX_FOCUS_PENALTY;
                catchScore *= 1 + opportunismPenalty * (1 + focusPenalty);
                accidentalCatchScore *= 1 + opportunismPenalty * (1 + focusPenalty);
            }

            return catchScore;
        }

        private bool TryGetPredatorCapabilities(Species predatorSpecies, out PredatorCapabilities capabilities)
        {
            // First values necessary to check whether predation is possible at all
            PredationToolsRawScores predatorToolScores;
            var canEngulf = false;

            if (predatorSpecies is MicrobeSpecies microbeSpecies)
            {
                predatorToolScores = GetPredationToolsRawScores(microbeSpecies);
                canEngulf = microbeSpecies.CanEngulf;
            }
            else if (predatorSpecies is MulticellularSpecies multicellularSpecies)
            {
                predatorToolScores = GetPredationToolsRawScores(multicellularSpecies);
                var cellTypes = multicellularSpecies.CellTypes;
                for (var i = 0; i < cellTypes.Count; ++i)
                {
                    var cellType = cellTypes[i];
                    if (canEngulf)
                        break;

                    if (cellType.MembraneType.CanEngulf)
                    {
                        foreach (var hex in multicellularSpecies.EditorCells)
                        {
                            var cell = hex.Data;
                            if (cell != null && ReferenceEquals(cell.CellType, cellType))
                            {
                                canEngulf = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                capabilities = default;
                return false;
            }

            capabilities = new PredatorCapabilities(predatorToolScores, canEngulf);
            return true;
        }

        private bool TryCollectPreyPredationData(Species preySpecies, float membraneRigidityHitpointsModifier,
            out PreyPredationData data)
        {
            float smallestPreyHexSize;
            var dissolverEnzyme = Constants.LIPASE_ENZYME;

            var preyHP = 1.0f;
            float preyToxinResistance;
            float preyPhysicalResistance;
            float preyStorageNominal;

            PredationToolsRawScores preyToolScores;
            var preyHasSignallingAgent = false;
            var preyOxygenUsingOrganellesCount = 0.0f;

            var preyHexSize = owner.GetBaseHexSizeForSpecies(preySpecies);
            if (preySpecies is MicrobeSpecies microbePrey)
            {
                preyToolScores = GetPredationToolsRawScores(microbePrey);
                smallestPreyHexSize = preyHexSize;
                dissolverEnzyme = microbePrey.MembraneType.DissolverEnzyme;
                preyStorageNominal = microbePrey.StorageCapacities.Nominal;

                // uses an HP estimate without taking into account environmental tolerance effect
                preyHP = microbePrey.MembraneType.Hitpoints + microbePrey.MembraneRigidity *
                    membraneRigidityHitpointsModifier;

                // Give damage resistance if you have a nucleus (50 % general damage resistance)
                if (!microbePrey.IsBacteria)
                    preyHP *= 2;
                preyToxinResistance = microbePrey.MembraneType.ToxinResistance;
                preyPhysicalResistance = microbePrey.MembraneType.PhysicalResistance;

                var preyOrganelles = microbePrey.Organelles.Organelles;
                var preyOrganellesCount = preyOrganelles.Count;
                for (int i = 0; i < preyOrganellesCount; ++i)
                {
                    var organelle = preyOrganelles[i];
                    if (organelle.Definition.HasSignalingFeature)
                        preyHasSignallingAgent = true;
                    if (preyOrganelles[i].Definition.IsOxygenMetabolism)
                        ++preyOxygenUsingOrganellesCount;
                }
            }
            else if (preySpecies is MulticellularSpecies multicellularPrey)
            {
                preyToolScores = GetPredationToolsRawScores(multicellularPrey);
                smallestPreyHexSize = preyHexSize;
                preyStorageNominal = multicellularPrey.StorageCapacities.Nominal;

                var totalToxinResistance = 0.0f;
                var totalPhysicalResistance = 0.0f;
                var cells = multicellularPrey.EditorCells;
                var totalCellCount = cells.Count;

                var cellTypes = multicellularPrey.CellTypes;
                for (var i = 0; i < cellTypes.Count; ++i)
                {
                    var cellType = cellTypes[i];

                    var cellCount = 0;
                    foreach (var hex in multicellularPrey.EditorCells)
                    {
                        var cell = hex.Data;
                        if (cell != null && ReferenceEquals(cell.CellType, cellType))
                        {
                            ++cellCount;
                        }
                    }

                    if (cellCount == 0)
                        continue;

                    var cellTypeHP = cellType.MembraneType.Hitpoints + cellType.MembraneRigidity *
                        membraneRigidityHitpointsModifier;
                    if (!cellType.IsBacteria)
                        cellTypeHP *= 2;

                    preyHP += cellCount * cellTypeHP;

                    // for simplicity's sake we are for now taking the smallest size cell in the body
                    var cellTypeSize = owner.GetBaseHexSizeForCellType(cellType);
                    if (cellTypeSize < smallestPreyHexSize)
                    {
                        smallestPreyHexSize = cellTypeSize;
                        dissolverEnzyme = cellType.MembraneType.DissolverEnzyme;
                    }

                    totalToxinResistance += cellCount * cellType.MembraneType.ToxinResistance;
                    totalPhysicalResistance += cellCount * cellType.MembraneType.PhysicalResistance;

                    var cellTypeOxygenUsingOrganellesCount = 0;
                    foreach (var organelle in cellType.Organelles)
                    {
                        if (organelle.Definition.HasSignalingFeature)
                            preyHasSignallingAgent = true;
                        if (organelle.Definition.IsOxygenMetabolism)
                            ++cellTypeOxygenUsingOrganellesCount;
                    }

                    preyOxygenUsingOrganellesCount += cellTypeOxygenUsingOrganellesCount * cellCount;
                }

                preyToxinResistance = totalToxinResistance / totalCellCount;
                preyPhysicalResistance = totalPhysicalResistance / totalCellCount;

                preyOxygenUsingOrganellesCount /= totalCellCount;
            }
            else
            {
                data = default;
                return false;
            }

            data = new PreyPredationData(preyToolScores, preyHexSize, smallestPreyHexSize, dissolverEnzyme, preyHP,
                preyToxinResistance, preyPhysicalResistance, preyStorageNominal, preyHasSignallingAgent,
                preyOxygenUsingOrganellesCount);
            return true;
        }

        private bool TryCollectPredatorPredationData(Species predatorSpecies, Species preySpecies,
            float membraneRigidityHitpointsModifier, bool canEngulf, in PreyPredationData preyData,
            out PredatorPredationData data)
        {
            var predatorHP = 1.0f;
            float predatorToxinResistance;
            float predatorPhysicalResistance;
            float predatorStorageNominal;

            var hasChemoreceptor = false;
            var hasSignallingAgent = false;
            var predatorOxygenUsingOrganellesCount = 0.0f;
            var enzymesScore = 0.0f;

            var predatorHexSize = owner.GetBaseHexSizeForSpecies(predatorSpecies);
            if (predatorSpecies is MicrobeSpecies microbePredator)
            {
                // TODO: If these two methods were combined it might result in better performance with needing just
                // one dictionary lookup
                predatorHP = microbePredator.MembraneType.Hitpoints + microbePredator.MembraneRigidity *
                    membraneRigidityHitpointsModifier;

                // Give damage resistance if you have a nucleus (50 % general damage resistance)
                if (!microbePredator.IsBacteria)
                    predatorHP *= 2;
                predatorToxinResistance = microbePredator.MembraneType.ToxinResistance;
                predatorPhysicalResistance = microbePredator.MembraneType.PhysicalResistance;

                predatorStorageNominal = microbePredator.StorageCapacities.Nominal;

                var organelles = microbePredator.Organelles.Organelles;
                int count = organelles.Count;
                for (int i = 0; i < count; ++i)
                {
                    var organelle = organelles[i];
                    if (organelle.Definition.HasChemoreceptorComponent &&
                        organelle.GetActiveTargetSpecies() == preySpecies)
                        hasChemoreceptor = true;
                    if (organelle.Definition.HasSignalingFeature)
                        hasSignallingAgent = true;
                    if (organelles[i].Definition.IsOxygenMetabolism)
                        ++predatorOxygenUsingOrganellesCount;
                }

                if (canEngulf && predatorHexSize / preyData.SmallestHexSize > Constants.ENGULF_SIZE_RATIO_REQ)
                {
                    enzymesScore = owner.GetEnzymesScore(microbePredator, preyData.DissolverEnzyme,
                        microbePredator.CellTypeSpecializationBonus);
                }
            }
            else if (predatorSpecies is MulticellularSpecies multicellularPredator)
            {
                predatorStorageNominal = multicellularPredator.StorageCapacities.Nominal;

                var totalToxinResistance = 0.0f;
                var totalPhysicalResistance = 0.0f;
                var cells = multicellularPredator.EditorCells;
                var totalCellCount = cells.Count;

                var cellTypes = multicellularPredator.CellTypes;
                for (var i = 0; i < cellTypes.Count; ++i)
                {
                    var cellType = cellTypes[i];

                    var cellCount = 0;
                    var cellTypeHexSize = owner.GetBaseHexSizeForCellType(cellType);

                    var cellTypeSpecializationBonus = cellType.CellTypeSpecializationBonus;

                    foreach (var hex in cells)
                    {
                        var cell = hex.Data;
                        if (cell != null && ReferenceEquals(cell.CellType, cellType))
                        {
                            ++cellCount;
                            if (cellType.MembraneType.CanEngulf &&
                                cellTypeHexSize / preyData.SmallestHexSize >= Constants.ENGULF_SIZE_RATIO_REQ)
                            {
                                var cellEnzymesScore = owner.GetEnzymesScore(cellType, preyData.DissolverEnzyme,
                                    cellTypeSpecializationBonus * CellBodyPlanInternalCalculations
                                        .GetAdjacencySpecializationBonusFromBodyPlan(cell, cells));
                                enzymesScore = Math.Max(cellEnzymesScore, enzymesScore);
                            }
                        }
                    }

                    if (cellCount == 0)
                        continue;

                    var cellTypeHP = cellType.MembraneType.Hitpoints + cellType.MembraneRigidity *
                        membraneRigidityHitpointsModifier;
                    if (!cellType.IsBacteria)
                        cellTypeHP *= 2;

                    predatorHP += cellCount * cellTypeHP;
                    totalToxinResistance += cellCount * cellType.MembraneType.ToxinResistance;
                    totalPhysicalResistance += cellCount * cellType.MembraneType.PhysicalResistance;

                    var cellTypeOxygenUsingOrganellesCount = 0;
                    foreach (var organelle in cellType.Organelles)
                    {
                        if (organelle.Definition.HasChemoreceptorComponent &&
                            organelle.GetActiveTargetSpecies() == preySpecies)
                            hasChemoreceptor = true;
                        if (organelle.Definition.HasSignalingFeature)
                            hasSignallingAgent = true;
                        if (organelle.Definition.IsOxygenMetabolism)
                            ++cellTypeOxygenUsingOrganellesCount;
                    }

                    predatorOxygenUsingOrganellesCount += cellTypeOxygenUsingOrganellesCount * cellCount;
                }

                predatorToxinResistance = totalToxinResistance / totalCellCount;
                predatorPhysicalResistance = totalPhysicalResistance / totalCellCount;

                predatorOxygenUsingOrganellesCount /= totalCellCount;
            }
            else
            {
                data = default;
                return false;
            }

            data = new PredatorPredationData(predatorHexSize, predatorHP, predatorToxinResistance,
                predatorPhysicalResistance, predatorStorageNominal, hasChemoreceptor, hasSignallingAgent,
                predatorOxygenUsingOrganellesCount, enzymesScore);
            return true;
        }

        /// <summary>
        ///   Calculates cos of the angle between the organelle and vertical axis
        /// </summary>
        private float CalculateAngleMultiplier(Hex pos, bool front)
        {
            // Slime jets are biased to go backwards at position (0,0)
            if (pos.R == 0 && pos.Q == 0)
                return 1;

            Vector3 organellePosition = Hex.AxialToCartesian(pos);
            Vector3 downVector = front ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
            float angleCos = organellePosition.Normalized().Dot(downVector);

            // If degrees are higher than 40, then return 0
            return angleCos >= 0.75 ? angleCos : 0;
        }

        private readonly record struct ToxinPresence(bool HasOxytoxy,
            bool HasCytotoxin,
            bool HasMacrolide,
            bool HasChannelInhibitor,
            bool HasOxygenMetabolismInhibitor);

        private readonly record struct ToxinToolScores(float Oxytoxy,
            float Cytotoxin,
            float Macrolide,
            float ChannelInhibitor,
            float OxygenMetabolismInhibitor);

        private readonly record struct PilusToolCounts(float Pilus,
            float Injectisome,
            float DefensivePilus,
            float DefensiveInjectisome);

        private readonly record struct PilusToolScores(float Pilus,
            float Injectisome,
            float DefensivePilus,
            float DefensiveInjectisome);

        private readonly record struct PredatorCapabilities(PredationToolsRawScores ToolScores, bool CanEngulf);

        private readonly record struct PreyPredationData(PredationToolsRawScores ToolScores,
            float HexSize,
            float SmallestHexSize,
            string DissolverEnzyme,
            float Hitpoints,
            float ToxinResistance,
            float PhysicalResistance,
            float StorageNominal,
            bool HasSignallingAgent,
            float OxygenUsingOrganellesCount);

        private readonly record struct PredatorPredationData(float HexSize,
            float Hitpoints,
            float ToxinResistance,
            float PhysicalResistance,
            float StorageNominal,
            bool HasChemoreceptor,
            bool HasSignallingAgent,
            float OxygenUsingOrganellesCount,
            float EnzymesScore);
    }
}
