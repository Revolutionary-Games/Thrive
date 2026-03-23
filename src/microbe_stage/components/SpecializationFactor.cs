namespace Components;

using SharedBase.Archive;

[ComponentIsReadByDefault]
public struct SpecializationFactor : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float SpecializationBonus;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpecializationFactor;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(SpecializationBonus);
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
            SpecializationBonus = reader.ReadFloat(),
        };
    }
}
