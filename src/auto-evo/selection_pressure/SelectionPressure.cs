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

        /// <summary>
        ///   Calculates the relative difference between the old and new scores
        /// </summary>
        public float WeightedComparedScores(float newScore, float oldScore)
        {
            if (newScore <= 0)
            {
                return -1;
            }

            if (oldScore == 0)
            {
                return newScore > 0 ? 1 : 0;
            }

            if (newScore > oldScore)
            {
                return newScore / oldScore * Strength;
            }
            else if (oldScore > newScore)
            {
                return -(oldScore / newScore) * Strength;
            }
            else
            {
                return 0;
            }
        }
    }
}
