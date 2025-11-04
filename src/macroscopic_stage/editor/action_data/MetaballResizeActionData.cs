using System;
using System.Collections.Generic;
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

    protected override double CalculateBaseCostInternal()
    {
        if (Math.Abs(OldSize - NewSize) < MathUtils.EPSILON)
            return 0;

        // TODO: scale cost based on the size change (also change in the below method)
        return Constants.METABALL_RESIZE_COST;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this metaball got resized again in this session on the same position
            if (other is MetaballResizeActionData<TMetaball> resizeActionData &&
                resizeActionData.ResizedMetaball.Equals(ResizedMetaball))
            {
                // If this metaball got resized to the old size
                if (MathF.Abs(OldSize - resizeActionData.NewSize) < MathUtils.EPSILON &&
                    MathF.Abs(NewSize - resizeActionData.OldSize) < MathUtils.EPSILON)
                {
                    cost = 0;
                    refund += other.GetAndConsumeAvailableRefund();
                    continue;
                }

                // Multiple resizes in a row are just one resize
                cost = 0;
                continue;
            }

            if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
                placementActionData.PlacedMetaball.Equals(ResizedMetaball))
            {
                // Resizing a just placed metaball is free
                cost = 0;
            }
        }

        return (cost, refund);
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
