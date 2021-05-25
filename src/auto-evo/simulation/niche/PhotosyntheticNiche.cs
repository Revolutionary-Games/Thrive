namespace Thrive.src.auto_evo.simulation.niche
{
    using System;

    public class PhotosyntheticNiche : Niche
    {
        private static readonly Compound Sunlight = SimulationParameters.Instance.GetCompound("sunlight");
        private static readonly Compound Glucose = SimulationParameters.Instance.GetCompound("glucose");
        private static readonly Compound ATP = SimulationParameters.Instance.GetCompound("atp");

        private float totalSunlight;

        public PhotosyntheticNiche(Patch patch)
        {
            totalSunlight = patch.Biome.Compounds[Sunlight].Dissolved * Constants.AUTO_EVO_SUNLIGHT_ENERGY_AMOUNT;
        }

        public float FitnessScore(Species species)
        {
            var microbeSpecies = (MicrobeSpecies)species;

            var photosynthesisingScore = 0.0f;
            foreach (var organelle in microbeSpecies.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Inputs.ContainsKey(Sunlight))
                    {
                        if (process.Process.Outputs.ContainsKey(Glucose))
                        {
                            photosynthesisingScore += process.Process.Outputs[Glucose]
                                / process.Process.Inputs[Sunlight] / Constants.AUTO_EVO_GLUCOSE_USE_SCORE_DIVISOR;
                        }

                        if (process.Process.Outputs.ContainsKey(ATP))
                        {
                            photosynthesisingScore += process.Process.Outputs[ATP]
                                / process.Process.Inputs[Sunlight] / Constants.AUTO_EVO_ATP_USE_SCORE_DIVISOR;
                        }
                    }
                }
            }

            // Moving too much can be harmfull
            var energyCost = microbeSpecies.BaseOsmoregulationCost();
            energyCost *= 1 + (microbeSpecies.Activity / Constants.MAX_SPECIES_ACTIVITY);

            return photosynthesisingScore / energyCost;
        }

        public float TotalEnergyAvailable()
        {
            return totalSunlight;
        }
    }
}
