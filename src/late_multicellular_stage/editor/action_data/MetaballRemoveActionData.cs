using Godot;
using Newtonsoft.Json;

public class MetaballRemoveActionData<TMetaball> : EditorCombinableActionData
    where TMetaball : Metaball
{
    public TMetaball RemovedMetaball;
    public Vector3 Position;
    public Metaball? Parent;

    [JsonConstructor]
    public MetaballRemoveActionData(TMetaball metaball, Vector3 position, Metaball? parent)
    {
        RemovedMetaball = metaball;
        Position = position;
        Parent = parent;
    }

    public MetaballRemoveActionData(TMetaball metaball)
    {
        RemovedMetaball = metaball;
        Position = metaball.Position;
        Parent = metaball.Parent;
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
                Parent, placementActionData.Parent);
        }

        var moveActionData = (MetaballMoveActionData<TMetaball>)other;
        return new MetaballRemoveActionData<TMetaball>(RemovedMetaball, moveActionData.OldPosition,
            moveActionData.OldParent);
    }
}
