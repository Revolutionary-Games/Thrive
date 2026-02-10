namespace Components;

using Arch.Core;
using SharedBase.Archive;

/// <summary>
///   Marker for microbes that are in a cell colony. The cell colony leader has <see cref="MicrobeColony"/>
///   component on it.
/// </summary>
public struct MicrobeColonyMember : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   The colony leader can be accessed through this if colony members need to send messages back to the
    ///   colony
    /// </summary>
    public Entity ColonyLeader;

    public MicrobeColonyMember(in Entity colonyLeader)
    {
        ColonyLeader = colonyLeader;
    }

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeColonyMember;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteAnyRegisteredValueAsObject(ColonyLeader);
    }
}

public static class MicrobeColonyMemberHelpers
{
    public static MicrobeColonyMember ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeColonyMember.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeColonyMember.SERIALIZATION_VERSION);

        return new MicrobeColonyMember
        {
            ColonyLeader = reader.ReadObject<Entity>(),
        };
    }

    /// <summary>
    ///   Gets the <see cref="MicrobeColony"/> from that colony's member
    /// </summary>
    /// <param name="member">Colony member to start from</param>
    /// <param name="colonyEntity">Set to the colony entity when successful</param>
    /// <returns>
    ///   True on success, false if the colony was incorrectly destroyed with this still being a member
    /// </returns>
    public static bool GetColonyFromMember(this ref MicrobeColonyMember member, out Entity colonyEntity)
    {
        if (member.ColonyLeader.IsAliveAndHas<MicrobeColony>())
        {
            colonyEntity = member.ColonyLeader;
            return true;
        }

        colonyEntity = Entity.Null;
        return false;
    }
}
