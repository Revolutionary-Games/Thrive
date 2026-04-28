namespace Components;

using SharedBase.Archive;

[ComponentIsReadByDefault]
public struct SpecializationFactor : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Total applied specialization bonus for this specific cell, including any adjacency effects.
    /// </summary>
    public float TotalSpecializationBonus;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpecializationFactor;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(TotalSpecializationBonus);
    }
}

public static class SpecializationFactorHelpers
{
    public static SpecializationFactor ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SpecializationFactor.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SpecializationFactor.SERIALIZATION_VERSION);

        return new SpecializationFactor
        {
            TotalSpecializationBonus = reader.ReadFloat(),
        };
    }
}
