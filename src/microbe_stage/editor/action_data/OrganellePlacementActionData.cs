using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class OrganellePlacementActionData : HexPlacementActionData<OrganelleTemplate, CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public List<OrganelleTemplate>? ReplacedCytoplasm;

    public OrganellePlacementActionData(OrganelleTemplate organelle, Hex location, int orientation) : base(organelle,
        location, orientation)
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.OrganellePlacementActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.ReproductionOrganelleData)
            throw new NotSupportedException();

        writer.WriteObject((OrganellePlacementActionData)obj);
    }

    public static OrganellePlacementActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var hexVersion = reader.ReadUInt16();
        var instance = new OrganellePlacementActionData(reader.ReadObject<OrganelleTemplate>(), reader.ReadHex(),
            reader.ReadInt32());

        // Base version is different
        instance.ReadBasePropertiesFromArchive(reader, hexVersion);

        instance.ReplacedCytoplasm = reader.ReadObjectOrNull<List<OrganelleTemplate>>();
        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(SERIALIZATION_VERSION_HEX);
        base.WriteToArchive(writer);

        writer.WriteObjectOrNull(ReplacedCytoplasm);
    }

    protected override double CalculateBaseCostInternal()
    {
        return PlacedHex.Definition.MPCost;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is OrganelleMoveActionData moveActionData && MatchesContext(moveActionData))
            {
                if ((moveActionData.MovedHex.Definition == PlacedHex.Definition &&
                        moveActionData.OldLocation == Location) ||
                    ReplacedCytoplasm?.Contains(moveActionData.MovedHex) == true)
                {
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }
            }

            if (other is OrganellePlacementActionData placementActionData &&
                ReplacedCytoplasm?.Contains(placementActionData.PlacedHex) == true &&
                MatchesContext(placementActionData))
            {
                refund += other.GetCalculatedSelfCost();
            }
        }

        var baseCost = base.CalculateCostInternal(history, insertPosition);

        return (baseCost.Cost, refund + baseCost.RefundCost);
    }
}
