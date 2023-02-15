namespace AutoEvo
{
    /// <summary>
    ///   Interface for all auto-evo step types
    /// </summary>
    public interface IRunStep
    {
        /// <summary>
        ///   Total number of steps. As step is called this is allowed to return lower values
        ///   than initially
        /// </summary>
        /// <value>The total steps.</value>
        public int TotalSteps { get; }

        /// <summary>
        ///   If true this step can be ran concurrently with other steps. If false all previous steps need to finish
        ///   before this can be ran.
        /// </summary>
        public bool CanRunConcurrently { get; }

        /// <summary>
        ///   Performs a single step. This needs to be called TotalSteps times
        /// </summary>
        /// <returns>True once final step is complete</returns>
        /// <param name="results">Results are stored here</param>
        public bool RunStep(RunResults results);
    }
}
