namespace AutoEvo
{
    /// <summary>
    ///   Step that finds the best mutation for a single species
    /// </summary>
    public class FindBestMutation : VariantTryingStep
    {
        private readonly GameWorld world;
        private readonly Species species;

        private readonly Mutations mutations = new Mutations();

        public FindBestMutation(GameWorld world, Species species, int mutationsToTry, bool allowNoMutation)
            : base(mutationsToTry, allowNoMutation)
        {
            this.world = world;
            this.species = species;
        }

        public override bool CanRunConcurrently => true;

        protected override void OnBestResultFound(RunResults results, IAttemptResult bestVariant)
        {
            results.AddMutationResultForSpecies(species, ((AttemptResult)bestVariant).Mutation);
        }

        protected override IAttemptResult TryCurrentVariant()
        {
            var config = new SimulationConfiguration(world, Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

            PopulationSimulation.Simulate(config);

            var population = config.Results.GetGlobalPopulation(species);

            return new AttemptResult(null, population);
        }

        protected override IAttemptResult TryVariant()
        {
            var mutated = (MicrobeSpecies)species.Clone();
            mutations.CreateMutatedSpecies((MicrobeSpecies)species, mutated);

            var config = new SimulationConfiguration(world, Constants.AUTO_EVO_VARIANT_SIMULATION_STEPS);

            config.ExcludedSpecies.Add(species);
            config.ExtraSpecies.Add(mutated);

            PopulationSimulation.Simulate(config);

            var population = config.Results.GetGlobalPopulation(mutated);

            return new AttemptResult(mutated, population);
        }

        private class AttemptResult : IAttemptResult
        {
            public AttemptResult(Species mutation, long score)
            {
                Mutation = mutation;
                Score = score;
            }

            public Species Mutation { get; }
            public long Score { get; }
        }
    }
}
