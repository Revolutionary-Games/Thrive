namespace AutoEvo
{
    /// <summary>
    ///   Species mutation and population data from a single generation.
    /// </summary>
    public abstract class SpeciesRecord
    {
        /// <summary>
        ///   Species population for this generation.
        /// </summary>
        public long Population { get; protected set; }

        /// <summary>
        ///   ID of the species this species mutated from. If null, this species did not mutate this generation.
        /// </summary>
        public uint? MutatedPropertiesID { get; protected set; }

        /// <summary>
        ///   ID of the species this species speciated from. If null, this species did not appear this generation.
        /// </summary>
        public uint? SplitFromID { get; protected set; }
    }
}
