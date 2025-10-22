using Arch.Core;
using Arch.Core.Extensions;
using Components;
using SharedBase.Archive;

/// <summary>
///   Base interface for all ECS components that can be archived. Implementing this interface requires also registering
///   the component in <see cref="ComponentDeserializers"/>
/// </summary>
public interface IArchivableComponent
{
    public ushort CurrentArchiveVersion { get; }
    public ThriveArchiveObjectType ArchiveObjectType { get; }

    public void WriteToArchive(ISArchiveWriter writer);
}

public static class ComponentDeserializers
{
    public static bool ReadComponentToEntity(ISArchiveReader reader, Entity entity, ThriveArchiveObjectType objectType,
        ushort version)
    {
        switch (objectType)
        {
            case ThriveArchiveObjectType.ComponentBioProcesses:
                entity.Add(BioProcessesHelpers.ReadFromArchive(reader, version));
                return true;
        }

        return false;
    }
}
