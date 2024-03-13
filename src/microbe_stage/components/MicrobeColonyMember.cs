namespace Components;

using DefaultEcs;

/// <summary>
///   Marker for microbes that are in a cell colony. The cell colony leader has <see cref="MicrobeColony"/>
///   component on it.
/// </summary>
[JSONDynamicTypeAllowed]
public struct MicrobeColonyMember
{
    /// <summary>
    ///   The colony leader can be accessed through this if colony members need to send messages back to the
    ///   colony
    /// </summary>
    public Entity ColonyLeader;

    public MicrobeColonyMember(in Entity colonyLeader)
    {
        ColonyLeader = colonyLeader;
    }
}

public static class MicrobeColonyMemberHelpers
{
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
        if (member.ColonyLeader.IsAlive && member.ColonyLeader.Has<MicrobeColony>())
        {
            colonyEntity = member.ColonyLeader;
            return true;
        }

        colonyEntity = default;
        return false;
    }
}
