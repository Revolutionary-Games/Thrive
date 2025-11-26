namespace Components;

using Godot;
using SharedBase.Archive;

/// <summary>
///   Displays a single <see cref="Node3D"/> as this entity's graphics in Godot
/// </summary>
public struct SpatialInstance : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Node3D? GraphicalInstance;

    /// <summary>
    ///   Visual scale to set. Only applies when <see cref="ApplyVisualScale"/> is set to true to only require
    ///   entities that want to scale to set this field
    /// </summary>
    public Vector3 VisualScale;

    /// <summary>
    ///   If true, applies visual scale to <see cref="GraphicalInstance"/>
    /// </summary>
    public bool ApplyVisualScale;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentSpatialInstance;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(VisualScale);
        writer.Write(ApplyVisualScale);
    }
}

public static class SpatialInstanceHelpers
{
    public static SpatialInstance ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SpatialInstance.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SpatialInstance.SERIALIZATION_VERSION);

        return new SpatialInstance
        {
            VisualScale = reader.ReadVector3(),
            ApplyVisualScale = reader.ReadBool(),
        };
    }
}
