namespace Thrive.src.auto_evo.simulation.niche
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
            totalEnergy = patch.SpeciesInPatch[prey] * prey.BaseOsmoregulationCost() * Constants.AUTO_EVO_PREDATION_ENERGY_MULTIPLIER;
        }

        public float FitnessScore(MicrobeSpecies species)
        {
            var predatorSize = species.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);
            var preySize = species.Organelles.Organelles.Sum(organelle => organelle.Definition.HexCount);

            var sizeScore = predatorSize / preySize > Constants.ENGULF_SIZE_RATIO_REQ ? Constants.AUTO_EVO_ENGULF_PREDATION_SCORE : 0.0f;

            var pilusScore = 0.0f;
            var oxytoxyScore = 0.0f;

            // TODO: replace this with a more accurate speed calculation
            var totalSpeedBonuses = 0.0f;
            foreach (var organelle in species.Organelles)
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

                totalSpeedBonuses += organelle.Definition.Components.Movement.Momentum;
            }

            pilusScore *= totalSpeedBonuses + 0.5f;

            var energyCost = species.BaseOsmoregulationCost();

            return pilusScore + sizeScore + oxytoxyScore + totalSpeedBonuses;
        }

        public float TotalEnergyAvailable()
        {
            return totalEnergy;
        }
    }
}
