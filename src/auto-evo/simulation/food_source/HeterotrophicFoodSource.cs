﻿namespace AutoEvo
{
    using System;

    public class HeterotrophicFoodSource : FoodSource
    {
        private readonly Compound oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");

        private readonly MicrobeSpecies prey;
        private readonly Patch patch;
        private readonly float preyHexSize;
        private readonly float preySpeed;
        private readonly float totalEnergy;

        public HeterotrophicFoodSource(Patch patch, MicrobeSpecies prey)
        {
            this.prey = prey;
            this.patch = patch;
            preyHexSize = prey.BaseHexSize;
            preySpeed = prey.BaseSpeed;
            patch.SpeciesInPatch.TryGetValue(prey, out long population);
            totalEnergy = population * prey.Organelles.Count * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
        }

        public override float FitnessScore(Species species, SimulationCache simulationCache)
        {
            var microbeSpecies = (MicrobeSpecies)species;

            // No cannibalism
            if (microbeSpecies == prey)
            {
                return 0.0f;
            }

            var behaviourScore = microbeSpecies.Behaviour.Aggression / Constants.MAX_SPECIES_AGGRESSION;

            var microbeSpeciesHexSize = microbeSpecies.BaseHexSize;
            var predatorSpeed = microbeSpecies.BaseSpeed;
            predatorSpeed += simulationCache.GetEnergyBalanceForSpecies(microbeSpecies, patch).FinalBalance;

            // It's great if you can engulf this prey, but only if you can catch it
            var engulfScore = 0.0f;
            if (microbeSpeciesHexSize / preyHexSize >
                Constants.ENGULF_SIZE_RATIO_REQ && !microbeSpecies.MembraneType.CellWall)
            {
                engulfScore = Constants.AUTO_EVO_ENGULF_PREDATION_SCORE;
            }

            engulfScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

            var pilusScore = 0.0f;
            var oxytoxyScore = 0.0f;
            foreach (var organelle in microbeSpecies.Organelles)
            {
                if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
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
                }
            }

            // Pili are much more useful if the microbe can close to melee
            pilusScore *= predatorSpeed > preySpeed ? 1.0f : Constants.AUTO_EVO_ENGULF_LUCKY_CATCH_PROBABILITY;

            // predators are less likely to use toxin against larger prey, unless they are opportunistic
            if (preyHexSize > microbeSpeciesHexSize)
            {
                oxytoxyScore *= microbeSpecies.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
            }

            // Intentionally don't penalize for osmoregulation cost to encourage larger monsters
            return behaviourScore * (pilusScore + engulfScore + microbeSpeciesHexSize + oxytoxyScore);
        }

        public override IFormattable GetDescription()
        {
            return new LocalizedString("PREDATION_FOOD_SOURCE", prey.FormattedName);
        }

        public override float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
