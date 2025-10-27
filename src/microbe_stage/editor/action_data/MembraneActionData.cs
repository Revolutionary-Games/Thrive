using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class MembraneActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MembraneActionData;

    public static double CalculateCost(MembraneType oldMembrane, MembraneType newMembrane)
    {
        if (oldMembrane == newMembrane)
            return 0;

        return newMembrane.EditorCost;
    }

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MembraneActionData)
            throw new NotSupportedException();

        writer.WriteObject((MembraneActionData)obj);
    }

    public static MembraneActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MembraneActionData(reader.ReadObject<MembraneType>(), reader.ReadObject<MembraneType>());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(OldMembrane);
        writer.WriteObject(NewMembrane);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override double CalculateBaseCostInternal()
    {
        return CalculateCost(OldMembrane, NewMembrane);
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;
        bool seenOther = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If the membrane got changed again
            if (other is MembraneActionData membraneActionData && MatchesContext(membraneActionData))
            {
                if (!seenOther)
                {
                    seenOther = true;
                    cost = CalculateCost(membraneActionData.OldMembrane, NewMembrane);
                }

                refund += other.GetCalculatedSelfCost() - other.GetCalculatedRefundCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
