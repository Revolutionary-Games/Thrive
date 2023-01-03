using System.Collections.Generic;

/// <summary>
///   Thing that has processes for ProcessSystem to run
/// </summary>
public interface IProcessable
{
    /// <summary>
    ///   The active processes that ProcessSystem handles
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     All processes that perform the same action should be combined together rather than listing that process
    ///     multiple times in this list (as that results in unexpected things as that isn't semantically how this
    ///     property is meant to be structured)
    ///   </para>
    /// </remarks>
    public List<TweakedProcess> ActiveProcesses { get; }

    /// <summary>
    ///   Input and output storage for the compounds used in processes
    /// </summary>
    public CompoundBag ProcessCompoundStorage { get; }

    /// <summary>
    ///   Optional statistics object to get data out of the process system on what processes it actually ran
    /// </summary>
    public ProcessStatistics? ProcessStatistics { get; }
}
