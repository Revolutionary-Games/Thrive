namespace AutoEvo
{
    using System;
    using Godot;

    public class HeterotrophicFoodSource : RandomEncounterFoodSource
    {
        private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private readonly Compound mucilage = SimulationParameters.Instance.GetCompound("mucilage");

        public readonly MicrobeSpecies prey;
        private readonly Patch patch;
        private readonly float preyHexSize;
        private readonly float preySpeed;
        private readonly float totalEnergy;

        public HeterotrophicFoodSource(Patch patch, MicrobeSpecies prey, SimulationCache simulationCache)
        {
            this.prey = prey;
            this.patch = patch;
            preyHexSize = simulationCache.GetBaseHexSizeForSpecies(prey);
            preySpeed = simulationCache.GetBaseSpeedForSpecies(prey);
            patch.SpeciesInPatch.TryGetValue(prey, out long population);
            totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
        }

        public override float FitnessScore(Species species, SimulationCache simulationCache,
            WorldGenerationSettings worldSettings)
        {
            var currentSpecies = (MicrobeSpecies)species;

            // No cannibalism
            if (currentSpecies == prey)
            {
                return 0.0f;
            }

            var behaviourScore = currentSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

            // TODO: if these two methods were combined it might result in better performance with needing just
            // one dictionary lookup
            var microbeSpeciesHexSize = simulationCache.GetBaseHexSizeForSpecies(currentSpecies);
            var predatorSpeed = simulationCache.GetBaseSpeedForSpecies(currentSpecies);

            predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(currentSpecies, patch.Biome).FinalBalance;

            var currentSpeciesOrganelleData = simulationCache.GetOrganelleData(currentSpecies);
            int pilusCount = currentSpeciesOrganelleData.PilusCount;
            float oxytoxyCount = currentSpeciesOrganelleData.OxytoxyCount;
            float mucilageCount = currentSpeciesOrganelleData.MucilageCount;

            var tempPreySpeed = preySpeed;
            tempPreySpeed += simulationCache.GetEnergyBalanceForSpecies(prey, patch.Biome).FinalBalance;

            var preyOrganelleData = simulationCache.GetOrganelleData(prey);
            int preyPilusCount = preyOrganelleData.PilusCount;
            float preyOxytoxyCount = preyOrganelleData.OxytoxyCount;
            float preyMucilageCount = preyOrganelleData.MucilageCount;

            // TODO: Properly account for Mucilage Speed Boost
            if(mucilageCount > 0 && preyMucilageCount == 0)
            {
                tempPreySpeed /= Constants.MUCILAGE_IMPEDE_FACTOR * Constants.AUTO_EVO_PREDATOR_MUCILAGE_ENSNARE_RATE;
            }
            else if(preyMucilageCount > 0 && mucilageCount == 0)
            {
                predatorSpeed /= Constants.MUCILAGE_IMPEDE_FACTOR;
            }

            // TODO: Take into account Enzymes properly
            bool canEngulf = microbeSpeciesHexSize / preyHexSize > Constants.ENGULF_SIZE_RATIO_REQ && 
                !currentSpecies.MembraneType.CellWall &&
                prey.MembraneType.DissolverEnzyme == "lipase";

            // Only assign engulf score if one can actually engulf
            var engulfScore = 0.0f;
            if (canEngulf)
            {
                // First, you may hunt individual preys, but only if you are fast enough...
                if (predatorSpeed > preySpeed)
                {
                    // You catch more preys if you are fast, and if they are slow.
                    // This incentivizes engulfment strategies in these cases.
                    engulfScore += predatorSpeed / preySpeed;
                }

                // ... but you may also catch them by luck (e.g. when they run into you),
                // and this is especially easy if you're huge.
                // This is also used to incentivize size in microbe species.
                engulfScore += Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY * microbeSpeciesHexSize;

                // It's hard to engulf microbes with pili 
                if(preyPilusCount > 0)
                    engulfScore *= Mathf.Pow((1 - Constants.AUTO_EVO_PILUS_ENGULF_PENALTY), preyPilusCount);

                // Engulfing a toxic microbe hurts
                if(preyOxytoxyCount > 0)
                    engulfScore *= (1 - Constants.AUTO_EVO_TOXIN_ENGULF_PENALTY);

                engulfScore *= Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;
            }


            float pilusScore = 0.0f;
            if(pilusCount > 0)
            {
                // You can stab them...
                // Having more Pili brings diminshing returns
                pilusScore += (Mathf.Pow(pilusCount+1, 1-Constants.AUTO_EVO_PILI_DIMINISHMENT) - 1) / 
                    (1 - Constants.AUTO_EVO_PILI_DIMINISHMENT);

                // ...But they can stab back
                if(preyPilusCount > 0)
                    pilusScore *= Mathf.Pow((1 - Constants.AUTO_EVO_PILUS_PILUS_PENALTY), preyPilusCount);

                // Pili are better the faster you are
                pilusScore *= predatorSpeed > preySpeed ? (predatorSpeed / preySpeed) : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

                // Prey can fight back with toxins
                if(oxytoxyCount > 0)
                    pilusScore *= (1 - Constants.AUTO_EVO_TOXIN_PILUS_PENALTY);

                // Pilus are worse against cells with resistances
                // and better when they can exploit weaknesses
                pilusScore /= prey.MembraneType.PhysicalResistance;

                pilusScore *= Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
            }

            float oxytoxyScore = 0.0f;
            if(oxytoxyCount > 0)
            {
                oxytoxyScore += oxytoxyCount;

                // predators are less likely to use toxin against larger prey, unless they are opportunistic
                if (preyHexSize > microbeSpeciesHexSize)
                    oxytoxyScore *= currentSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
                
                // It's harder to hit fast creatures
                if (predatorSpeed < tempPreySpeed)
                    oxytoxyScore *= (1 - Constants.AUTO_EVO_SPEED_TOXIN_PENALTY);

                // Toxin is worse against cells with resistances
                // and better when it can exploit weaknesses
                oxytoxyScore /= prey.MembraneType.ToxinResistance;

                oxytoxyScore *= Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
            }

            return behaviourScore * (pilusScore + engulfScore + oxytoxyScore);
        }

        public override IFormattable GetDescription()
        {
            return new LocalizedString("PREDATION_FOOD_SOURCE", prey.FormattedNameBbCode);
        }

        public override float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
