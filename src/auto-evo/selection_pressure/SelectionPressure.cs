namespace AutoEvo;

using System.Collections.Generic;

public abstract class SelectionPressure
{
    public readonly float Strength;
    public readonly List<IMutationStrategy<MicrobeSpecies>> Mutations;

    public SelectionPressure(float strength, List<IMutationStrategy<MicrobeSpecies>> mutations)
    {
        Strength = strength;
        Mutations = mutations;
    }

    public abstract float Score(Species species, SimulationCache cache);
    public abstract float GetEnergy();

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

        if (oldScore > newScore)
        {
            return -(oldScore / newScore) * Strength;
        }

        return 0;
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    /// <summary>
    ///   A description of this miche.
    /// </summary>
    public abstract LocalizedString GetDescription();

    // ToString is used to display the Selection Pressure in the Miche Tree
    public abstract override string ToString();
}
