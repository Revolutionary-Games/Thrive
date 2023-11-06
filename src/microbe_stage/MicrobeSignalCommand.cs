public enum MicrobeSignalCommand
{
    /// <summary>
    ///   No command specified
    /// </summary>
    None = 0,

    /// <summary>
    ///   Wants other cells to move very close (swarm around) the emitting cell
    /// </summary>
    MoveToMe,

    /// <summary>
    ///   Wants other cells to be relatively close (move to be pretty near if far away)
    /// </summary>
    FollowMe,

    /// <summary>
    ///   Run away from the emitting cell
    /// </summary>
    FleeFromMe,

    /// <summary>
    ///   Increase aggression by a lot against every enemy nearby
    /// </summary>
    BecomeAggressive,
}
