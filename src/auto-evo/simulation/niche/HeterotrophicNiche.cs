﻿namespace Thrive.src.auto_evo.simulation.niche
{
    using System;
    using System.Linq;

    public class HeterotrophicNiche : Niche
    {
        private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

        private MicrobeSpecies prey;
        private float totalEnergy;

        public HeterotrophicNiche(Patch patch, MicrobeSpecies prey)
        {
            this.prey = prey;
            patch.SpeciesInPatch.TryGetValue(prey, out long population);
            totalEnergy = population * prey.BaseOsmoregulationCost() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
        }

        public float FitnessScore(Species species)
        {
            var microbeSpecies = (MicrobeSpecies)species;

            // No canibalism
            if (species == prey)
            {
                return 0.0f;
            }

            var predatorSize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
            var preySize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);

            var engulfScore = predatorSize / preySize > Constants.ENGULF_SIZE_RATIO_REQ ? Constants.AUTO_EVO_ENGULF_PREDATION_SCORE : 0.0f;
            engulfScore *= microbeSpecies.BaseSpeed() / prey.BaseSpeed();

            var pilusScore = 0.0f;
            var oxytoxyScore = 0.0f;

            // TODO: replace this with a more accurate speed calculation
            var speedFactor = 1.0f;
            foreach (var organelle in microbeSpecies.Organelles)
            {
                if (organelle.Definition.HasComponentFactory<PilusComponentFactory>())
                {
                    pilusScore += Constants.AUTO_EVO_PILUS_PREDATION_SCORE;
                    continue;
                }

                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Outputs.ContainsKey(Oxytoxy))
                    {
                        oxytoxyScore += Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                    }
                }

                if (organelle.Definition.Components?.Movement?.Momentum != null)
                {
                    speedFactor += organelle.Definition.Components.Movement.Momentum;
                }
            }

            pilusScore *= speedFactor + 0.5f;

            // Intentionally don't penalize for osmoregulation cost to get encourage monsters
            return pilusScore + engulfScore + predatorSize + oxytoxyScore + speedFactor;
        }

        public float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
