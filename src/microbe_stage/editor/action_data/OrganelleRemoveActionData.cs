using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class OrganelleRemoveActionData : HexRemoveActionData<OrganelleTemplate, CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Used for replacing Cytoplasm. If true, this action is free.
    /// </summary>
    public bool GotReplaced;

    public OrganelleRemoveActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
    {
    }

    public OrganelleRemoveActionData(OrganelleTemplate organelle) : base(organelle, organelle.Position,
        organelle.Orientation)
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.OrganelleRemoveActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.OrganelleRemoveActionData)
            throw new NotSupportedException();

        writer.WriteObject((OrganelleRemoveActionData)obj);
    }

    public static OrganelleRemoveActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var hexVersion = reader.ReadUInt16();
        var instance = new OrganelleRemoveActionData(reader.ReadObject<OrganelleTemplate>(), reader.ReadHex(),
            reader.ReadInt32());

        // Base version is different
        instance.ReadBasePropertiesFromArchive(reader, hexVersion);

        instance.GotReplaced = reader.ReadBool();
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_HEX);
        base.WriteToArchive(writer);

        writer.Write(GotReplaced);
    }

    protected override double CalculateBaseCostInternal()
    {
        return GotReplaced ? 0 : base.CalculateBaseCostInternal();
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = base.CalculateCostInternal(history, insertPosition);
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // Endosymbionts can be deleted for free after placing (not that it is very useful, but it should be free)
            if (other is EndosymbiontPlaceActionData endosymbiontPlaceActionData &&
                MatchesContext(endosymbiontPlaceActionData))
            {
                if (RemovedHex == endosymbiontPlaceActionData.PlacedOrganelle)
                {
                    return (0, cost.RefundCost);
                }
            }

            if (other is OrganelleUpgradeActionData upgradeActionData &&
                upgradeActionData.UpgradedOrganelle == RemovedHex && MatchesContext(upgradeActionData))
            {
                // This replaces (refunds) the MP for an upgrade done to this organelle
                if (ReferenceEquals(upgradeActionData.UpgradedOrganelle, RemovedHex))
                {
                    refund += upgradeActionData.GetAndConsumeAvailableRefund();
                }
            }
        }

        return (cost.Cost, cost.RefundCost + refund);
    }
}
