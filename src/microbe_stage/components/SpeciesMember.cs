namespace Components
{
    /// <summary>
    ///   Entity is a member of a species and has species related data applied to it
    /// </summary>
    public struct SpeciesMember
    {
        public MicrobeSpecies Species;

        /// <summary>
        ///   Set to false if the species is changed and this entity needs fresh new initialization from the species
        ///   data
        /// </summary>
        public bool SpeciesApplied;
    }
}
