using System;
using System.Collections.Generic;
using SharedBase.Archive;
using ThriveScriptsShared;

/// <summary>
///   Contains definitions of meteors for meteor impact event
/// </summary>
public class Meteor : RegistryType
{
    public PatchEventTypes VisualEffect;

    public List<string> Chunks = new();

    public Dictionary<Compound, float> Compounds = new();

    public double Probability;

    public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.Meteor;

    public static Meteor ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > IRegistryType.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, IRegistryType.SERIALIZATION_VERSION);

        return SimulationParameters.Instance.GetMeteor(reader.ReadString() ?? throw new NullArchiveObjectException());
    }

    public static void CheckAllMeteors(List<Meteor> meteors)
    {
        double totalSum = 0;
        foreach (var meteor in meteors)
        {
            totalSum += meteor.Probability;
        }

        if (Math.Abs(totalSum - 1.0) > 1e-6)
        {
            throw new InvalidRegistryDataException($"Meteors probability sum mismatch: {totalSum}. Expected: 1.0");
        }
    }

    public override void Check(string name)
    {
        if (Probability < 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Invalid probability value for meteor: " + InternalName);
        }
    }

    public override void ApplyTranslations()
    {
    }
}
