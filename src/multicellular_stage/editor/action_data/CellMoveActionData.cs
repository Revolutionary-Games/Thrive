using System;
using SharedBase.Archive;

public class CellMoveActionData : HexMoveActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    public CellMoveActionData(HexWithData<CellTemplate> organelle, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation) : base(organelle, oldLocation, newLocation, oldRotation, newRotation)
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_HEX;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellMoveActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellMoveActionData)
            throw new NotSupportedException();

        writer.WriteObject((CellMoveActionData)obj);
    }

    public static CellMoveActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        var instance = new CellMoveActionData(reader.ReadObject<HexWithData<CellTemplate>>(), reader.ReadHex(),
            reader.ReadHex(), reader.ReadInt32(), reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, version);

        return instance;
    }
}
