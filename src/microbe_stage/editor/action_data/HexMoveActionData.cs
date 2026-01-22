using SharedBase.Archive;

public abstract class HexMoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex, IArchivable
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_HEX = 1;

    public THex MovedHex;
    public Hex OldLocation;
    public Hex NewLocation;
    public int OldRotation;
    public int NewRotation;

    protected HexMoveActionData(THex hex, Hex oldLocation, Hex newLocation, int oldRotation,
        int newRotation)
    {
        MovedHex = hex;
        OldLocation = oldLocation;
        NewLocation = newLocation;
        OldRotation = oldRotation;
        NewRotation = newRotation;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(MovedHex);
        writer.Write(OldLocation);
        writer.Write(NewLocation);
        writer.Write(OldRotation);
        writer.Write(NewRotation);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        // Base version is different
        base.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        // Properties are read for the constructor already
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
