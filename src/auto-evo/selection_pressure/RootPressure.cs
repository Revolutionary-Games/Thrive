namespace AutoEvo
{
    using System.Collections.Generic;
    using System.Linq;
    using Godot;

    public class RootPressure : SelectionPressure
    {
        private readonly Patch patch;
        private readonly float weight;

        public RootPressure(Patch patch, float weight) : base(
            weight,
            new List<IMutationStrategy<MicrobeSpecies>>
            {
                // Add a little bit of randomness to the miche tree
                new AddOrganelleAnywhere(_ => true),
                new RemoveAnyOrganelle(),
            })
        {
            this.patch = patch;
            this.weight = weight;
        }

        public override float Score(MicrobeSpecies species, SimulationCache cache)
        {
            return 1;
        }
    }
}
