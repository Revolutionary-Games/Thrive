namespace Components;

using SharedBase.Archive;

/// <summary>
///   Makes entities vent excess (or not-useful) compounds from
/// </summary>
public struct UnneededCompoundVenter : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Sets how many extra compounds above capacity a thing needs to have before some are vented.
    ///   For example, 2 means any compounds that are above 2x the capacity will be vented.
    /// </summary>
    public float VentThreshold;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentUnneededCompoundVenter;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(VentThreshold);
    }
}

public static class UnneededCompoundVenterHelpers
{
    public static UnneededCompoundVenter ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > UnneededCompoundVenter.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, UnneededCompoundVenter.SERIALIZATION_VERSION);

        return new UnneededCompoundVenter
        {
            VentThreshold = reader.ReadFloat(),
        };
    }
}
