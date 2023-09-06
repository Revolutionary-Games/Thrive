namespace Components
{
    using System.Collections.Generic;

    // TODO: need to actually use this data somewhere
    /// <summary>
    ///   General info about the reproduction status of a creature
    /// </summary>
    public struct ReproductionStatus
    {
        public Dictionary<Compound, float>? MissingCompoundsForBaseReproduction;

        // TODO: remove if unused for now
        public bool ReadyToReproduce;
    }

    public static class ReproductionStatusHelpers
    {
        /// <summary>
        ///   Sets up the base reproduction cost that is on top of the normal costs (for microbes)
        /// </summary>
        public static void SetupRequiredBaseReproductionCompounds(this ref ReproductionStatus reproductionStatus,
            Species species)
        {
            reproductionStatus.MissingCompoundsForBaseReproduction ??= new Dictionary<Compound, float>();

            reproductionStatus.MissingCompoundsForBaseReproduction.Clear();
            reproductionStatus.MissingCompoundsForBaseReproduction.Merge(species.BaseReproductionCost);

            // TODO: there was a line here to reset the multicellular growth needed totals, so whatever calls this will
            // need to handle that in the future
        }
    }
}
