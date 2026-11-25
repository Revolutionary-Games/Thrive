using System;

/// <summary>
///   Functional roles a cell can cover in a multicellular body plan or a macroscopic body. These are derived from the
///   organelles of the cell, which keeps them compatible with every existing save as no new data is stored.
/// </summary>
[Flags]
public enum CellFunctionalRole : uint
{
    /// <summary>
    ///   No specialised role, a cell that only has basic organelles.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Cell can move itself or the colony (flagella and cilia).
    /// </summary>
    Movement = 1,

    /// <summary>
    ///   Cell can defend the colony (pili and toxin vacuoles).
    /// </summary>
    Defense = 2,

    /// <summary>
    ///   Cell can sense the environment (chemoreceptors).
    /// </summary>
    Sensing = 4,

    /// <summary>
    ///   Cell increases storage capacity (vacuoles).
    /// </summary>
    Storage = 8,

    /// <summary>
    ///   Cell produces glucose from sunlight (photoreceptor organelles).
    /// </summary>
    Photosynthesis = 16,
}
