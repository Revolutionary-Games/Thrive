using System.Collections.Generic;

/// <summary>
///   Anything that can be constructed in a city
/// </summary>
public interface ICityConstructionProject
{
    public LocalizedString ProjectName { get; }

    public IReadOnlyDictionary<WorldResource, int> ConstructionCost { get; }
}
