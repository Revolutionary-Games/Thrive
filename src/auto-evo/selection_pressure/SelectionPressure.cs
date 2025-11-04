namespace AutoEvo;

using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   Selection pressures in miches both score species how well they do and also generate mutations for species to be
///   better in terms of this selection pressure
/// </summary>
public abstract class SelectionPressure : IArchivable
{
    public const ushort SERIALIZATION_VERSION_BASE = 1;

    public readonly float Weight;

    public readonly List<IMutationStrategy<MicrobeSpecies>> Mutations;

    public SelectionPressure(float weight, List<IMutationStrategy<MicrobeSpecies>> mutations)
    {
        Weight = weight;
        Mutations = mutations;
    }

    public abstract ushort CurrentArchiveVersion { get; }
    public abstract ArchiveObjectType ArchiveObjectType { get; }
    public bool CanBeReferencedInArchive => true;

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

    public virtual void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(Weight);

        // By default, mutations are not needed to be saved for all miche types, so they aren't.
        // writer.WriteObject(Mutations);
    }

    /// <summary>
    ///   Converts this to a string. For some reason this is used to display the Selection Pressure in the Miche Tree.
    /// </summary>
    /// <returns>A readable string of this selection pressure</returns>
    public override string ToString()
    {
        return Name.ToString();
    }

    protected virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_BASE or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_BASE);

        // We only have constructor properties
    }
}
