using System.Collections.Generic;

/// <summary>
///   Thing that has processes for ProcessSystem to run
/// </summary>
public interface IProcessable
{
    /// <summary>
    ///   The active processes that ProcessSystem handles
    /// </summary>
    List<TweakedProcess> ActiveProcesses { get; }

    /// <summary>
    ///   Input and output storage for the compounds used in processes
    /// </summary>
    CompoundBag ProcessCompoundStorage { get; }

    /// <summary>
    ///   Optional statistics object to get data out of the process system on what processes it actually ran
    /// </summary>
    ProcessStatistics ProcessStatistics { get; }
}
