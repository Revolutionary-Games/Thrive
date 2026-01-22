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
}
