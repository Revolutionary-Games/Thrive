using System.Collections.Generic;

public interface IReadOnlyOrganelleUpgrades
{
    public IReadOnlyList<string> UnlockedFeatures { get; }

    public IComponentSpecificUpgrades? CustomUpgradeData { get; }

    public OrganelleUpgrades? Clone();
}
