using System;
using SharedBase.Archive;

public class CellRemoveActionData : HexRemoveActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    public CellRemoveActionData(HexWithData<CellTemplate> hex, Hex location, int orientation) : base(hex, location,
        orientation)
    {
    }

    public CellRemoveActionData(HexWithData<CellTemplate> hex) : base(hex, hex.Position,
        hex.Data?.Orientation ?? throw new ArgumentException("Hex with no data"))
    {
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
            reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, version);

        return instance;
    }
}
