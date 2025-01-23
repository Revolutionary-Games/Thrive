/// <summary>
///   Stores information after an organelle placement is done so that future placements are faster and don't cause lag
///   spikes with huge cells
/// </summary>
public struct OrganellePlacementSearchCache
{
    public int NextQ;
    public int NextR;

    // When looking for places, empty spots might get skipped that aren't suitable for the current organelle
    // (for example, being too large) that information is stored here when HasHoleLocation
    public int SkippedHoleQ;
    public int SkippedHoleR;

    public bool Initialized;
    public bool HasHoleLocation;
}
