namespace Components
{
    /// <summary>
    ///   A collection place for various microbe status flags and variables that don't have more sensible components
    ///   to put them in
    /// </summary>
    public struct MicrobeStatus
    {
        public float LastCheckedATPDamage;

        public float LastCheckedOxytoxyDigestionDamage;

        public float LastCheckedReproduction;

        public float TimeUntilChemoreceptionUpdate;
        public float TimeUntilDigestionUpdate;

        /// <summary>
        ///   Flips every reproduction update. Used to make compound use for reproduction distribute more evenly between
        ///   the compound types.
        /// </summary>
        public bool ConsumeReproductionCompoundsReverse;
    }
}
