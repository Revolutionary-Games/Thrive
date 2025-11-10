using System.Collections.Generic;
using SharedBase.Archive;

public abstract class HexRemoveActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex, IArchivable
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_HEX = 1;

    public THex RemovedHex;
    public Hex Location;
    public int Orientation;

    protected HexRemoveActionData(THex hex, Hex location, int orientation)
    {
        RemovedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(RemovedHex);
        writer.Write(Location);
        writer.Write(Orientation);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_HEX or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_HEX);

        // Base version is different
        base.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());
    }

    protected override double CalculateBaseCostInternal()
    {
        return Constants.ORGANELLE_REMOVE_COST;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        bool placementRefunded = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this hex got placed in this session (and was at the same place so that we don't mix unrelated
            // organelles)
            if (other is HexPlacementActionData<THex, TContext> placementActionData &&
                placementActionData.PlacedHex.MatchesDefinition(RemovedHex) && MatchesContext(placementActionData))
            {
                // If removed something that was placed
                var placementFinalLocation = HexMoveActionData<THex, TContext>
                    .ResolveFinalLocation(placementActionData.PlacedHex,
                        placementActionData.Location, placementActionData.Orientation, history, i + 1,
                        insertPosition, Context).Location;
                if (Location == placementActionData.Location || Location == placementFinalLocation)
                {
                    // Check if there's a remove that has already taken advantage of this, and if that is the case,
                    // don't refund
                    bool conflict2 = false;
                    for (int j = i + 1; j < insertPosition && j < count; ++j)
                    {
                        var other2 = history[j];

                        if (other2 is HexRemoveActionData<THex, TContext> removeActionData &&
                            removeActionData.RemovedHex.MatchesDefinition(RemovedHex) &&
                            MatchesContext(removeActionData))
                        {
                            if (removeActionData.Location == Location ||
                                removeActionData.Location == placementFinalLocation)
                            {
                                conflict2 = true;
                                break;
                            }
                        }
                    }

                    if (!conflict2)
                    {
                        cost = 0;

                        if (!placementRefunded)
                        {
                            refund += other.GetAndConsumeAvailableRefund();
                            placementRefunded = true;
                        }
                    }

                    continue;
                }

                // Or if removed from another position, then this counts as a move, as long as there are no other
                // conflicting actions in-between
                bool conflict = false;
                for (int j = i + 1; j < insertPosition && j < count; ++j)
                {
                    var other2 = history[i];

                    if (other2 is HexPlacementActionData<THex, TContext> placementActionData2 &&
                        placementActionData2.PlacedHex.MatchesDefinition(RemovedHex) &&
                        MatchesContext(placementActionData2))
                    {
                        {
                            conflict = true;
                            break;
                        }
                    }

                    if (other2 is HexRemoveActionData<THex, TContext> removeActionData &&
                        removeActionData.RemovedHex.MatchesDefinition(RemovedHex) && MatchesContext(removeActionData))
                    {
                        if (removeActionData.Location == Location)
                        {
                            conflict = true;
                            break;
                        }
                    }
                }

                if (!conflict)
                {
                    refund += other.GetAndConsumeAvailableRefund();
                    cost = Constants.ORGANELLE_MOVE_COST;
                    continue;
                }
            }

            // If this hex got moved in this session, refund the move cost
            if (other is HexMoveActionData<THex, TContext> moveActionData &&
                moveActionData.MovedHex.MatchesDefinition(RemovedHex) &&
                moveActionData.NewLocation == Location && MatchesContext(moveActionData))
            {
                refund += other.GetAndConsumeAvailableRefund() /*- other.GetCalculatedRefundCost()*/;
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
