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
        ///     Cache that should be used to reduce amount of times expensive computations are run
        /// </param>
        /// <param name="worldSettings">Player-configured settings for this game</param>
        /// <returns>
        ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
        ///   so different implementations do not need to worry about scale.
        /// </returns>
        public abstract float FitnessScore(Species microbe, SimulationCache simulationCache,
            WorldGenerationSettings worldSettings);

        /// <summary>
        ///   A description of this niche. Needs to support translations changing and be player readable
        /// </summary>
        /// <returns>A formattable that has the description in it</returns>
        public abstract IFormattable GetDescription();

        /// <summary>
        ///   A measure of how good the species is when storing food against shortages.
        /// </summary>
        /// <returns>
        ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
        ///   so different implementations do not need to worry about scale.
        /// </returns>
        protected abstract float StorageScore(MicrobeSpecies species, Compound compound, Patch patch,
            SimulationCache simulationCache, WorldGenerationSettings worldSettings);

        /// <summary>
        ///   A measure of how good the species is globally, when using a given compound for nutrition.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This method should include any phenomenon related to the compound and its use,
        ///     such as energy generation, storage...
        ///   </para>
        /// </remarks>
        /// <returns>
        ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
        ///   so different implementations do not need to worry about scale.
        /// </returns>
        protected float CompoundUseScore(MicrobeSpecies species, Compound compound, Patch patch,
            SimulationCache simulationCache, WorldGenerationSettings worldSettings)
        {
            var energyGenerationScore = EnergyGenerationScore(species, compound, patch, simulationCache);

            if (energyGenerationScore <= MathUtils.EPSILON)
                return 0.0f;

            return energyGenerationScore * StorageScore(
                species, compound, patch, simulationCache, worldSettings);
        }

        /// <summary>
        ///   A measure of how good the species is for generating energy from a given compound.
        /// </summary>
        /// <returns>
        ///   A float to represent score. Scores are only compared against other scores from the same FoodSource,
        ///   so different implementations do not need to worry about scale.
        /// </returns>
        private float EnergyGenerationScore(MicrobeSpecies species, Compound compound, Patch patch,
            SimulationCache simulationCache)
        {
            var energyCreationScore = 0.0f;

            // We check generation from all the processes of the cell..
            foreach (var organelle in species.Organelles)
            {
                foreach (var process in organelle.Definition.RunnableProcesses)
                {
                    // ... that uses the given compound...
                    if (process.Process.Inputs.TryGetValue(compound, out var inputAmount))
                    {
                        var processEfficiency = simulationCache.GetProcessMaximumSpeed(process, patch.Biome).Efficiency;

                        // ... and that produce glucose
                        if (process.Process.Outputs.TryGetValue(glucose, out var glucoseAmount))
                        {
                            // Better ratio means that we transform stuff more efficiently and need less input
                            var compoundRatio = glucoseAmount / inputAmount;

                            // Better output is a proxy for more time dedicated to reproduction than energy production
                            var absoluteOutput = glucoseAmount * processEfficiency;

                            energyCreationScore += (float)(
                                Math.Pow(compoundRatio, Constants.AUTO_EVO_COMPOUND_RATIO_POWER_BIAS)
                                * Math.Pow(absoluteOutput, Constants.AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS)
                                * Constants.AUTO_EVO_GLUCOSE_USE_SCORE_MULTIPLIER);
                        }

                        // ... and that produce ATP
                        if (process.Process.Outputs.TryGetValue(atp, out var atpAmount))
                        {
                            // Better ratio means that we transform stuff more efficiently and need less input
                            var compoundRatio = atpAmount / inputAmount;

                            // Better output is a proxy for more time dedicated to reproduction than energy production
                            var absoluteOutput = atpAmount * processEfficiency;

                            energyCreationScore += (float)(
                                Math.Pow(compoundRatio, Constants.AUTO_EVO_COMPOUND_RATIO_POWER_BIAS)
                                * Math.Pow(absoluteOutput, Constants.AUTO_EVO_ABSOLUTE_PRODUCTION_POWER_BIAS)
                                * Constants.AUTO_EVO_ATP_USE_SCORE_MULTIPLIER);
                        }
                    }
                }
            }

            return energyCreationScore;
        }
    }
}
