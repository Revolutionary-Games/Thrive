namespace Thrive.src.auto_evo.simulation.niche
{
    using System;

    public class PhotosyntheticNiche : Niche
    {
        private float totalSunlight;

        private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

        public PhotosyntheticNiche(Patch patch)
        {
            totalSunlight = patch.Biome.Compounds[SimulationParameters.Instance.GetCompound("sunlight")].Dissolved;
        }

        public float FitnessScore(MicrobeSpecies species)
        {
            var compoundUseScore = 0.0f;

            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Inputs.ContainsKey(Sunlight))
                    {
                        if (process.Process.Outputs.ContainsKey(Glucose))
                        {
                            compoundUseScore += process.Process.Outputs[Glucose]
                                / process.Process.Inputs[Sunlight] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                        }

                        if (process.Process.Outputs.ContainsKey(ATP))
                        {
                            compoundUseScore += process.Process.Outputs[ATP]
                                / process.Process.Inputs[Sunlight] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                        }
                    }
                }
            }

            var energyCost = species.BaseOsmoregulationCost();

            return compoundUseScore / energyCost;
        }

        public float TotalEnergyAvailable()
        {
            return totalSunlight;
        }
    }
}
