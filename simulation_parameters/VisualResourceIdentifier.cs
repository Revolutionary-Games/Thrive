/// <summary>
///   Identifiers for <see cref="VisualResourceData"/>. Exact values are used in saves so new values must be appended
///   at the end.
/// </summary>
public enum VisualResourceIdentifier
{
    /// <summary>
    ///   No visual resource
    /// </summary>
    None = 0,

    /// <summary>
    ///   An error model when something can't be found
    /// </summary>
    Error,

    CellBurstEffect,

    /// <summary>
    ///   Iron projectile effect
    /// </summary>
    SidenophoreProjectile,

    /// <summary>
    ///   Oxytoxy effect
    /// </summary>
    AgentProjectile,

    AgentProjectileCytotoxin,

    AgentProjectileMacrolide,

    AgentProjectileCyanide,

    AgentProjectileChannelInhibitor,
}
