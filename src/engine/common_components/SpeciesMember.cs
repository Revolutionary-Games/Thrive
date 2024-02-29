namespace Components
{
    /// <summary>
    ///   General marker for species members to be able to check other members of their species
    /// </summary>
    [ComponentIsReadByDefault]
    [JSONDynamicTypeAllowed]
    public struct SpeciesMember
    {
        /// <summary>
        ///   Access to the full species data. Comparing species should be done with the ID, but this is required here
        ///   as entities need to know various properties about their species for various gameplay purposes.
        /// </summary>
        public Species Species;

        /// <summary>
        ///   ID of the species this is a member of. The <see cref="GameWorld"/> should make sure there can't be
        ///   duplicate IDs. If there are then it is a world or mutation problem. Used as an optimization to quickly
        ///   compare species.
        /// </summary>
        public uint ID;

        public SpeciesMember(Species species)
        {
            Species = species;
            ID = species.ID;
        }
    }
}
