using System.Collections.Generic;

/// <summary>
///   Thing that has processes for ProcessSystem to run
/// </summary>
public interface IProcessable
{
    List<TweakedProcess> ActiveProcesses { get; }
    CompoundBag ProcessCompoundStorage { get; }
}
