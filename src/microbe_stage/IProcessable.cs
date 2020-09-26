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
    ///   Optional list to track what the ProcessSystem was able to run for this processable entity.
    ///   Used for example to show that processes the player cell is performing
    ///   TODO: this might need additional information tracking to be more useful for process panel implementation
    /// </summary>
    List<TweakedProcess> LastRanProcesses { get; }
}
