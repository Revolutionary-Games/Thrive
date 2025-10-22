namespace Components;

using SharedBase.Archive;
using Systems;

/// <summary>
///   Overrides rendering order for an entity with <see cref="EntityMaterial"/>. Used for some specific rendering
///   effects that can't be done otherwise. Microbe stage specifically uses:
///   <see cref="MicrobeRenderPrioritySystem"/>
/// </summary>
public struct RenderPriorityOverride : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Overrides the render priority of this Spatial. Use
    ///   <see cref="RenderPriorityOverrideHelpers.SetRenderPriority"/> to set this to ensure the applied flag is
    ///   reset to have the effect be applied.
    /// </summary>
    public int RenderPriority;

    /// <summary>
    ///   Must be set to false when changing <see cref="RenderPriority"/> to have a new value be applied
    /// </summary>
    public bool RenderPriorityApplied;

    public RenderPriorityOverride(int renderPriority)
    {
        RenderPriority = renderPriority;
        RenderPriorityApplied = false;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentRenderPriorityOverride;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(RenderPriority);
    }
}

public static class RenderPriorityOverrideHelpers
{
    public static RenderPriorityOverride ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > RenderPriorityOverride.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, RenderPriorityOverride.SERIALIZATION_VERSION);

        return new RenderPriorityOverride
        {
            RenderPriority = reader.ReadInt32(),
        };
    }

    public static void SetRenderPriority(this ref RenderPriorityOverride spatialInstance, int priority)
    {
        spatialInstance.RenderPriorityApplied = false;
        spatialInstance.RenderPriority = priority;
    }
}
