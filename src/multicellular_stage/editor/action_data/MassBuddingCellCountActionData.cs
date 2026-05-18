using System;
using SharedBase.Archive;

/// <summary>
///   Action data for changing mass budding initial cell count
/// </summary>
public class MassBuddingCellCountActionData : EditorCombinableActionData<MulticellularSpecies>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public int OldCellCount;
    public int NewCellCount;

    public MassBuddingCellCountActionData(int previousCellCount, int newCellCount)
    {
        OldCellCount = previousCellCount;
        NewCellCount = newCellCount;
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

        var instance = new MassBuddingCellCountActionData(reader.ReadInt32(), reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(OldCellCount);
        writer.Write(NewCellCount);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
