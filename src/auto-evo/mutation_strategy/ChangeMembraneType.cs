namespace AutoEvo
{
    using System.Collections.Generic;

    public class ChangeMembraneType : IMutationStrategy<MicrobeSpecies>
    {
        private MembraneType membraneType;
        public ChangeMembraneType(MembraneType membraneType)
        {
            this.membraneType = membraneType;
        }

        public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.MembraneType = membraneType;

            return new List<MicrobeSpecies> { newSpecies };
        }
    }
}
