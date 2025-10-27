using System.Collections.Generic;
using System.Linq;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;

public class MetaballMoveActionData<TMetaball> : EditorCombinableActionData, IMetaballAction
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

    public override ushort CurrentArchiveVersion => MetaballActionDataSerializer.SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.MetaballMoveActionData;

    public static List<MetaballMoveActionData<TMetaball>>? CreateMovementActionForChildren(TMetaball movedMetaball,
        Vector3 oldPosition, Vector3 newPosition, MetaballLayout<TMetaball> descendantData)
    {
        var descendantList = new List<TMetaball>();
        descendantData.DescendantsOfAndSelf(descendantList, movedMetaball);

        var movementVector = newPosition - oldPosition;

        // As the descendant list always includes self, 2 is the minimum size to have some items
        if (descendantList.Count < 2 || movementVector.IsEqualApprox(Vector3.Zero))
            return null;

        var result = new List<MetaballMoveActionData<TMetaball>>();

        foreach (var descendant in descendantList)
        {
            // Skip the self that is always in the descendant list
            if (ReferenceEquals(descendant, movedMetaball))
                continue;

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

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(MovedMetaball);
        writer.Write(OldPosition);
        writer.Write(NewPosition);
        writer.WriteObjectOrNull(OldParent);
        writer.WriteObjectOrNull(NewParent);
        writer.WriteObjectOrNull(MovedChildMetaballs);

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
        if (OldPosition.DistanceSquaredTo(NewPosition) < MathUtils.EPSILON && OldParent == NewParent)
            return 0;

        return Constants.METABALL_MOVE_COST;
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

            // If this metaball got moved in the same session again
            if (other is MetaballMoveActionData<TMetaball> moveActionData &&
                moveActionData.MovedMetaball.MatchesDefinition(MovedMetaball))
            {
                // If this metaball got moved back and forth
                if (OldPosition.DistanceSquaredTo(moveActionData.NewPosition) < MathUtils.EPSILON &&
                    NewPosition.DistanceSquaredTo(moveActionData.OldPosition) < MathUtils.EPSILON &&
                    OldParent == moveActionData.NewParent && NewParent == moveActionData.OldParent)
                {
                    cost = 0;
                    refund += moveActionData.GetCalculatedSelfCost();
                    continue;
                }

                // If this metaball got moved twice
                if ((moveActionData.NewPosition.DistanceSquaredTo(OldPosition) < MathUtils.EPSILON &&
                        moveActionData.NewParent == OldParent) ||
                    (NewPosition.DistanceSquaredTo(moveActionData.OldPosition) < MathUtils.EPSILON &&
                        NewParent == moveActionData.OldParent))
                {
                    cost = 0;
                    continue;
                }
            }

            // If this metaball got placed in this session
            if (other is MetaballPlacementActionData<TMetaball> placementActionData &&
                placementActionData.PlacedMetaball.MatchesDefinition(MovedMetaball) &&
                placementActionData.Position == OldPosition &&
                placementActionData.Parent == OldParent)
            {
                cost = 0;
            }

            // Moves shouldn't happen after a remove, so we don't check that here
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
