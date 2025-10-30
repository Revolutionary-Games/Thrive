using System.Collections.Generic;
using SharedBase.Archive;

public abstract class HexPlacementActionData<THex, TContext> : EditorCombinableActionData<TContext>
    where THex : class, IActionHex, IArchivable
    where TContext : IArchivable
{
    public const ushort SERIALIZATION_VERSION_HEX = 1;

    public THex PlacedHex;
    public Hex Location;
    public int Orientation;

    protected HexPlacementActionData(THex hex, Hex location, int orientation)
    {
        PlacedHex = hex;
        Location = location;
        Orientation = orientation;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(PlacedHex);
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

        // Constructor already read all
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;

        bool seenEarlierPlacement = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            // If this has been placed before, the cost is not free to avoid exploits where the player places and
            // deletes something they then want to place for free. This stops that exploit.
            if (other is HexPlacementActionData<THex, TContext> placementActionData &&
                placementActionData.PlacedHex.MatchesDefinition(PlacedHex) &&
                MatchesContext(placementActionData) &&

                // Matches the initial position or the final position of the earlier placement
                ((placementActionData.Location == Location && placementActionData.Orientation == Orientation) ||
                    (Location, Orientation) ==
                    HexMoveActionData<THex, TContext>.ResolveFinalLocation(placementActionData.PlacedHex,
                        placementActionData.Location, placementActionData.Orientation, history, i + 1,
                        insertPosition - 1, Context)))
            {
                seenEarlierPlacement = true;
                continue;
            }

            // If this hex got removed in this session before being placed again
            if (other is HexRemoveActionData<THex, TContext> removeActionData &&
                removeActionData.RemovedHex.MatchesDefinition(PlacedHex) && MatchesContext(removeActionData))
            {
                // If the placed hex has been placed in the same position where it got removed from before
                if (removeActionData.Location == Location)
                {
                    if (!seenEarlierPlacement)
                        cost = 0;

                    refund += other.GetCalculatedSelfCost();
                }
                else
                {
                    // Removing and placing a hex is a move operation
                    cost = Constants.ORGANELLE_MOVE_COST;
                    refund += other.GetCalculatedSelfCost();
                }
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
