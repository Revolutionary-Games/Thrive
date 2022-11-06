namespace AutoEvo
{
    /// <summary>
    ///   Like a SpeciesRecord, but with guaranteed non-null species data. Created when recreating game history from a
    ///   save.
    /// </summary>
    public class SpeciesRecordFull : SpeciesRecord
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
    }
}
