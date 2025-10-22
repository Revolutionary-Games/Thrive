namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Holds operation info for delayed microbe colony operations
/// </summary>
public struct DelayedMicrobeColony : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   If not default, then this entity wants to attach to a colony after initialization. Note that this entity
    ///   must already have a <see cref="AttachedToEntity"/> component added.
    /// </summary>
    public Entity FinishAttachingToColony;

    public int AttachIndex;

    public int GrowAdditionalMembers;

    // This doesn't have an applied field as this component is always removed after operating on it because this
    // component only is used once on each entity

    /// <summary>
    ///   Delayed growth of colony members
    /// </summary>
    /// <param name="growAdditionalMembers">
    ///   How many members to add (should be one less than the multicellular body plan count for fully grown colony)
    /// </param>
    public DelayedMicrobeColony(int growAdditionalMembers)
    {
        GrowAdditionalMembers = growAdditionalMembers;

        FinishAttachingToColony = Entity.Null;
        AttachIndex = 0;
    }

    /// <summary>
    ///   Attach to a colony in a delayed way (must have attached position already set)
    /// </summary>
    /// <param name="delayAttachToColony">Entity to attach to</param>
    /// <param name="targetIndex">
    ///   The index the new member should be placed at. This exists to allow ensuring colonies to have consistent
    ///   order for their delay-attached members if multiple is added at once;
    /// </param>
    public DelayedMicrobeColony(Entity delayAttachToColony, int targetIndex)
    {
        FinishAttachingToColony = delayAttachToColony;
        AttachIndex = targetIndex;

        GrowAdditionalMembers = 0;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentDelayedMicrobeColony;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(A PROPERTY);
        writer.WriteObject(A PROPERTY OF COMPLEX TYPE);
    }
}

public static class DelayedMicrobeColonyHelpers
{
    public static DelayedMicrobeColony ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > DelayedMicrobeColony.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, DelayedMicrobeColony.SERIALIZATION_VERSION);

        return new DelayedMicrobeColony
        {
            AProperty = reader.ReadFloat(),
            AnotherProperty = reader.ReadObject<PropertyTypeGoesHere>(),
        };
    }
}
