using System.Collections.Generic;

/// <summary>
///   Simpler interface than inventory or other resource storages to just provide aggregate resource info
/// </summary>
public interface IAggregateResourceSource
{
    /// <summary>
    ///   Calculates how much of each resource there is whole units of in total
    /// </summary>
    /// <returns>
    ///   The calculated dictionary, this needs to be a copy of the data to allow the data receiver to modify it
    /// </returns>
    public Dictionary<WorldResource, int> CalculateWholeAvailableResources();
}
