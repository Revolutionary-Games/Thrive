namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    /// <summary>
    /// </summary>
    public static class SurvivabilityScore
    {
        private static readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private static readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");

        /// <summary>
        /// </summary>
        /// <remarks>
        ///   Lower is better here. 
        /// </remarks>
        public static float Score(MicrobeSpecies currentSpecies, MicrobeSpecies predator, SimulationCache simulationCache, Patch patch)
        {
            
            // No cannibalism
            if (currentSpecies == predator)
            {
                return 0.0f;
            }

            var currentSpeciesSpeed = simulationCache.GetBaseSpeedForSpecies(currentSpecies);
            currentSpeciesSpeed += simulationCache.GetEnergyBalanceForSpecies(currentSpecies, patch.Biome).FinalBalance;

            var predatorSpeed = simulationCache.GetBaseSpeedForSpecies(predator);
            predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(predator, patch.Biome).FinalBalance;

            // Catch scores accounts for how many times you get cought
            var catchScore = 0.0f;
            if (predator.BaseHexSize / currentSpecies.BaseHexSize >
                Constants.ENGULF_SIZE_RATIO_REQ && !predator.MembraneType.CellWall &&
                currentSpecies.MembraneType.DissolverEnzyme == "lipase") // TODO: Take into account Enzymes properly
            {
                // predators that are faster than you will catch you
                if (predatorSpeed > currentSpeciesSpeed)
                {
                    catchScore += (predatorSpeed / currentSpeciesSpeed) * 100;
                }
            }

            var pilusScore = 0.0f;
            var oxytoxyScore = 0.0f;
            var mucilageScore = 0.0f;
            foreach (var organelle in predator.Organelles)
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

            // Pili are much more useful if the microbe can close to melee
            // Pili are better the faster you are as well
            pilusScore *= predatorSpeed > currentSpeciesSpeed ? (predatorSpeed / currentSpeciesSpeed) : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;


            // predators are less likely to use toxin against larger prey, unless they are opportunistic
            if (currentSpecies.BaseHexSize > predator.BaseHexSize)
            {
                oxytoxyScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            }

            // Toxin and Pilus are worse against cells with resistances
            // and better when they can exploit weaknesses
            oxytoxyScore /= currentSpecies.MembraneType.ToxinResistance;
            pilusScore /= currentSpecies.MembraneType.PhysicalResistance;

            var pilusDefenseScore = 0.0f;
            var mucilageDefenseScore = 0.0f;
            var predatorMucilage = mucilageScore > 0;
            foreach (var organelle in predator.Organelles)
            {
                if (organelle.Definition.HasPilusComponent)
                {
                    // Pilus make it harder to eat you
                    pilusDefenseScore += Constants.AUTO_EVO_PILUS_DEFENSE_SCORE;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.TryGetValue(mucilage, out var mucilageAmount))
                    {
                        if(predatorMucilage)
                        {
                            mucilageScore = 0;
                        }
                        else
                        {
                            mucilageDefenseScore += mucilageAmount * Constants.AUTO_EVO_MUCILAGE_DEFENSE_SCORE;
                        }
                    }
                }
            }

            var speedBonus = currentSpeciesSpeed > predatorSpeed ? 
                (currentSpeciesSpeed / predatorSpeed) * Constants.AUTO_EVO_SPEED_DEFENSE_BONUS : 0;

            float predatorScore = (pilusScore + catchScore + oxytoxyScore + mucilageScore);
            float defenseScore = (pilusDefenseScore + speedBonus + mucilageDefenseScore);

            // Lower is better here. This represents how many microbes got munched on.
            return Constants.AUTO_EVO_SURVIVABILITY_SCORE_MULTIPLIER * Mathf.Max(predatorScore - defenseScore, 0);
        }
    }
}