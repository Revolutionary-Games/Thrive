namespace AutoEvo
{
    using System;
    using Godot;

    /// <summary>
    /// </summary>
    public static class MortalityScore
    {
        private static readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private static readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");

        /// <summary>
        /// Calculates the mortality rate for cells
        /// </summary>
        public static float Score(MicrobeSpecies currentSpecies, MicrobeSpecies predator, SimulationCache simulationCache, Patch patch)
        {
            
            // No cannibalism
            if (currentSpecies == predator)
            {
                return 0.0f;
            }

            var currentSpeciesSpeed = simulationCache.GetBaseSpeedForSpecies(currentSpecies);
            currentSpeciesSpeed += simulationCache.GetEnergyBalanceForSpecies(currentSpecies, patch.Biome).FinalBalance;
            
            var currentSpeciesOrganelleData = simulationCache.GetOrganelleData(currentSpecies);
            int pilusCount = currentSpeciesOrganelleData.PilusCount;
            float oxytoxyCount = currentSpeciesOrganelleData.OxytoxyCount;
            float mucilageCount = currentSpeciesOrganelleData.MucilageCount;

            var predatorOrganelleData = simulationCache.GetOrganelleData(predator);
            int preadatorPilusCount = predatorOrganelleData.PilusCount;
            float preadatorOxytoxyCount = predatorOrganelleData.OxytoxyCount;
            float preadatorMucilageCount = predatorOrganelleData.MucilageCount;

            var predatorSpeed = simulationCache.GetBaseSpeedForSpecies(predator);
            predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(predator, patch.Biome).FinalBalance;
            
            // TODO: Properly account for Mucilage Speed Boost
            if(mucilageCount > 0 && preadatorMucilageCount == 0)
            {
                predatorSpeed /= Constants.MUCILAGE_IMPEDE_FACTOR;
            }
            else if(preadatorMucilageCount > 0 && mucilageCount == 0)
            {
                currentSpeciesSpeed /= Constants.MUCILAGE_IMPEDE_FACTOR;
            }

            // TODO: Take into account Enzymes properly
            bool canBeEngulfed = predator.BaseHexSize / currentSpecies.BaseHexSize > Constants.ENGULF_SIZE_RATIO_REQ && 
                !predator.MembraneType.CellWall &&
                currentSpecies.MembraneType.DissolverEnzyme == "lipase";

            // Engluf Score is what percentage of the time you get englufed
            float engulfScore = 0.0f;
            if (canBeEngulfed) 
            {
                engulfScore += Constants.AUTO_EVO_ENGULF_MORTALITY_SCORE;

                // Predators that are slower than you can't catch you
                if (predatorSpeed < currentSpeciesSpeed)
                    engulfScore *= Mathf.Min(Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY * predator.BaseHexSize, 1);
                
                // It's hard to engulf microbes with pili 
                if(pilusCount > 0)
                    engulfScore *= Mathf.Pow((1 - Constants.AUTO_EVO_PILUS_ENGULF_PENALTY), pilusCount);

                // Engulfing toxic cells hurts
                if(oxytoxyCount > 0)
                    engulfScore *= (1 - Constants.AUTO_EVO_TOXIN_ENGULF_PENALTY);
            }

            float pilusScore = 0.0f;
            if(preadatorPilusCount > 0)
            {
                // Predators can stab you...
                // Having more Pili brings diminshing returns
                pilusScore += (Mathf.Pow(pilusCount+1, 1-Constants.AUTO_EVO_PILI_DIMINISHMENT) - 1) / 
                    (1 - Constants.AUTO_EVO_PILI_DIMINISHMENT) * Constants.AUTO_EVO_PILUS_MORTALITY_SCORE;

                // ...But you can stab back
                if(pilusCount > 0)
                    pilusScore *= Mathf.Pow((1 - Constants.AUTO_EVO_PILUS_PILUS_PENALTY), pilusCount);

                // Predators that are slower than you can't catch you
                if (predatorSpeed < currentSpeciesSpeed)
                    pilusScore *= Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

                // You can fight back with toxins
                if(oxytoxyCount > 0)
                    pilusScore *= (1 - Constants.AUTO_EVO_TOXIN_PILUS_PENALTY);

                // Pili are worse against cells with resistances
                // and better when they can exploit weaknesses
                pilusScore /= currentSpecies.MembraneType.PhysicalResistance;
            }

            
            float oxytoxyScore = 0.0f;
            if(preadatorOxytoxyCount > 0)
            {
                oxytoxyScore += preadatorOxytoxyCount * Constants.AUTO_EVO_TOXIN_MORTALITY_SCORE;

                // predators are less likely to use toxin against larger prey, unless they are opportunistic
                if (currentSpecies.BaseHexSize > predator.BaseHexSize)
                    oxytoxyScore *= predator.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
                
                // It's harder to hit fast creatures
                if (predatorSpeed < currentSpeciesSpeed)
                    oxytoxyScore *= (1 - Constants.AUTO_EVO_SPEED_TOXIN_PENALTY);

                // Toxin is worse against cells with resistances
                // and better when it can exploit weaknesses
                oxytoxyScore /= currentSpecies.MembraneType.ToxinResistance;
            }

            float predatorScore = (engulfScore + pilusScore + oxytoxyScore);

            if(predatorScore < 0)
                GD.PrintErr("predatorScore below 1: " + predatorScore);

            // As a fraction, cap between 0 and 1
            return Math.Min(Math.Max(predatorScore, 0), 1);
        }
    }
}