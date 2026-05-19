using System;
using SharedBase.Archive;

public class MassBuddingCellCountActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public int OldCellCount;
    public int NewCellCount;

    private int maxCellCount;

    public MassBuddingCellCountActionData(int oldCellCount, int newCellCount, int maxCellCount)
    {
        OldCellCount = oldCellCount;
        NewCellCount = newCellCount;
        this.maxCellCount = maxCellCount;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MassBuddingCellCountActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.MassBuddingCellCountActionData)
            throw new NotSupportedException();

        writer.WriteObject((MassBuddingCellCountActionData)obj);
    }

    public static MassBuddingCellCountActionData ReadFromArchive(ISArchiveReader reader, ushort version,
        int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new MassBuddingCellCountActionData(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(OldCellCount);
        writer.Write(NewCellCount);
        writer.Write(maxCellCount);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return other is MassBuddingCellCountActionData otherCellCountData
            && otherCellCountData.maxCellCount == maxCellCount;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var otherCellCountData = (MassBuddingCellCountActionData)other;

        if (OldCellCount == otherCellCountData.NewCellCount)
        {
            // Handle cancels out
            if (NewCellCount == otherCellCountData.OldCellCount)
            {
                NewCellCount = otherCellCountData.NewCellCount;
                return;
            }

            OldCellCount = otherCellCountData.OldCellCount;
            return;
        }

        NewCellCount = otherCellCountData.NewCellCount;
    }
}
