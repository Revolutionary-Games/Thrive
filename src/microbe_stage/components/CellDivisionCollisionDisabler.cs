namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Lets dividing cells clip through each other until too far
/// </summary>
public struct CellDivisionCollisionDisabler : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Entity IgnoredCollisionWith;

    public float SeparationForce = 1.0f;

    public CellDivisionCollisionDisabler(in Entity ignoredCollisionWith)
    {
        IgnoredCollisionWith = ignoredCollisionWith;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCellDivisionCollisionDisabler;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // Save only persistent state

        writer.WriteAnyRegisteredValueAsObject(IgnoredCollisionWith);
        writer.Write(SeparationForce);
    }
}

public static class CellDivisionCollisionDisablerHelpers
{
    public static CellDivisionCollisionDisabler ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CellDivisionCollisionDisabler.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CellDivisionCollisionDisabler.SERIALIZATION_VERSION);

        return new CellDivisionCollisionDisabler
        {
            IgnoredCollisionWith = reader.ReadObject<Entity>(),
            SeparationForce = reader.ReadFloat(),
        };
    }
}
