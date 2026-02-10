using SharedBase.Archive;

public abstract class HexPlacementActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex, IArchivable
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_HEX = 1;

    /// <summary>
    ///   Hex placed by this action. Note that the hex is allowed to be modified afterwards, so its position may not
    ///   match!
    /// </summary>
    public THex PlacedHex;

    public Hex Location;
    public int Orientation;

    protected HexPlacementActionData(THex hex, Hex location, int orientation)
    {
        PlacedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(PlacedHex);
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

        // Constructor already read all
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
