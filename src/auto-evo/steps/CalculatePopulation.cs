namespace AutoEvo
{
    /// <summary>
    ///   Step that calculate the populations for all species
    /// </summary>
    public class CalculatePopulation : IRunStep
    {
        private PatchMap map;

        public CalculatePopulation(PatchMap map)
        {
            this.map = map;
        }

        public int TotalSteps
        {
            get
            {
                return 1;
            }
        }

        public bool Step(RunResults results)
        {
            var config = new SimulationConfiguration(map, 1);

            // Directly feed the population results to the main results object
            config.Results = results;

            PopulationSimulation.Simulate(config);

            return true;
        }
    }
}
