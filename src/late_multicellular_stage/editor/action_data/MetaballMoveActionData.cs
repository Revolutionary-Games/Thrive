using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

public class MetaballMoveActionData<TMetaball> : EditorCombinableActionData
    where TMetaball : Metaball
{
    public TMetaball MovedMetaball;
    public Vector3 OldPosition;
    public Vector3 NewPosition;
    public Metaball? OldParent;
    public Metaball? NewParent;

    /// <summary>
    ///   Moved metaballs that are children of <see cref="MovedMetaball"/> that also needed to move are stored here
    /// </summary>
    public List<MetaballMoveActionData<TMetaball>>? MovedChildMetaballs;

    [JsonConstructor]
    public MetaballMoveActionData(TMetaball metaball, Vector3 oldPosition, Vector3 newPosition, Metaball? oldParent,
        Metaball? newParent, List<MetaballMoveActionData<TMetaball>>? movedChildMetaballs)
    {
        MovedMetaball = metaball;
        OldPosition = oldPosition;
        NewPosition = newPosition;
        OldParent = oldParent;
        NewParent = newParent;
        MovedChildMetaballs = movedChildMetaballs;
    }

    public static List<MetaballMoveActionData<TMetaball>>? CreateMovementActionForChildren(TMetaball movedMetaball,
        Vector3 oldPosition, Vector3 newPosition, MetaballLayout<TMetaball> descendantData)
    {
        var descendantList = new List<TMetaball>();
        descendantData.DescendantsOfAndSelf(descendantList, movedMetaball);

        var movementVector = newPosition - oldPosition;

        if (descendantList.Count < 1 || movementVector.IsEqualApprox(Vector3.Zero))
            return null;

        var result = new List<MetaballMoveActionData<TMetaball>>();

        foreach (var descendant in descendantList)
        {
            var descendantPosition = descendant.Position + movementVector;

            if (descendantPosition.IsEqualApprox(descendant.Position))
                continue;

            result.Add(new MetaballMoveActionData<TMetaball>(descendant, descendant.Position,
                descendantPosition, descendant.Parent, descendant.Parent, null));
        }

        return result;
    }

    public static List<MetaballMoveActionData<TMetaball>>? UpdateOldMovementPositions(
        IEnumerable<MetaballMoveActionData<TMetaball>>? movements, Vector3 offset)
    {
        return movements?.Select(m => new MetaballMoveActionData<TMetaball>(m.MovedMetaball, m.OldPosition + offset,
            m.NewPosition, m.OldParent, m.NewParent, null)).ToList();
    }

    public static List<MetaballMoveActionData<TMetaball>>? UpdateNewMovementPositions(
        IEnumerable<MetaballMoveActionData<TMetaball>>? movements, Vector3 offset)
    {
        return movements?.Select(m => new MetaballMoveActionData<TMetaball>(m.MovedMetaball, m.OldPosition,
            m.NewPosition + offset, m.OldParent, m.NewParent, null)).ToList();
    }

    protected override int CalculateCostInternal()
    {
        if (OldPosition.DistanceSquaredTo(NewPosition) < MathUtils.EPSILON && OldParent == NewParent)
            return 0;

        return Constants.METABALL_MOVE_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this metaball got moved in the same session again
        if (other is MetaballMoveActionData<TMetaball> moveActionData &&
            moveActionData.MovedMetaball.MatchesDefinition(MovedMetaball))
        {
            // If this metaball got moved back and forth
            if (OldPosition.DistanceSquaredTo(moveActionData.NewPosition) < MathUtils.EPSILON &&
                NewPosition.DistanceSquaredTo(moveActionData.OldPosition) < MathUtils.EPSILON &&
                OldParent == moveActionData.NewParent && NewParent == moveActionData.OldParent)
            {
                return ActionInterferenceMode.CancelsOut;
            }

            // If this metaball got moved twice
            if ((moveActionData.NewPosition.DistanceSquaredTo(OldPosition) < MathUtils.EPSILON &&
                    moveActionData.NewParent == OldParent) ||
                (NewPosition.DistanceSquaredTo(moveActionData.OldPosition) < MathUtils.EPSILON &&
                    NewParent == moveActionData.OldParent))
            {
                return ActionInterferenceMode.Combinable;
            }
        }

        // If this metaball got placed in this session
        if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
            placementActionData.PlacedMetaball.MatchesDefinition(MovedMetaball) &&
            placementActionData.Position == OldPosition &&
            placementActionData.Parent == OldParent)
        {
            return ActionInterferenceMode.Combinable;
        }

        // If this metaball got removed in this session
        if (other is MetaballRemoveActionData<TMetaball> removeActionData &&
            removeActionData.RemovedMetaball.MatchesDefinition(MovedMetaball) &&
            removeActionData.Position == NewPosition && removeActionData.Parent == NewParent)
        {
            return ActionInterferenceMode.ReplacesOther;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        switch (other)
        {
            case MetaballPlacementActionData<TMetaball> placementActionData:
                return new MetaballPlacementActionData<TMetaball>(placementActionData.PlacedMetaball, NewPosition,
                    placementActionData.Size, NewParent);
            case MetaballMoveActionData<TMetaball> moveActionData
                when moveActionData.NewPosition.DistanceSquaredTo(OldPosition) < MathUtils.EPSILON:
                return new MetaballMoveActionData<TMetaball>(MovedMetaball, moveActionData.OldPosition, NewPosition,
                    moveActionData.OldParent, NewParent,
                    UpdateOldMovementPositions(MovedChildMetaballs, moveActionData.OldPosition - OldPosition));
            case MetaballMoveActionData<TMetaball> moveActionData:
                return new MetaballMoveActionData<TMetaball>(moveActionData.MovedMetaball, OldPosition,
                    moveActionData.NewPosition, OldParent, moveActionData.NewParent,
                    UpdateNewMovementPositions(MovedChildMetaballs, moveActionData.NewPosition - NewPosition));
            default:
                throw new NotSupportedException();
        }
    }
}
