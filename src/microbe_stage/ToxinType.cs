/// <summary>
///   Toxin types. See <see cref="ToxinUpgradeNames"/> for what names match each enum value in the upgrades list.
/// </summary>
public enum ToxinType
{
    /// <summary>
    ///   The default toxin type for now. Less effective against oxygen using species.
    /// </summary>
    Oxytoxy = 0,

    /// <summary>
    ///   A basic toxin that targets membranes. This will be the default toxin at some point.
    /// </summary>
    Cytotoxin,

    /// <summary>
    ///   A movement (base movement) inhibiting toxin
    /// </summary>
    Macrolide,

    /// <summary>
    ///   ATP production inhibiting toxin
    /// </summary>
    ChannelInhibitor,

    /// <summary>
    ///   Toxin that gets bonus damage against oxygen users (opposite of <see cref="Oxytoxy"/>)
    /// </summary>
    OxygenMetabolismInhibitor,
}
