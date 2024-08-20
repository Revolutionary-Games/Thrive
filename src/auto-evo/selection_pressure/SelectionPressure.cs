namespace AutoEvo;

using System.Collections.Generic;

/// <summary>
///   Selection pressures in miches both score species how well they do and also generate mutations for species to be
///   better in terms of this selection pressure
/// </summary>
public abstract class SelectionPressure
{
    public readonly float Strength;
    public readonly List<IMutationStrategy<MicrobeSpecies>> Mutations;

    public SelectionPressure(float strength, List<IMutationStrategy<MicrobeSpecies>> mutations)
    {
        Strength = strength;
        Mutations = mutations;
    }

    public abstract LocalizedString Name { get; }

    public abstract float Score(Species species, Patch patch, SimulationCache cache);
    public abstract float GetEnergy(Patch patch);

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

    /// <summary>
    ///   A description of this miche.
    /// </summary>
    public virtual LocalizedString GetDescription()
    {
        return Name;
    }

    /// <summary>
    ///   Converts this to a string. For some reason this is used to display the Selection Pressure in the Miche Tree.
    /// </summary>
    /// <returns>A readable string of this selection pressure</returns>
    public override string ToString()
    {
        return Name.ToString();
    }
}
