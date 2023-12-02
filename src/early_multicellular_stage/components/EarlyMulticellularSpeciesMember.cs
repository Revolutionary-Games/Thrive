namespace Components
{
    using System;

    /// <summary>
    ///   Entity is an early multicellular thing. Still exists in the microbial environment.
    /// </summary>
    [JSONDynamicTypeAllowed]
    public struct EarlyMulticellularSpeciesMember
    {
        public EarlyMulticellularSpecies Species;

        /// <summary>
        ///   For each part of a multicellular species, the cell type they are must be known
        /// </summary>
        public CellType MulticellularCellType;

        /// <summary>
        ///   Used to keep track of which part of a body plan a non-first cell in a multicellular colony is.
        ///   This is required for regrowing after losing a cell. This is the index of
        ///   <see cref="MulticellularCellType"/> in the <see cref="EarlyMulticellularSpecies.Cells"/>
        /// </summary>
        public int MulticellularBodyPlanPartIndex;

        // /// <summary>
        // ///   Set to false if the species is changed
        // /// </summary>
        // public bool SpeciesApplied;

        public EarlyMulticellularSpeciesMember(EarlyMulticellularSpecies species, CellType cellType)
        {
            Species = species;
            MulticellularCellType = cellType;

            MulticellularBodyPlanPartIndex = species.CellTypes.FindIndex(c => c == cellType);

            if (MulticellularBodyPlanPartIndex == -1)
            {
                MulticellularBodyPlanPartIndex = 0;

#if DEBUG
                throw new ArgumentException("Multicellular growth given invalid first cell type");
#endif
            }
        }
    }
}
