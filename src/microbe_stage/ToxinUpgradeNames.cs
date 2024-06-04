using System;

/// <summary>
///   Names of upgrades for the various toxins
/// </summary>
public static class ToxinUpgradeNames
{
    /// <summary>
    ///   Technically this is "none" but internally upgrades don't save the "none" upgrade in the upgrades list.
    /// </summary>
    public const string OXYTOXY_UPGRADE_NAME = Constants.ORGANELLE_UPGRADE_SPECIAL_NONE;

    public const string MACROLIDE_UPGRADE_NAME = "macrolide";

    public const string CHANNEL_INHIBITOR_UPGRADE_NAME = "channel";

    public const string OXYGEN_INHIBITOR_UPGRADE_NAME = "oxygen_inhibitor";

    public const string CYTOTOXIN_UPGRADE_NAME = "cytotoxin";

    public static ToxinType GetToxinTypeFromUpgrades(this OrganelleUpgrades? upgrades)
    {
        if (upgrades == null || upgrades.UnlockedFeatures.Count < 1)
            return ToxinType.Oxytoxy;

        foreach (var feature in upgrades.UnlockedFeatures)
        {
            if (TryGetToxinTypeFromName(feature, out var type))
                return type;
        }

        return ToxinType.Oxytoxy;
    }

    public static ToxinType ToxinTypeFromName(string name)
    {
        if (TryGetToxinTypeFromName(name, out var result))
            return result;

        throw new ArgumentException("Name doesn't match any toxin upgrade name");
    }

    public static bool TryGetToxinTypeFromName(string name, out ToxinType type)
    {
        switch (name)
        {
            case OXYTOXY_UPGRADE_NAME:
                type = ToxinType.Oxytoxy;
                return true;
            case MACROLIDE_UPGRADE_NAME:
                type = ToxinType.Macrolide;
                return true;
            case CHANNEL_INHIBITOR_UPGRADE_NAME:
                type = ToxinType.ChannelInhibitor;
                return true;
            case OXYGEN_INHIBITOR_UPGRADE_NAME:
                type = ToxinType.OxygenMetabolismInhibitor;
                return true;
            case CYTOTOXIN_UPGRADE_NAME:
                type = ToxinType.Cytotoxin;
                return true;
        }

        type = ToxinType.Oxytoxy;
        return false;
    }

    public static string ToxinNameFromType(ToxinType toxinType)
    {
        switch (toxinType)
        {
            case ToxinType.Oxytoxy:
                return OXYTOXY_UPGRADE_NAME;
            case ToxinType.Cytotoxin:
                return CYTOTOXIN_UPGRADE_NAME;
            case ToxinType.Macrolide:
                return MACROLIDE_UPGRADE_NAME;
            case ToxinType.ChannelInhibitor:
                return CHANNEL_INHIBITOR_UPGRADE_NAME;
            case ToxinType.OxygenMetabolismInhibitor:
                return OXYGEN_INHIBITOR_UPGRADE_NAME;
            default:
                throw new ArgumentOutOfRangeException(nameof(toxinType), toxinType, null);
        }
    }
}
