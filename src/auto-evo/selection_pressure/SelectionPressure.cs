namespace AutoEvo;

using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Selection pressures in miches both score species how well they do and also generate mutations for species to be
///   better in terms of this selection pressure
/// </summary>
[JSONDynamicTypeAllowed]
public abstract class SelectionPressure
{
    [JsonProperty]
    public readonly float Weight;

    [JsonIgnore]
    public readonly List<IMutationStrategy<MicrobeSpecies>> Mutations;

    public SelectionPressure(float weight, List<IMutationStrategy<MicrobeSpecies>> mutations)
    {
        Weight = weight;
        Mutations = mutations;
    }

    [JsonIgnore]
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
            return newScore / oldScore * Weight;
        }

        if (oldScore > newScore)
        {
            return -(oldScore / newScore) * Weight;
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
