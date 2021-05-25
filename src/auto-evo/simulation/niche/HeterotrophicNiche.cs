namespace Thrive.src.auto_evo.simulation.niche
{
    using System;
    using System.Linq;

    public class HeterotrophicNiche : Niche
    {
        private static readonly Compound Oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

        private MicrobeSpecies prey;
        float preySpeed;
        private float totalEnergy;

        public HeterotrophicNiche(Patch patch, MicrobeSpecies prey)
        {
            this.prey = prey;
            preySpeed = prey.BaseSpeed();
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
            var predatorSpeed = microbeSpecies.BaseSpeed();
            var preySize = microbeSpecies.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);

            var engulfScore = predatorSize / preySize > Constants.ENGULF_SIZE_RATIO_REQ ? Constants.AUTO_EVO_ENGULF_PREDATION_SCORE : 0.0f;
            engulfScore *= predatorSpeed > preySpeed ? 1.0f : 0.1f;

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
                    if (process.Process.Outputs.ContainsKey(Oxytoxy))
                    {
                        oxytoxyScore += Constants.AUTO_EVO_TOXIN_PREDATION_SCORE;
                    }
                }
            }

            pilusScore *= predatorSpeed;

            // Intentionally don't penalize for osmoregulation cost to encourage monsters
            return pilusScore + engulfScore + predatorSize + oxytoxyScore;
        }

        public float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
