namespace AutoEvo
{
    /// <summary>
    ///   Step that calculate the populations for all species
    /// </summary>
    public class CalculatePopulation : IRunStep
    {
        private readonly GameWorld world;

        public CalculatePopulation(GameWorld world)
        {
            this.world = world;
        }

        public int TotalSteps => 1;

        public bool RunStep(RunResults results)
        {
            // ReSharper disable RedundantArgumentDefaultValue
            var config = new SimulationConfiguration(world, 1) { Results = results };

            // ReSharper restore RedundantArgumentDefaultValue

            // Directly feed the population results to the main results object

            PopulationSimulation.Simulate(config);

            return true;
        }
    }
}
