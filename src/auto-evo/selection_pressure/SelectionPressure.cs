namespace AutoEvo;

using System.Collections.Generic;
using System.Dynamic;

public abstract class SelectionPressure
{
    public readonly float Strength;
    public readonly List<IMutationStrategy<MicrobeSpecies>> Mutations;
    public readonly int EnergyProvided = 0;

    public SelectionPressure(float strength, List<IMutationStrategy<MicrobeSpecies>> mutations, int energyProvided)
    {
        Strength = strength;
        Mutations = mutations;
        EnergyProvided = energyProvided;
    }

    public abstract float Score(MicrobeSpecies species, SimulationCache cache);

    public abstract override string ToString();

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

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }
}
