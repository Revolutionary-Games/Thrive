/// <summary>
///   Easy access to often required upgrade data checks
/// </summary>
public static class OrganelleUpgradeHelpers
{
    public static bool HasInjectisomeUpgrade(this IReadOnlyOrganelleUpgrades? organelleUpgrades)
    {
        if (organelleUpgrades == null)
            return false;

        return organelleUpgrades.UnlockedFeatures.Contains(Constants.PILUS_INJECTISOME_UPGRADE_NAME);
    }
}
