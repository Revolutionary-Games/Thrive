namespace Components;

using System.Collections.Generic;
using SharedBase.Archive;

/// <summary>
///   General info about the reproduction status of a creature
/// </summary>
public struct ReproductionStatus : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Dictionary<Compound, float>? MissingCompoundsForBaseReproduction;

    public ReproductionStatus(IReadOnlyDictionary<Compound, float> baseReproductionCost)
    {
        MissingCompoundsForBaseReproduction = baseReproductionCost.CloneShallow();
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentReproductionStatus;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        if (MissingCompoundsForBaseReproduction != null)
        {
            writer.WriteObject(MissingCompoundsForBaseReproduction);
        }
        else
        {
            writer.WriteNullObject();
        }
    }
}

public static class ReproductionStatusHelpers
{
    public static ReproductionStatus ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > ReproductionStatus.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, ReproductionStatus.SERIALIZATION_VERSION);

        return new ReproductionStatus
        {
            MissingCompoundsForBaseReproduction = reader.ReadObject<Dictionary<Compound, float>>(),
        };
    }

    /// <summary>
    ///   Sets up the base reproduction cost on top of the normal costs (for microbes)
    /// </summary>
    public static void SetupRequiredBaseReproductionCompounds(this ref ReproductionStatus reproductionStatus,
        Species species)
    {
        reproductionStatus.MissingCompoundsForBaseReproduction ??= new Dictionary<Compound, float>();

        reproductionStatus.MissingCompoundsForBaseReproduction.Clear();
        reproductionStatus.MissingCompoundsForBaseReproduction.Merge(species.BaseReproductionCost);
    }

    public static void CalculateAlreadyUsedBaseReproductionCompounds(this ref ReproductionStatus reproductionStatus,
        Species species, Dictionary<Compound, float> resultReceiver)
    {
        if (reproductionStatus.MissingCompoundsForBaseReproduction == null)
            return;

        foreach (var totalCost in species.BaseReproductionCost)
        {
            if (!reproductionStatus.MissingCompoundsForBaseReproduction.TryGetValue(totalCost.Key,
                    out var left))
            {
                // If we used any unknown values (which are 0) to calculate the absorbed amounts, this would be
                // vastly incorrect
                continue;
            }

            var absorbed = totalCost.Value - left;

            if (!(absorbed > 0))
                continue;

            resultReceiver.TryGetValue(totalCost.Key, out var alreadyAbsorbed);
            resultReceiver[totalCost.Key] = alreadyAbsorbed + absorbed;
        }
    }
}
