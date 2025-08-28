using System;
using System.Collections.Generic;
using Godot;

public class MetaballResizeActionData<TMetaball> : EditorCombinableActionData
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

    protected override double CalculateBaseCostInternal()
    {
        if (Math.Abs(OldSize - NewSize) < MathUtils.EPSILON)
            return 0;

        // TODO: scale cost based on the size change (also change in the below method)
        return Constants.METABALL_RESIZE_COST;
    }

    protected override double CalculateCostInternal(IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();

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
                    cost = Math.Min(-other.GetCalculatedCost(), cost);
                    continue;
                }

                // Multiple resizes in a row are just one resize
                // TODO: once there's a scaled cost, this needs to be updated
                cost = Math.Min(0, cost);
                continue;
            }

            if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
                placementActionData.PlacedMetaball.Equals(ResizedMetaball))
            {
                // Resizing a just placed metaball is free
                cost = Math.Min(0, cost);
            }
        }

        return cost;
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
