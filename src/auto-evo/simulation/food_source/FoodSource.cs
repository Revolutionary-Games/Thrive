namespace AutoEvo
{
    using System;

    public abstract class FoodSource
    {
        public abstract float TotalEnergyAvailable();

        /// <summary>
        ///   Provides a fitness metric to determine population adjustments for species in a patch.
        /// </summary>
        /// <param name="microbe">The species to be evaluated.</param>
        /// <param name="simulationCache">
        ///   Cache that should be used to reduce amount of times expensive computations are run
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
            var energyGenerationScore = simulationCache.GetEnergyGenerationScoreForSpecies(species, patch.Biome, compound);

            if (energyGenerationScore <= MathUtils.EPSILON)
                return 0.0f;

            return energyGenerationScore * StorageScore(
                species, compound, patch, simulationCache, worldSettings);
        }
    }
}
