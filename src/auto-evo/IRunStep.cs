namespace AutoEvo;

/// <summary>
///   Interface for all auto-evo step types
/// </summary>
public interface IRunStep
{
    /// <summary>
    ///   Total number of steps. As <see cref="RunStep"/> is called, this is allowed to return lower values
    ///   than initially
    /// </summary>
    /// <value>The total steps.</value>
    public int TotalSteps { get; }

    /// <summary>
    ///   If true, this step can be run concurrently with other steps. If false, all previous steps need to finish
    ///   before this can be run.
    /// </summary>
    public bool CanRunConcurrently { get; }

    /// <summary>
    ///   Performs a single step. This needs to be called TotalSteps times
    /// </summary>
    /// <returns>True once the final step is complete</returns>
    /// <param name="results">Results are stored here</param>
    /// <param name="cache">
    ///   Access to a cache where various auto-evo data can be got from efficiently.
    /// </param>
    public bool RunStep(RunResults results, SimulationCache cache);
}
