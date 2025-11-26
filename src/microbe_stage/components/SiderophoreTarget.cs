namespace Components;

using SharedBase.Archive;

/// <summary>
///   Marks entity as the target for siderophore
/// </summary>
public struct SiderophoreTarget : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSiderophoreTarget;

    public void WriteToArchive(ISArchiveWriter writer)
    {
    }
}

public static class SiderophoreTargetHelpers
{
    public static SiderophoreTarget ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SiderophoreTarget.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SiderophoreTarget.SERIALIZATION_VERSION);

        return default(SiderophoreTarget);
    }
}
