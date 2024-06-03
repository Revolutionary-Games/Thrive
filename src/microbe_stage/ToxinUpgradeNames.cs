/// <summary>
///   Names of upgrades for the various toxins
/// </summary>
public static class ToxinUpgradeNames
{
    /// <summary>
    ///   Technically this would be "none" but internally upgrades don't save the "none" upgrade in the upgrades list.
    /// </summary>
    public const string OXYTOXY_UPGRADE_NAME = "";

    public const string MACROLIDE_UPGRADE_NAME = "macrolide";

    public const string CHANNEL_INHIBITOR_UPGRADE_NAME = "channel";

    public const string OXYGEN_INHIBITOR_UPGRADE_NAME = "oxygen_inhibitor";

    public const string CYTOTOXIN_UPGRADE_NAME = "cytotoxin";

    public static ToxinType GetToxinTypeFromUpgrades(this OrganelleUpgrades? upgrades)
    {
        if (upgrades == null || upgrades.UnlockedFeatures.Count < 1)
            return ToxinType.Oxytoxy;

        var features = upgrades.UnlockedFeatures;

        // TODO: is a loop like this faster than using Contains multiple times?
        foreach (var feature in features)
        {
            switch (feature)
            {
                case MACROLIDE_UPGRADE_NAME:
                    return ToxinType.Macrolide;
                case CHANNEL_INHIBITOR_UPGRADE_NAME:
                    return ToxinType.ChannelInhibitor;
                case OXYGEN_INHIBITOR_UPGRADE_NAME:
                    return ToxinType.OxygenMetabolismInhibitor;
                case CYTOTOXIN_UPGRADE_NAME:
                    return ToxinType.Cytotoxin;
            }
        }

        /*if (features.Contains(MACROLIDE_UPGRADE_NAME))
            return ToxinType.Macrolide;

        if (features.Contains(CHANNEL_INHIBITOR_UPGRADE_NAME))
            return ToxinType.ChannelInhibitor;

        if (features.Contains(OXYGEN_INHIBITOR_UPGRADE_NAME))
            return ToxinType.OxygenMetabolismInhibitor;

        if (features.Contains(CYTOTOXIN_UPGRADE_NAME))
            return ToxinType.Cytotoxin;*/

        return ToxinType.Oxytoxy;
    }
}
