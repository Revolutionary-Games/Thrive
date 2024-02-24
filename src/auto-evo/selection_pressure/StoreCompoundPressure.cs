namespace AutoEvo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class StoragePressure : SelectionPressure
    {
        public StoragePressure(float weight) : base(
            weight,
            new List<IMutationStrategy<MicrobeSpecies>>
            {
                new AddOrganelleAnywhere(organelle => organelle.Components?.Storage?.Capacity > 0.5f),
            })
        {
        }

        public override float Score(MicrobeSpecies species, SimulationCache cache)
        {
            // It should be impossible for this to be null at this point
            return species.Organelles.Sum(x => x.Definition.Components!.Storage!.Capacity);
        }
    }
}
