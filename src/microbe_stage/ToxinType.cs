using System.ComponentModel;

/// <summary>
///   Toxin types. See <see cref="ToxinUpgradeNames"/> for what names match each enum value in the upgrades list.
/// </summary>
public enum ToxinType
{
    /// <summary>
    ///   The default toxin type for now. Less effective against oxygen using species.
    /// </summary>
    [Description("TOXIN_OXYTOXY_DESCRIPTION")]
    Oxytoxy = 0,

    /// <summary>
    ///   A basic toxin that targets membranes. This will be the default toxin at some point.
    /// </summary>
    [Description("TOXIN_CYTOTOXIN_DESCRIPTION")]
    Cytotoxin,

    /// <summary>
    ///   A movement (base movement) inhibiting toxin
    /// </summary>
    [Description("TOXIN_MACROLIDE_DESCRIPTION")]
    Macrolide,

    /// <summary>
    ///   ATP production inhibiting toxin
    /// </summary>
    [Description("TOXIN_CHANNEL_INHIBITOR_DESCRIPTION")]
    ChannelInhibitor,

    /// <summary>
    ///   Toxin that gets bonus damage against oxygen users (opposite of <see cref="Oxytoxy"/>)
    /// </summary>
    [Description("TOXIN_OXYGEN_METABOLISM_INHIBITOR_DESCRIPTION")]
    OxygenMetabolismInhibitor,
}
