namespace AutoEvo
{
    using System;

    public abstract class FoodSource
    {
        private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
        private readonly Compound atp = SimulationParameters.Instance.GetCompound("atp");

        public abstract float TotalEnergyAvailable();

        /// <summary>
        ///   Provides a fitness metric to determine population adjustments for species in a patch.
        /// </summary>
        /// <param name="microbe">The species to be evaluated.</param>
        /// <param name="simulationCache">
        ///   Cache that should be used to reduce amount of times expensive computations are run
        /// </param>
        /// <returns>
        ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
        ///   so different implementations do not need to worry about scale.
        /// </returns>
        public abstract float FitnessScore(Species microbe, SimulationCache simulationCache);

        /// <summary>
        ///   A description of this niche. Needs to support translations changing and be player readable
        /// </summary>
        /// <returns>A formattable that has the description in it</returns>
        public abstract IFormattable GetDescription();

        protected float EnergyGenerationScore(MicrobeSpecies species, Compound compound, Patch patch)
        {
            var energyCreationScore = 0.0f;
            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                    {
                        var processEfficiency = ProcessSystem.CalculateProcessMaximumSpeed(
                            process, patch.Biome).Efficiency;

                        if (process.Process.Outputs.TryGetValue(glucose, out var glucoseAmount))
                        {
                            energyCreationScore += glucoseAmount / inputAmount
                                * processEfficiency * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER;
                        }

                        if (process.Process.Outputs.TryGetValue(atp, out var atpAmount))
                        {
                            energyCreationScore += atpAmount / inputAmount
                                * processEfficiency * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER;
                        }
                    }
                }
            }

            return energyCreationScore;
        }
    }
}
