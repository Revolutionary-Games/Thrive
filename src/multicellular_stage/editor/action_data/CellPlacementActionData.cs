using System;
using SharedBase.Archive;

public class CellPlacementActionData : HexPlacementActionData<HexWithData<CellTemplate>, MulticellularSpecies>
{
    public CellPlacementActionData(HexWithData<CellTemplate> hex, Hex location, int orientation) : base(hex, location,
        orientation)
    {
    }

    public CellPlacementActionData(HexWithData<CellTemplate> hex) : base(hex, hex.Position,
        hex.Data?.Orientation ?? throw new ArgumentException("Hex with no data"))
    {
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION_HEX;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.CellPlacementActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.CellPlacementActionData)
            throw new NotSupportedException();

        writer.WriteObject((CellPlacementActionData)obj);
    }

    public static CellPlacementActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        var instance = new CellPlacementActionData(reader.ReadObject<HexWithData<CellTemplate>>(), reader.ReadHex(),
            reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, version);

        return instance;
    }

    protected override double CalculateBaseCostInternal()
    {
        return PlacedHex.Data?.ModifiableCellType.MPCost ?? throw new InvalidOperationException("Hex with no data");
    }
}
