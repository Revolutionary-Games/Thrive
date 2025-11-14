using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class OrganelleUpgradeActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public OrganelleUpgrades NewUpgrades;
    public OrganelleUpgrades OldUpgrades;

    // TODO: make the upgrade not cost MP if a new organelle of the same type is placed at the same location and then
    // upgraded in the same way
    public OrganelleTemplate UpgradedOrganelle;

    public OrganelleUpgradeActionData(OrganelleUpgrades oldUpgrades, OrganelleUpgrades newUpgrades,
        OrganelleTemplate upgradedOrganelle)
    {
        OldUpgrades = oldUpgrades;
        NewUpgrades = newUpgrades;
        UpgradedOrganelle = upgradedOrganelle;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.OrganelleUpgradeActionData)
            throw new NotSupportedException();

        writer.WriteObject((OrganelleUpgradeActionData)obj);
    }

    public static OrganelleUpgradeActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new OrganelleUpgradeActionData(reader.ReadObject<OrganelleUpgrades>(),
            reader.ReadObject<OrganelleUpgrades>(), reader.ReadObject<OrganelleTemplate>());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(OldUpgrades);
        writer.WriteObject(NewUpgrades);
        writer.WriteObject(UpgradedOrganelle);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override double CalculateBaseCostInternal()
    {
        return MicrobeSpeciesComparer.CalculateUpgradeCost(UpgradedOrganelle.Definition.AvailableUpgrades,
            NewUpgrades.UnlockedFeatures,
            OldUpgrades.UnlockedFeatures);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        int i = CalculateValidityRegionStart(history, insertPosition, 0);

        var count = history.Count;
        for (; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is OrganelleUpgradeActionData upgradeActionData && MatchesContext(upgradeActionData))
            {
                if (ReferenceEquals(UpgradedOrganelle, upgradeActionData.UpgradedOrganelle))
                {
                    // When there's a previous upgrade, calculate this cost in relation to that to process refunds as
                    // well correctly
                    cost = MicrobeSpeciesComparer.CalculateUpgradeCost(UpgradedOrganelle.Definition.AvailableUpgrades,
                        NewUpgrades.UnlockedFeatures,
                        upgradeActionData.NewUpgrades.UnlockedFeatures, true);
                }
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        // Doesn't need to merge as organelle upgrades are applied when hitting "ok" in the GUI and not for each slider
        // step
        return false;
    }

    protected override bool ActionDenotesInterestingRegionBoundary(EditorCombinableActionData action)
    {
        // We want to calculate the cost from the latest upgrade action before us.
        // But also ignore anything before a delete operation, because deletes already refund the upgrade cost.
        if (action is OrganelleUpgradeActionData upgradeActionData && MatchesContext(upgradeActionData) &&
            ReferenceEquals(UpgradedOrganelle, upgradeActionData.UpgradedOrganelle))
        {
            return true;
        }

        if (action is HexRemoveActionData<OrganelleTemplate, CellType> deleteActionData &&
            MatchesContext(deleteActionData) && ReferenceEquals(deleteActionData.RemovedHex, UpgradedOrganelle))
        {
            return true;
        }

        return false;
    }
}
