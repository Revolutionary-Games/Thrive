namespace AutoEvo
{
    /// <summary>
    ///   A class for random-encounter based food sources.
    /// </summary>
    public abstract class RandomFoodSource : FoodSource
    {
        protected override float StorageScore(MicrobeSpecies species, Compound compound, Patch patch,
            SimulationCache simulationCache)
        {
            // TODO compute value based on encouter chance ; this is a temporary solution for day/night cycle PR.
            return 1.0f;
        }
    }
}
