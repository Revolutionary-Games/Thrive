using System;
using SharedBase.Archive;

public class CellRemoveActionData : HexRemoveActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    public new const ushort SERIALIZATION_VERSION_HEX = 2;

    public int MassBuddingCellCount;

    public CellRemoveActionData(HexWithData<CellTemplate> hex, Hex location, int orientation, int massBuddingCellCount)
        : base(hex, location, orientation)
    {
        MassBuddingCellCount = massBuddingCellCount;
    }

    public CellRemoveActionData(HexWithData<CellTemplate> hex, int massBuddingCellCount) : base(hex, hex.Position,
        hex.Data?.Orientation ?? throw new ArgumentException("Hex with no data"))
    {
        MassBuddingCellCount = massBuddingCellCount;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_HEX;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellRemoveActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellRemoveActionData)
            throw new NotSupportedException();

        writer.WriteObject((CellRemoveActionData)obj);
    }

    public static CellRemoveActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        var instance = new CellRemoveActionData(reader.ReadObject<HexWithData<CellTemplate>>(), reader.ReadHex(),
            reader.ReadInt32(), SERIALIZATION_VERSION_HEX >= 2 ? reader.ReadInt32() : 1);

        instance.ReadBasePropertiesFromArchive(reader, version);

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(RemovedHex);
        writer.Write(Location);
        writer.Write(Orientation);
        writer.Write(MassBuddingCellCount);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }
}
