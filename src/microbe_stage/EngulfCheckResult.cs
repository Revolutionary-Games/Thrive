public enum EngulfCheckResult
{
    /// <summary>
    ///   Target can be engulfed
    /// </summary>
    Ok,

    /// <summary>
    ///   Targeting an entity that can't be engulfed at all (missing components for example)
    /// </summary>
    InvalidEntity,

    NotInEngulfMode,
    RecentlyExpelled,
    TargetDead,
    TargetTooBig,
    IngestedMatterFull,
    CannotCannibalize,
    TargetInvulnerable,
}
