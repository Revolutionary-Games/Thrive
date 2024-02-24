namespace AutoEvo
{
    using System;
    using System.Collections.Generic;

    public class LowerRigidity : IMutationStrategy<MicrobeSpecies>
    {
        public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, MutationLibrary partList)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.MembraneRigidity = (newSpecies.MembraneRigidity + -1.0f) / 2.0f;

            // Just cap it at some point
            if (Math.Abs(newSpecies.MembraneRigidity - baseSpecies.MembraneRigidity) < 0.1)
            {
                newSpecies.MembraneRigidity = -1.0f;
            }

            return new List<MicrobeSpecies> { newSpecies };
        }
    }
}
