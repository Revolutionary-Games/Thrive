namespace Components
{
    /// <summary>
    ///   Entity is an early multicellular thing. Still exists in the microbial environment.
    /// </summary>
    public struct EarlyMulticellularSpeciesMember
    {
        public EarlyMulticellularSpecies Species;

        /// <summary>
        ///  For each part of a multicellular species, the cell type they are must be known
        /// </summary>
        public CellType MulticellularCellType;

        // /// <summary>
        // ///   Set to false if the species is changed
        // /// </summary>
        // public bool SpeciesApplied;
    }
}
