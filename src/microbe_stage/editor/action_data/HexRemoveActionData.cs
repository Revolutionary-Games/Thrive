using SharedBase.Archive;

public abstract class HexRemoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex, IArchivable
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_HEX = 1;

    public THex RemovedHex;
    public Hex Location;
    public int Orientation;

    protected HexRemoveActionData(THex hex, Hex location, int orientation)
    {
        RemovedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(RemovedHex);
        writer.Write(Location);
        writer.Write(Orientation);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        // Base version is different
        base.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
