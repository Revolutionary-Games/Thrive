using System;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;

public class MetaballResizeActionData<TMetaball> : EditorCombinableActionData, IMetaballAction
    where TMetaball : Metaball
{
    public TMetaball ResizedMetaball;
    public float OldSize;
    public float NewSize;

    public MetaballResizeActionData(TMetaball resizedMetaball, float oldSize, float newSize)
    {
        ResizedMetaball = resizedMetaball;
        OldSize = oldSize;
        NewSize = newSize;
    }

    public override ushort CurrentArchiveVersion => MetaballActionDataSerializer.SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MetaballResizeActionData;

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(ResizedMetaball);
        writer.Write(OldSize);
        writer.Write(NewSize);

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
        if (other is MetaballResizeActionData<TMetaball> resizeActionData)
        {
            if (resizeActionData.ResizedMetaball == ResizedMetaball)
                return true;
        }

        return false;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        if (other is MetaballResizeActionData<TMetaball> resizeActionData)
        {
            if (MathF.Abs(OldSize - resizeActionData.NewSize) < MathUtils.EPSILON)
            {
                OldSize = resizeActionData.NewSize;
                return;
            }

            if (MathF.Abs(NewSize - resizeActionData.OldSize) < MathUtils.EPSILON)
            {
                NewSize = resizeActionData.NewSize;
                return;
            }

            // TODO: this isn't actually fully sensible action
            GD.PrintErr("Verify that this action combine makes sense");
            NewSize = resizeActionData.NewSize;
            OldSize = resizeActionData.OldSize;
            return;
        }

        throw new InvalidOperationException();
    }
}
