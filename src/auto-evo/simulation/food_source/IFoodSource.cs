public interface IFoodSource
{
    public float TotalEnergyAvailable();

    /// <summary>
    ///   Provides a fitness metric to determine population adjustments for species in a patch.
    /// </summary>
    /// <param name="microbe">The species to be evaluated.</param>
    /// <returns>A float to represent score. Scores are only compared against other scores from the same FoodSource,
    /// so different implementations do not need to worry about scale.</returns>
    public float FitnessScore(Species microbe);
}
