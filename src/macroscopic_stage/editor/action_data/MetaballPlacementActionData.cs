using Godot;
using Saving.Serializers;
using SharedBase.Archive;

public class MetaballPlacementActionData<TMetaball> : EditorCombinableActionData, IMetaballAction
    where TMetaball : Metaball
{
    public TMetaball PlacedMetaball;
    public Vector3 Position;
    public float Size;
    public Metaball? Parent;

    public MetaballPlacementActionData(TMetaball metaball, Vector3 position, float size, Metaball? parent)
    {
        PlacedMetaball = metaball;
        Position = position;
        Size = size;
        Parent = parent;
    }

    public MetaballPlacementActionData(TMetaball metaball)
    {
        PlacedMetaball = metaball;
        Position = metaball.Position;
        Size = metaball.Size;
        Parent = metaball.ModifiableParent;
    }

    public override ushort CurrentArchiveVersion => MetaballActionDataSerializer.SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MetaballPlacementActionData;

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(PlacedMetaball);
        writer.Write(Position);
        writer.Write(Size);
        writer.WriteObjectOrNull(Parent);

        writer.Write(SERIALIZATION_VERSION_EDITOR);
        base.WriteToArchive(writer);
    }

    public void FinishBaseLoad(ISArchiveReader reader, ushort version)
    {
        if (version == 0 || version > CurrentArchiveVersion)
            throw new InvalidArchiveVersionException(version, CurrentArchiveVersion);

        ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
