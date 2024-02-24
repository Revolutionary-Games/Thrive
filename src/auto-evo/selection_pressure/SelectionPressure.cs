namespace AutoEvo
{
    using System.Collections.Generic;

    public abstract class SelectionPressure
    {
        public float Strength;
        public List<IMutationStrategy<MicrobeSpecies>> Mutations;
        public int EnergyProvided = 0;

        public SelectionPressure(float strength, List<IMutationStrategy<MicrobeSpecies>> mutations)
        {
            Strength = strength;
            Mutations = mutations;
        }

        public abstract float Score(MicrobeSpecies species, SimulationCache cache);
    }
}
