using System;
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

    protected override int CalculateCostInternal()
    {
        if (Mathf.Abs(OldSize - NewSize) < MathUtils.EPSILON)
            return 0;

        return Constants.METABALL_RESIZE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this metaball got resized again in this session on the same position
        if (other is MetaballResizeActionData<TMetaball> resizeActionData &&
            resizeActionData.ResizedMetaball.Equals(ResizedMetaball))
        {
            // If this metaball got resized to the old size
            if (Mathf.Abs(OldSize - resizeActionData.NewSize) < MathUtils.EPSILON &&
                Mathf.Abs(NewSize - resizeActionData.OldSize) < MathUtils.EPSILON)
                return ActionInterferenceMode.CancelsOut;

            // Multiple resizes in a row is just one resize
            return ActionInterferenceMode.Combinable;
        }

        // If this metaball got removed later in this session
        if (other is MetaballRemoveActionData<TMetaball> removeActionData &&
            removeActionData.RemovedMetaball.Equals(ResizedMetaball))
        {
            return ActionInterferenceMode.ReplacesOther;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is MetaballResizeActionData<TMetaball> resizeActionData)
        {
            if (Mathf.Abs(OldSize - resizeActionData.OldSize) < MathUtils.EPSILON)
            {
                return new MetaballResizeActionData<TMetaball>(ResizedMetaball, OldSize, resizeActionData.NewSize);
            }

            return new MetaballResizeActionData<TMetaball>(ResizedMetaball, resizeActionData.OldSize, NewSize);
        }

        throw new InvalidOperationException();
    }
}
