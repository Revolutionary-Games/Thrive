using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

public class MetaballPlacementActionData<TMetaball> : EditorCombinableActionData
    where TMetaball : Metaball
{
    public TMetaball PlacedMetaball;
    public Vector3 Position;
    public float Size;
    public Metaball? Parent;

    [JsonConstructor]
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
        Parent = metaball.Parent;
    }

    protected override double CalculateBaseCostInternal()
    {
        return Constants.METABALL_ADD_COST;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history,
        int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this metaball got removed in this session
            if (other is MetaballRemoveActionData<TMetaball> removeActionData &&
                removeActionData.RemovedMetaball.MatchesDefinition(PlacedMetaball))
            {
                // If the placed metaball has been placed in the same position where it got removed before
                if (removeActionData.Position.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
                    removeActionData.Parent == Parent)
                {
                    cost = 0;
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }

                // Removing and placing a metaball is a move operation
                cost = Constants.METABALL_MOVE_COST;
                refund += other.GetCalculatedSelfCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
