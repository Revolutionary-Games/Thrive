namespace AutoEvo
{
    /// <summary>
    ///   Like a SpeciesRecord, but with guaranteed non-null species data. Created when recreating game history from a
    ///   save.
    /// </summary>
    public class SpeciesRecordFull
    {
        public SpeciesRecordFull(Species species,
            long population,
            uint? mutatedPropertiesID = null,
            uint? splitFromID = null)
        {
            Species = species;
            Population = population;
            MutatedPropertiesID = mutatedPropertiesID;
            SplitFromID = splitFromID;
        }

        /// <summary>
        ///   Full species data for this species.
        /// </summary>
        public Species Species { get; set; }

        /// <summary>
        ///   Species population for this generation.
        /// </summary>
        public long Population { get; private set; }

        /// <summary>
        ///   ID of the species this species mutated from. If null, this species did not mutate this generation.
        /// </summary>
        public uint? MutatedPropertiesID { get; private set; }

        /// <summary>
        ///   ID of the species this species speciated from. If null, this species did not appear this generation.
        /// </summary>
        public uint? SplitFromID { get; private set; }
    }
}
