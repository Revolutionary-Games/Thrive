/// <summary>
///   Spawn customization for multicellular species
/// </summary>
public enum MulticellularSpawnState
{
    /// <summary>
    ///   Might be a single cell, a spore, or a bunch of cells. Depends on the reproduction method.
    /// </summary>
    InitialState = 0,
    /// <summary>
    ///   Just the colony root (a single cell).
    /// </summary>
    ColonyRoot,
    FullColony,
    ChanceForFullColony,
}
