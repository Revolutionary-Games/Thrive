/// <summary>
///   Identifiers for <see cref="VisualResourceData"/>. Exact values are used in saves, so new values must be appended
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
    ///   Oxytoxy effect
    /// </summary>
    AgentProjectile,

    AgentProjectileCytotoxin,

    AgentProjectileMacrolide,

    AgentProjectileCyanide,

    AgentProjectileChannelInhibitor,

    /// <summary>
    ///   Iron projectile effect
    /// </summary>
    SiderophoreProjectile,

    UnderwaterVentModel1,

    ClayTerrain1,
    ClayTerrain2,

    QuartzTerrain1,
    QuartzTerrain2,
    QuartzTerrain3,

    PyriteTerrain1,
    PyriteTerrain2,
    ChalcopyriteTerrain1,
    ChalcopyriteTerrain2,
    SerpentiniteTerrain1,
    SerpentiniteTerrain2,
}
