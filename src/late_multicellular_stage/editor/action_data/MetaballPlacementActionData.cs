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

    protected override int CalculateCostInternal()
    {
        return Constants.METABALL_ADD_COST;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        // If this metaball got removed in this session
        if (other is MetaballRemoveActionData<TMetaball> removeActionData &&
            removeActionData.RemovedMetaball.MatchesDefinition(PlacedMetaball))
        {
            // If the placed metaball has been placed on the same position where it got removed before
            if (removeActionData.Position.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
                removeActionData.Parent == Parent)
                return ActionInterferenceMode.CancelsOut;

            // Removing and placing a metaball is a move operation
            return ActionInterferenceMode.Combinable;
        }

        // If this metaball got resized in this session
        // TODO: is the Equals check here too strict or just right?
        if (other is MetaballResizeActionData<TMetaball> resizeActionData &&
            resizeActionData.ResizedMetaball.Equals(PlacedMetaball))
        {
            // Placing and resizing a metaball is a place operation
            return ActionInterferenceMode.Combinable;
        }

        if (other is MetaballMoveActionData<TMetaball> moveActionData &&
            moveActionData.MovedMetaball.MatchesDefinition(PlacedMetaball))
        {
            if (moveActionData.OldPosition.DistanceSquaredTo(Position) < MathUtils.EPSILON &&
                moveActionData.OldParent == Parent)
                return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        if (other is MetaballRemoveActionData<TMetaball> removeActionData)
        {
            return new MetaballMoveActionData<TMetaball>(removeActionData.RemovedMetaball, removeActionData.Position,
                Position, removeActionData.Parent, Parent,
                MetaballMoveActionData<TMetaball>.UpdateNewMovementPositions(removeActionData.ReParentedMetaballs,
                    Position - removeActionData.Position));
        }

        if (other is MetaballResizeActionData<TMetaball> resizeActionData)
        {
            return new MetaballPlacementActionData<TMetaball>(PlacedMetaball, Position, resizeActionData.NewSize,
                Parent);
        }

        var moveActionData = (MetaballMoveActionData<TMetaball>)other;
        return new MetaballPlacementActionData<TMetaball>(PlacedMetaball, moveActionData.NewPosition, Size,
            moveActionData.NewParent);
    }
}
