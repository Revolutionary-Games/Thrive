using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class MetaballRemoveActionData<TMetaball> : EditorCombinableActionData<LateMulticellularSpecies>
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

    protected override int CalculateCostInternal()
    {
        return Constants.METABALL_REMOVE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this metaball got placed in this session on the same position
        if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
            placementActionData.PlacedMetaball.MatchesDefinition(RemovedMetaball))
        {
            // If this metaball got placed on the same position
            if (placementActionData.Position.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
                placementActionData.Parent == Parent)
                return ActionInterferenceMode.CancelsOut;

            // Removing a metaball and then placing it is a move operation
            return ActionInterferenceMode.Combinable;
        }

        // If this metaball got moved in this session
        if (other is MetaballMoveActionData<TMetaball> moveActionData &&
            moveActionData.MovedMetaball.MatchesDefinition(RemovedMetaball) &&
            moveActionData.NewPosition.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
            moveActionData.NewParent == Parent)
        {
            return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is MetaballPlacementActionData<TMetaball> placementActionData)
        {
            return new MetaballMoveActionData<TMetaball>(placementActionData.PlacedMetaball, Position,
                placementActionData.Position,
                Parent, placementActionData.Parent, MetaballMoveActionData<TMetaball>.UpdateNewMovementPositions(
                    ReParentedMetaballs,
                    placementActionData.Position - Position));
        }

        var moveActionData = (MetaballMoveActionData<TMetaball>)other;
        return new MetaballRemoveActionData<TMetaball>(RemovedMetaball, moveActionData.OldPosition,
            moveActionData.OldParent,
            MetaballMoveActionData<TMetaball>.UpdateOldMovementPositions(ReParentedMetaballs,
                moveActionData.OldPosition - Position));
    }
}
