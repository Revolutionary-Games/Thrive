using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class MetaballRemoveActionData<TMetaball> : EditorCombinableActionData
    where TMetaball : Metaball
{
    public TMetaball RemovedMetaball;
    public Vector3 Position;
    public Metaball? Parent;

    /// <summary>
    ///   If any metaballs that were the children of <see cref="RemovedMetaball"/> exist, they need to be moved,
    ///   that movement data is stored here
    /// </summary>
    public List<MetaballMoveActionData<TMetaball>>? ReParentedMetaballs;

    [JsonConstructor]
    public MetaballRemoveActionData(TMetaball metaball, Vector3 position, Metaball? parent,
        List<MetaballMoveActionData<TMetaball>>? reParentedMetaballs)
    {
        RemovedMetaball = metaball;
        Position = position;
        Parent = parent;
        ReParentedMetaballs = reParentedMetaballs;
    }

    public MetaballRemoveActionData(TMetaball metaball, List<MetaballMoveActionData<TMetaball>>? reParentedMetaballs)
    {
        RemovedMetaball = metaball;
        Position = metaball.Position;
        Parent = metaball.Parent;
        ReParentedMetaballs = reParentedMetaballs;
    }

    public static List<MetaballMoveActionData<TMetaball>>? CreateMovementActionForChildren(TMetaball removedMetaball,
        MetaballLayout<TMetaball> descendantData)
    {
        var childMetaballs = descendantData.GetChildrenOf(removedMetaball).ToList();

        if (childMetaballs.Count < 1)
            return null;

        var result = new List<MetaballMoveActionData<TMetaball>>();

        Metaball? parentMetaball = removedMetaball.Parent;

        var descendantList = new List<TMetaball>();

        foreach (var childMetaball in childMetaballs)
        {
            Vector3 newPosition;

            if (parentMetaball != null)
            {
                newPosition = childMetaball.CalculatePositionTouchingParent(childMetaball.DirectionToParent());
            }
            else
            {
                // Move to fill the position the parent leaves
                // This will also become the root as parentMetaball is null
                newPosition = removedMetaball.Position;
            }

            Vector3 movementVector = newPosition - childMetaball.Position;

            if (parentMetaball == childMetaball)
                throw new Exception("logic error in child metaball adjustment action generation");

            // We pass null here as child moves because we handle adding those separately
            result.Add(new MetaballMoveActionData<TMetaball>(childMetaball, childMetaball.Position,
                newPosition, removedMetaball, parentMetaball, null));

            // If the removed metaball doesn't have a parent, the first child metaball will have to become the new root
            parentMetaball ??= childMetaball;

            // All descendants of childMetaball also must be adjusted
            descendantList.Clear();
            descendantData.DescendantsOfAndSelf(descendantList, childMetaball);

            foreach (var descendant in descendantList)
            {
                if (descendant == childMetaball)
                    continue;

                var descendantPosition = descendant.Position + movementVector;

                if (descendantPosition.IsEqualApprox(descendant.Position))
                    continue;

                var descendantParent = descendant.Parent;

                if (descendantParent == removedMetaball)
                    descendantParent = parentMetaball;

                if (descendantParent == descendant)
                    throw new Exception("logic error in child metaball adjustment action generation");

                result.Add(new MetaballMoveActionData<TMetaball>(descendant, descendant.Position,
                    descendantPosition, descendant.Parent, descendantParent, null));
            }
        }

        return result;
    }

    protected override double CalculateBaseCostInternal()
    {
        return Constants.METABALL_REMOVE_COST;
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

            // If this metaball got placed in this session (on the same position)
            if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
                placementActionData.PlacedMetaball.MatchesDefinition(RemovedMetaball))
            {
                // Deleting a placed metaball refunds it
                refund += other.GetCalculatedSelfCost();
                continue;
            }

            // If this metaball got moved in this session, refund that
            if (other is MetaballMoveActionData<TMetaball> moveActionData &&
                moveActionData.MovedMetaball.MatchesDefinition(RemovedMetaball) &&
                moveActionData.NewPosition.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
                moveActionData.NewParent == Parent)
            {
                refund += moveActionData.GetCalculatedEffectiveCost();
                continue;
            }

            // If this metaball got resized in this session, refund that
            if (other is MetaballResizeActionData<TMetaball> resizeActionData &&
                resizeActionData.ResizedMetaball.MatchesDefinition(RemovedMetaball) &&
                Math.Abs(resizeActionData.NewSize - RemovedMetaball.Size) < MathUtils.EPSILON)
            {
                refund += resizeActionData.GetCalculatedEffectiveCost();
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
