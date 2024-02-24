namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class AvoidPredationSelectionPressure : SelectionPressure
    {
        public Species Predator;
        public Patch Patch;
        private readonly float weight;

        public AvoidPredationSelectionPressure(Species predator, float weight, Patch patch) : base(weight,
            new List<IMutationStrategy<MicrobeSpecies>>
            {
                new AddOrganelleAnywhere(organelle => organelle.MPCost < 30),

                // new LowerRigidity(),
            })
        {
            Patch = patch;
            Predator = predator;
            this.weight = weight;
        }

        public override float Score(MicrobeSpecies species, SimulationCache cache)
        {
            var predationScore = new PredationEffectivenessPressure(species, Patch, 1, cache)
                .FitnessScore((MicrobeSpecies)Predator, species);

            if (predationScore == 0)
            {
                return 1.0f * weight;
            }

            return 1 / predationScore * weight;
        }
    }
}
