using System;
using SharedBase.Archive;

public class OrganelleRemoveActionData : HexRemoveActionData<OrganelleTemplate, CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Used for replacing Cytoplasm. This used to make the action free, but now the species comparison needs special
    ///   logic to detect this.
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
}
