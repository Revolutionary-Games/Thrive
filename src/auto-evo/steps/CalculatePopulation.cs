namespace AutoEvo
{
    /// <summary>
    ///   Step that calculate the populations for all species
    /// </summary>
    public class CalculatePopulation : IRunStep
    {
        private readonly PatchMap map;

        public CalculatePopulation(PatchMap map)
        {
            this.map = map;
        }

        public int TotalSteps => 1;

        public bool CanRunConcurrently { get; set; } = true;

        public bool RunStep(RunResults results)
        {
            // ReSharper disable RedundantArgumentDefaultValue
            var config = new SimulationConfiguration(map, 1) { Results = results };

            // ReSharper restore RedundantArgumentDefaultValue

            // Directly feed the population results to the main results object

            PopulationSimulation.Simulate(config);

            return true;
        }
    }
}
