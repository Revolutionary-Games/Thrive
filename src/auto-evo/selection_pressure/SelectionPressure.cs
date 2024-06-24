namespace AutoEvo;

using System;
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

    public abstract float Score(MicrobeSpecies species, SimulationCache cache);
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
    ///   A description of this miche. Needs to support translations changing and be player readable
    /// </summary>
    /// <returns>A formattable that has the description in it</returns>
    public abstract IFormattable GetDescription();

    public abstract override string ToString();
}
