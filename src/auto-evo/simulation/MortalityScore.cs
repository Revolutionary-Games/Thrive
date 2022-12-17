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

            var predatorSpeed = simulationCache.GetBaseSpeedForSpecies(predator);
            predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(predator, patch.Biome).FinalBalance;
            
            int preadatorPilusCount = 0;
            float preadatorOxytoxyCount = 0.0f;
            float preadatorMucilageCount = 0.0f;
            foreach (var organelle in predator.Organelles)
            {
                if (organelle.Definition.HasPilusComponent)
                {
                    preadatorPilusCount += 1;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.TryGetValue(oxytoxy, out var oxytoxyAmount))
                    {
                        preadatorOxytoxyCount += oxytoxyAmount;
                    }

                    if (process.Process.Outputs.TryGetValue(mucilage, out var mucilageAmount))
                    {
                        preadatorMucilageCount += mucilageAmount * 5;
                    }
                }
            }

            int pilusCount = 0;
            float oxytoxyCount = 0.0f;
            float mucilageCount = 0.0f;
            foreach (var organelle in predator.Organelles)
            {
                if (organelle.Definition.HasPilusComponent)
                {
                    pilusCount += 1;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.TryGetValue(oxytoxy, out var oxytoxyAmount))
                    {
                        oxytoxyCount += oxytoxyAmount;
                    }

                    if (process.Process.Outputs.TryGetValue(mucilage, out var mucilageAmount))
                    {
                        mucilageCount += mucilageAmount;
                    }
                }
            }

            if(mucilageCount > 0 && preadatorMucilageCount == 0)
            {
                predatorSpeed /= Constants.MUCILAGE_IMPEDE_FACTOR;
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

                // predators that are slower than you can't catch you
                if (predatorSpeed < currentSpeciesSpeed)
                    engulfScore *= Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;
                
                // It's hard to engulf cells pilus 
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
                pilusScore += preadatorPilusCount * Constants.AUTO_EVO_PILUS_MORTALITY_SCORE;

                // ...But you can stab back
                pilusScore -= pilusCount * Constants.AUTO_EVO_PILUS_MORTALITY_SCORE;

                // predators that are slower than you can't catch you
                if (predatorSpeed < currentSpeciesSpeed)
                    engulfScore *= Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

                // Keep pilusScore positive
                pilusScore = Mathf.Max(pilusScore, 0);

                // You can fight back with toxins
                if(oxytoxyCount > 0)
                    pilusScore *= (1 - Constants.AUTO_EVO_TOXIN_PILUS_PENALTY);

                // Pilus are worse against cells with resistances
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
                    engulfScore *= (1 - Constants.AUTO_EVO_SPEED_TOXIN_PENALTY);

                // Toxin and Pilus are worse against cells with resistances
                // and better when they can exploit weaknesses
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